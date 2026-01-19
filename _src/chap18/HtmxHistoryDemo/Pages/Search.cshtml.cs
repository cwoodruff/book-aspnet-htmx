using HtmxHistoryDemo.Models;
using HtmxHistoryDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HtmxHistoryDemo.Pages
{
    public class SearchModel : PageModel
    {
        private readonly ProductService _productService;

        public SearchModel(ProductService productService)
        {
            _productService = productService;
        }

        public ProductSearchResult? SearchResult { get; set; }
        public string Query { get; set; } = string.Empty;
        public List<string> SelectedCategories { get; set; } = new();
        public bool HasBooks => SelectedCategories.Contains("books");
        public bool HasElectronics => SelectedCategories.Contains("electronics");
        public bool HasClothing => SelectedCategories.Contains("clothing");

        public void OnGet(string query = "", List<string> category = null, int page = 1)
        {
            Query = query ?? string.Empty;
            SelectedCategories = category ?? new List<string>();
            
            if (!string.IsNullOrEmpty(Query) || SelectedCategories.Any() || Request.Query.ContainsKey("showAll"))
            {
                SearchResult = _productService.Search(Query, SelectedCategories, page, 6);
            }
        }

        public IActionResult OnGetResults(string query = "", List<string> category = null, int page = 1)
        {
            var result = _productService.Search(query ?? string.Empty, category ?? new List<string>(), page, 6);
            return Partial("_SearchResults", result);
        }
    }
}