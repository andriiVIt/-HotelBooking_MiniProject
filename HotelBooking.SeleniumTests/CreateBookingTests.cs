using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace HotelBooking.SeleniumTests;

public class CreateBookingTests : IDisposable
{
    private readonly IWebDriver driver;

    public CreateBookingTests()
    {
        var options = new ChromeOptions();
            // options.AddArgument("--headless=new");
        options.AddArgument("--ignore-certificate-errors");
        // Selenium Manager finds a ChromeDriver that matches the installed Chrome
        driver = new ChromeDriver(options);
    }

    public void Dispose()
    {
        driver.Quit();
    }

    [Fact]
    public void CreateBooking_TC1_PeriodBeforeOccupiedRange_RedirectsToBookingsList()
    {
        // Arrange
        var createBookingPage = new CreateBookingPage(driver);
        createBookingPage.Visit();

        // Act
        createBookingPage.SubmitBooking(DateTime.Today.AddDays(1), DateTime.Today.AddDays(3));
        string url = createBookingPage.WaitUntilRedirected();

        // Assert
        Assert.EndsWith("/Bookings", url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateBooking_TC4_EndDateOnFirstOccupiedDay_ShowsErrorMessage()
    {
        // Arrange
        var createBookingPage = new CreateBookingPage(driver);
        createBookingPage.Visit();

        // Act
        createBookingPage.SubmitBooking(DateTime.Today.AddDays(3), DateTime.Today.AddDays(4));
        string message = createBookingPage.GetStatusMessage();

        // Assert
        Assert.Equal("The booking could not be created. There were no available room.", message);
    }
}