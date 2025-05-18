using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyAccountSetup.Pages;

public class IndexModel(ILogger<IndexModel> logger) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;

    public IActionResult OnPostStep1(string FirstName, string LastName)
    {
        // Optionally store in session or temp data
        // Validate names...
        return Partial("_Step2");
    }

    public IActionResult OnPostStep2(string PhoneNumber, string Address)
    {
        // Validate/contact data...
        return Partial("_Step3");
    }

    public IActionResult OnPostComplete()
    {
        // Finalize account setup
        return Content("<h3>Account created successfully!</h3>", "text/html");
    }
}