namespace HtmxHistoryDemo.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class ProductSearchResult
    {
        public List<Product> Products { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string Query { get; set; } = string.Empty;
        public List<string> SelectedCategories { get; set; } = new();
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasBooks => SelectedCategories.Contains("books");
        public bool HasElectronics => SelectedCategories.Contains("electronics");
        public bool HasClothing => SelectedCategories.Contains("clothing");
    }
}