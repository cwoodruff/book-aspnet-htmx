using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    [BindProperty]
    public string Query { get; set; }

    [BindProperty]
    public string Filter { get; set; }

    public List<string> Results { get; set; } = new();

    public void OnGet()
    {
        Results = PerformSearch(Query, Filter);
    }

    public IActionResult OnPostSearch()
    {
        Results = PerformSearch(Query, Filter);
        return Partial("_Results", Results);
    }

    private List<string> PerformSearch(string query, string filter)
    {
        var allItems = new List<string> { "Active Item A", "Archived Item B", "Active Item C", "Item D" };

        return allItems
            .Where(item =>
                (string.IsNullOrEmpty(query) || item.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
                (filter == "all" ||
                 (filter == "active" && item.Contains("Active")) ||
                 (filter == "archived" && item.Contains("Archived"))))
            .ToList();
    }
}