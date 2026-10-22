using System.Globalization;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace HotelBooking.SeleniumTests;

// This class represents the "Create Booking" page in the hotel booking application.
public class CreateBookingPage
{
    private const string PageUrl = "https://localhost:5001/Bookings/Create";

    private static readonly By StartDateLocator = By.Id("StartDate");
    private static readonly By EndDateLocator = By.Id("EndDate");
    private static readonly By CreateButtonLocator =
        By.CssSelector("input[type='submit'][value='Create']");
    private static readonly By StatusMessageLocator = By.CssSelector("h4.text-danger");

    private readonly IWebDriver driver;

    public CreateBookingPage(IWebDriver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);
        this.driver = driver;
    }

    // Navigates to the "Create Booking" page.
    public void Visit()
    {
        driver.Navigate().GoToUrl(PageUrl);
    }

    // Fills in the dates and submits the form.
    public void SubmitBooking(DateTime startDate, DateTime endDate)
    {
        SetDate(StartDateLocator, startDate);
        SetDate(EndDateLocator, endDate);
        Thread.Sleep(2000); // DEMO ONLY: time to see the filled-in form
        driver.FindElement(CreateButtonLocator).Click();
        Thread.Sleep(2000); // DEMO ONLY: time to see the result page
    }

    // Waits until the browser has left the "Create" page and returns the new URL.
    public string WaitUntilRedirected()
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        wait.Until(d => !d.Url.Contains("/Create", StringComparison.OrdinalIgnoreCase));
        return driver.Url;
    }

    // Waits until the status message (shown when a booking fails) has text, and returns it.
    public string GetStatusMessage()
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException));
        return wait.Until(d =>
        {
            var text = d.FindElement(StatusMessageLocator).Text;
            return string.IsNullOrWhiteSpace(text) ? null : text;
        });
    }

    // Date inputs behave differently depending on the browser's language when typing,
    // so the value is set directly in the ISO format that the input always uses.
    private void SetDate(By locator, DateTime date)
    {
        var input = driver.FindElement(locator);
        var value = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].value = arguments[1];", input, value);
    }
}