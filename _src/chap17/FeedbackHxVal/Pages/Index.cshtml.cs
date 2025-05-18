using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FeedbackHxVal.Pages;

public class IndexModel : PageModel
{
    [BindProperty]
    public string Message { get; set; }

    [BindProperty]
    public DateTime SubmittedAt { get; set; }

    [BindProperty]
    public string UserRole { get; set; }

    public IActionResult OnPostSubmit()
    {
        // Simulate storage or logging of feedback with metadata
        var responseHtml = $@"
            <p><strong>Thank you for your feedback!</strong></p>
            <p><em>Received at {SubmittedAt:u} from a {UserRole} user.</em></p>";
        
        return Content(responseHtml, "text/html");
    }
}