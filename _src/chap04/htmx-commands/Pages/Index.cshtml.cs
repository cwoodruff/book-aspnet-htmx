using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace htmx_commands.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public IActionResult OnGetList()
    {
        var users = new List<string> { "Maarten", "Woody", "Khalid" };
        return Content($"<ul>{string.Join("", users.Select(u => $"<li>{u}</li>"))}</ul>", "text/html");
    }

    public IActionResult OnGetUserInfo()
    {
        User user = new User { Name = "Tracy", Age = 29 };
        return Partial("_UserInfo", user);
    }
}

public class User
{
    public string Name { get; set; }
    public int Age { get; set; }
}