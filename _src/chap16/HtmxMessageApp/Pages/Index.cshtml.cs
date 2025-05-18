// Pages/Index.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HtmxMessageApp.Pages;

public class IndexModel(ILogger<IndexModel> logger) : PageModel
{
    private static List<string> _messages = new();
    private static int _unreadCount = 0;

    [BindProperty]
    public string Text { get; set; } = string.Empty;

    public List<string> Messages => _messages;
    public int UnreadCount => _unreadCount;

    public void OnGet() 
    {
        // Initial page load - nothing special needed
    }

    public async Task<IActionResult> OnPostSendAsync()
    {
        if (!string.IsNullOrWhiteSpace(Text))
        {
            _messages.Insert(0, Text);  // Add new message at the beginning
            _unreadCount++;
        }

        // Set content type for the response
        Response.ContentType = "text/html";

        // Return both message list and unread count
        var html = new StringWriter();

        // Render the message list partial view
        await html.WriteAsync(await RazorPartialToString.RenderPartialViewToString(
            HttpContext, 
            "_MessageList", 
            Messages));

        // Append the unread count div with hx-oob attribute
        await html.WriteAsync($"<div id=\"unread-count\" hx-swap-oob=\"true\">Messages: {UnreadCount}</div>");

        return Content(html.ToString(), "text/html");
    }
}