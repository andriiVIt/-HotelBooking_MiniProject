using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Core;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests
{
    public class BookingManagerTests
    {
        private readonly Mock<IRepository<Booking>> mockBookingRepository;
        private readonly Mock<IRepository<Room>> mockRoomRepository;
        private readonly IBookingManager bookingManager;

        private readonly List<Room> rooms;

        public BookingManagerTests()
        {
            rooms = new List<Room>
            {
                new Room { Id = 1, Description = "A" },
                new Room { Id = 2, Description = "B" },
            };

            mockBookingRepository = new Mock<IRepository<Booking>>();
            mockRoomRepository = new Mock<IRepository<Room>>();
            mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

            bookingManager = new BookingManager(mockBookingRepository.Object, mockRoomRepository.Object);
        }

        private void SetUpBookings(params Booking[] bookings)
        {
            mockBookingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(bookings.ToList());
        }

        #region FindAvailableRoom

        // Data-driven test: several combinations of start/end date offsets (relative to
        // today, in days) that should all be rejected by the date-range validation.
        // InlineData requires compile-time constants, so dates are expressed as day
        // offsets and converted to DateTime.Today.AddDays(...) inside the test body.
        [Theory]
        [InlineData(0, 5)]   // start date is today (not in the future)
        [InlineData(-1, 5)]  // start date is in the past
        [InlineData(5, 2)]   // start date later than end date
        public async Task FindAvailableRoom_InvalidDateRange_ThrowsArgumentException(int startOffset, int endOffset)
        {
            // Arrange
            DateTime start = DateTime.Today.AddDays(startOffset);
            DateTime end = DateTime.Today.AddDays(endOffset);
            SetUpBookings();

            // Act
            Task Result() => bookingManager.FindAvailableRoom(start, end);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(Result);
        }

        [Fact]
        public async Task FindAvailableRoom_NoExistingBookings_ReturnsIdOfFirstRoom()
        {
            // Arrange
            SetUpBookings();
            DateTime start = DateTime.Today.AddDays(1);
            DateTime end = DateTime.Today.AddDays(2);

            // Act
            int roomId = await bookingManager.FindAvailableRoom(start, end);

            // Assert
            Assert.Equal(rooms[0].Id, roomId);
        }

        // Data-driven test covering the boundary value analysis of the overlap check in
        // BookingManager.FindAvailableRoom: an existing booking occupies room 1 from
        // day 10 to day 20. Each case places the requested period at, or just past, the
        // boundaries of that existing booking, so the assertions directly exercise the
        // "<" / ">" comparisons used by the overlap condition.
        public static IEnumerable<object[]> OverlapBoundaryCases()
        {
            // requestedStartOffset, requestedEndOffset, expectRoom1Available
            yield return new object[] { 1, 9, true };    // ends the day before existing booking starts
            yield return new object[] { 1, 10, false };  // ends exactly on existing start date -> overlap
            yield return new object[] { 15, 15, false }; // fully inside existing booking -> overlap
            yield return new object[] { 20, 25, false }; // starts exactly on existing end date -> overlap
            yield return new object[] { 21, 25, true };  // starts the day after existing booking ends
            yield return new object[] { 5, 25, false };  // fully surrounds existing booking -> overlap
        }

        [Theory]
        [MemberData(nameof(OverlapBoundaryCases))]
        public async Task FindAvailableRoom_OverlapWithExistingBooking_ReturnsExpectedAvailability(
            int requestedStartOffset, int requestedEndOffset, bool expectRoom1Available)
        {
            // Arrange
            SetUpBookings(new Booking
            {
                Id = 1,
                RoomId = 1,
                IsActive = true,
                StartDate = DateTime.Today.AddDays(10),
                EndDate = DateTime.Today.AddDays(20),
            });

            DateTime start = DateTime.Today.AddDays(requestedStartOffset);
            DateTime end = DateTime.Today.AddDays(requestedEndOffset);

            // Act
            int roomId = await bookingManager.FindAvailableRoom(start, end);

            // Assert
            if (expectRoom1Available)
                Assert.Equal(1, roomId);
            else
                Assert.Equal(2, roomId); // room 1 is occupied, so room 2 is returned instead
        }

        [Fact]
        public async Task FindAvailableRoom_InactiveBookingOverlapsPeriod_RoomIsStillReturnedAsAvailable()
        {
            // Arrange: room 1 has a booking for the requested period, but it is not active
            // (e.g. it was cancelled), so it should not block the room from being booked again.
            SetUpBookings(new Booking
            {
                Id = 1,
                RoomId = 1,
                IsActive = false,
                StartDate = DateTime.Today.AddDays(10),
                EndDate = DateTime.Today.AddDays(20),
            });

            DateTime start = DateTime.Today.AddDays(12);
            DateTime end = DateTime.Today.AddDays(14);

            // Act
            int roomId = await bookingManager.FindAvailableRoom(start, end);

            // Assert
            Assert.Equal(1, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_AllRoomsOccupied_ReturnsMinusOne()
        {
            // Arrange
            DateTime start = DateTime.Today.AddDays(10);
            DateTime end = DateTime.Today.AddDays(20);

            SetUpBookings(
                new Booking { Id = 1, RoomId = 1, IsActive = true, StartDate = start, EndDate = end },
                new Booking { Id = 2, RoomId = 2, IsActive = true, StartDate = start, EndDate = end });

            // Act
            int roomId = await bookingManager.FindAvailableRoom(start, end);

            // Assert
            Assert.Equal(-1, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_ReturnedRoomHasNoOverlappingActiveBooking()
        {
            // Arrange
            SetUpBookings(new Booking
            {
                Id = 1,
                RoomId = 1,
                IsActive = true,
                StartDate = DateTime.Today.AddDays(10),
                EndDate = DateTime.Today.AddDays(20),
            });

            DateTime date = DateTime.Today.AddDays(15);

            // Act
            int roomId = await bookingManager.FindAvailableRoom(date, date);
            var allBookings = await mockBookingRepository.Object.GetAllAsync();

            var overlappingBookingsForReturnedRoom = allBookings.Where(b =>
                b.RoomId == roomId && b.IsActive && b.StartDate <= date && b.EndDate >= date);

            // Assert
            Assert.Empty(overlappingBookingsForReturnedRoom);
        }

        #endregion

        #region CreateBooking

        [Fact]
        public async Task CreateBooking_RoomAvailable_ReturnsTrue()
        {
            // Arrange
            SetUpBookings();
            var booking = new Booking
            {
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2),
            };

            // Act
            bool result = await bookingManager.CreateBooking(booking);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CreateBooking_RoomAvailable_SetsBookingActiveAndAssignsRoom()
        {
            // Arrange
            SetUpBookings();
            var booking = new Booking
            {
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2),
            };

            // Act
            await bookingManager.CreateBooking(booking);

            // Assert
            Assert.True(booking.IsActive);
            Assert.Equal(rooms[0].Id, booking.RoomId);
        }

        [Fact]
        public async Task CreateBooking_RoomAvailable_AddsBookingToRepositoryExactlyOnce()
        {
            // Arrange
            SetUpBookings();
            var booking = new Booking
            {
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2),
            };

            // Act
            await bookingManager.CreateBooking(booking);

            // Assert: use the mocking framework to verify the collaborator was invoked.
            mockBookingRepository.Verify(r => r.AddAsync(booking), Times.Once);
        }

        [Fact]
        public async Task CreateBooking_NoRoomAvailable_ReturnsFalse()
        {
            // Arrange
            DateTime start = DateTime.Today.AddDays(10);
            DateTime end = DateTime.Today.AddDays(20);
            SetUpBookings(
                new Booking { Id = 1, RoomId = 1, IsActive = true, StartDate = start, EndDate = end },
                new Booking { Id = 2, RoomId = 2, IsActive = true, StartDate = start, EndDate = end });

            var booking = new Booking { StartDate = start, EndDate = end };

            // Act
            bool result = await bookingManager.CreateBooking(booking);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CreateBooking_NoRoomAvailable_DoesNotAddBookingToRepository()
        {
            // Arrange
            DateTime start = DateTime.Today.AddDays(10);
            DateTime end = DateTime.Today.AddDays(20);
            SetUpBookings(
                new Booking { Id = 1, RoomId = 1, IsActive = true, StartDate = start, EndDate = end },
                new Booking { Id = 2, RoomId = 2, IsActive = true, StartDate = start, EndDate = end });

            var booking = new Booking { StartDate = start, EndDate = end };

            // Act
            await bookingManager.CreateBooking(booking);

            // Assert
            mockBookingRepository.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        #endregion

        #region GetFullyOccupiedDates

        [Fact]
        public async Task GetFullyOccupiedDates_StartDateLaterThanEndDate_ThrowsArgumentException()
        {
            // Arrange
            SetUpBookings();
            DateTime start = DateTime.Today.AddDays(5);
            DateTime end = DateTime.Today.AddDays(1);

            // Act
            Task Result() => bookingManager.GetFullyOccupiedDates(start, end);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(Result);
        }

        [Fact]
        public async Task GetFullyOccupiedDates_NoBookings_ReturnsEmptyList()
        {
            // Arrange
            SetUpBookings();

            // Act
            List<DateTime> occupiedDates = await bookingManager.GetFullyOccupiedDates(
                DateTime.Today.AddDays(1), DateTime.Today.AddDays(10));

            // Assert
            Assert.Empty(occupiedDates);
        }

        [Fact]
        public async Task GetFullyOccupiedDates_NoDateWithAllRoomsBooked_ReturnsEmptyList()
        {
            // Arrange: only one of the two rooms is booked for the period.
            DateTime start = DateTime.Today.AddDays(10);
            DateTime end = DateTime.Today.AddDays(12);
            SetUpBookings(new Booking { Id = 1, RoomId = 1, IsActive = true, StartDate = start, EndDate = end });

            // Act
            List<DateTime> occupiedDates = await bookingManager.GetFullyOccupiedDates(start, end);

            // Assert
            Assert.Empty(occupiedDates);
        }

        // Data-driven test: each case books the two rooms differently around the queried
        // period and asserts exactly which dates come back as fully occupied./////
        public static IEnumerable<object[]> FullyOccupiedDatesCases()
        {
            DateTime today = DateTime.Today;

            // Case 1: both rooms booked for the full queried period -> every date is occupied.
            yield return new object[]
            {
                new[]
                {
                    new Booking { Id = 1, RoomId = 1, IsActive = true, StartDate = today.AddDays(10), EndDate = today.AddDays(12) },
                    new Booking { Id = 2, RoomId = 2, IsActive = true, StartDate = today.AddDays(10), EndDate = today.AddDays(12) },
                },
                today.AddDays(10),
                today.AddDays(12),
                new[] { today.AddDays(10), today.AddDays(11), today.AddDays(12) },
            };

            // Case 2: both rooms booked for only the middle day of the queried period.
            yield return new object[]
            {
                new[]
                {
                    new Booking { Id = 1, RoomId = 1, IsActive = true, StartDate = today.AddDays(11), EndDate = today.AddDays(11) },
                    new Booking { Id = 2, RoomId = 2, IsActive = true, StartDate = today.AddDays(11), EndDate = today.AddDays(11) },
                },
                today.AddDays(10),
                today.AddDays(12),
                new[] { today.AddDays(11) },
            };

            // Case 3: an inactive (cancelled) booking should not count towards occupancy.
            yield return new object[]
            {
                new[]
                {
                    new Booking { Id = 1, RoomId = 1, IsActive = true, StartDate = today.AddDays(10), EndDate = today.AddDays(10) },
                    new Booking { Id = 2, RoomId = 2, IsActive = false, StartDate = today.AddDays(10), EndDate = today.AddDays(10) },
                },
                today.AddDays(10),
                today.AddDays(10),
                Array.Empty<DateTime>(),
            };
        }

        [Theory]
        [MemberData(nameof(FullyOccupiedDatesCases))]
        public async Task GetFullyOccupiedDates_VariousBookingConfigurations_ReturnsExpectedDates(
            Booking[] bookings, DateTime start, DateTime end, DateTime[] expectedDates)
        {
            // Arrange
            SetUpBookings(bookings);

            // Act
            List<DateTime> occupiedDates = await bookingManager.GetFullyOccupiedDates(start, end);

            // Assert
            Assert.Equal(expectedDates, occupiedDates);
        }

        #endregion
    }
}