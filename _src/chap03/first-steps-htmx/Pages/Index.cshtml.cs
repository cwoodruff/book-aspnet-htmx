using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace first_steps_htmx.Pages;

public class IndexModel(ILogger<IndexModel> logger) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;

    private static readonly string[] Messages =
    {
        "Hello, World!", "Welcome to htmx!", "Dynamic content is fun!"
    };

    public IActionResult OnGetMessage()
    {
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            var message = Messages[new Random().Next(Messages.Length)];
            return Content($"<p>{message}</p>", "text/html");
        }

        return Content($"<p>No htmx</p>", "text/html");
    }
    
    public IActionResult OnPostSubmit(string name)
    {
        return Content($"<p>Thank you, {name}!</p>", "text/html");
    }
    
    public IActionResult OnGetSnippet()
    {
        return Content($"<p>Welcome to htmx!</p>", "text/html");
    }
    
    public IActionResult OnPostSubmitBefore(string name)
    {
        return Content($"<p>Thank you, {name}!</p>", "text/html");
    }
    
    public IActionResult OnPostSubmitValidate(string email)
    {
        return Content($"<p>Sending email to {email}!</p>", "text/html");
    }
}