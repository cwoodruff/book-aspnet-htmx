using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HtmxTabsAccordionsDemo.Pages;

public class ProductModel : PageModel
{
    public string ProductName { get; set; } = "Contoso Headphones";
    public int ProductId { get; set; } = 42;

    public IActionResult OnGetInfo(int productId)
    {
        var html =
            "<p>This is a great pair of wireless headphones with noise cancellation and 20-hour battery life.</p>";
        return Content(html, "text/html");
    }

    public IActionResult OnGetSpecs(int productId)
    {
        var html = "<ul><li>Bluetooth 5.2</li><li>Noise Cancellation</li><li>USB-C Charging</li></ul>";
        return Content(html, "text/html");
    }

    public IActionResult OnGetReviews(int productId)
    {
        var html = "<p>⭐️⭐️⭐️⭐️☆ - Sounds amazing and battery lasts all day.</p>";
        return Content(html, "text/html");
    }

    private readonly Dictionary<int, string> _answers = new()
    {
        { 1, "You can return any item within 30 days of delivery for a full refund." },
        { 2, "Yes, we ship to over 50 countries worldwide. Shipping costs vary by region." },
        { 3, "To request a refund, visit your order history and click 'Request Refund' next to the item." }
    };

    public IActionResult OnGetAnswer(int id)
    {
        if (_answers.TryGetValue(id, out var answer))
        {
            return Content($"<p>{answer}</p>", "text/html");
        }

        return NotFound();
    }
}