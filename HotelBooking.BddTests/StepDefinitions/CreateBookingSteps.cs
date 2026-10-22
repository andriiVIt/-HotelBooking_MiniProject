using HotelBooking.Core;
using Moq;
using Reqnroll;


namespace HotelBooking.BddTests.StepDefinitions;

[Binding]
public class CreateBookingSteps
{
    private readonly List<Room> rooms = new();
    private readonly List<Booking> bookings = new();
    private readonly Mock<IRepository<Room>> roomRepository = new();
    private readonly Mock<IRepository<Booking>> bookingRepository = new();

    private bool bookingCreated;
    private Exception? caughtException;

    public CreateBookingSteps()
    {
        // Fake repositories return our in-memory lists
        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(bookings);
    }

    [Given("the hotel has {int} rooms")]
    public void GivenTheHotelHasRooms(int numberOfRooms)
    {
        for (int i = 1; i <= numberOfRooms; i++)
            rooms.Add(new Room { Id = i, Description = $"Room {i}" });
    }

    [Given("all rooms are booked from {int} to {int} days from today")]
    public void GivenAllRoomsAreBooked(int startOffset, int endOffset)
    {
        foreach (var room in rooms)
        {
            bookings.Add(new Booking
            {
                StartDate = DateTime.Today.AddDays(startOffset),
                EndDate = DateTime.Today.AddDays(endOffset),
                IsActive = true,
                RoomId = room.Id,
                CustomerId = 1
            });
        }
    }

    [When("I try to book a room from {int} to {int} days from today")]
    public async Task WhenITryToBookARoom(int startOffset, int endOffset)
    {
        var bookingManager = new BookingManager(bookingRepository.Object, roomRepository.Object);
        var booking = new Booking
        {
            StartDate = DateTime.Today.AddDays(startOffset),
            EndDate = DateTime.Today.AddDays(endOffset),
            CustomerId = 1
        };

        try
        {
            bookingCreated = await bookingManager.CreateBooking(booking);
        }
        catch (ArgumentException ex)
        {
            caughtException = ex;
        }
    }

    [Then("the booking should be created")]
    public void ThenTheBookingShouldBeCreated()
    {
        Assert.Null(caughtException);
        Assert.True(bookingCreated);
        bookingRepository.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Once);
    }

    [Then("the booking should be rejected")]
    public void ThenTheBookingShouldBeRejected()
    {
        Assert.Null(caughtException);
        Assert.False(bookingCreated);
        bookingRepository.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Then("the booking should fail because the dates are invalid")]
    public void ThenTheBookingShouldFail()
    {
        Assert.IsType<ArgumentException>(caughtException);
        bookingRepository.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
    }
}