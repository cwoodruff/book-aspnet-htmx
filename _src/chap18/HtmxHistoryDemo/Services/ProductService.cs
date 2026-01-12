using HtmxHistoryDemo.Models;

namespace HtmxHistoryDemo.Services
{
    public class ProductService
    {
        private readonly List<Product> _products;

        public ProductService()
        {
            _products = GenerateSampleProducts();
        }

        public ProductSearchResult Search(string query = "", List<string> categories = null, int page = 1, int pageSize = 10)
        {
            categories ??= new List<string>();
            
            var filteredProducts = _products.AsEnumerable();

            if (!string.IsNullOrEmpty(query))
            {
                filteredProducts = filteredProducts.Where(p => 
                    p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            if (categories.Any())
            {
                filteredProducts = filteredProducts.Where(p => 
                    categories.Contains(p.Category.ToLower()));
            }

            var totalCount = filteredProducts.Count();
            var products = filteredProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new ProductSearchResult
            {
                Products = products,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Query = query,
                SelectedCategories = categories
            };
        }

        public Product? GetById(int id)
        {
            return _products.FirstOrDefault(p => p.Id == id);
        }

        private List<Product> GenerateSampleProducts()
        {
            return new List<Product>
            {
                new() { Id = 1, Name = "Laptop Pro 13", Description = "High-performance laptop for professionals", Price = 1299.99m, Category = "Electronics" },
                new() { Id = 2, Name = "The Great Gatsby", Description = "Classic American novel by F. Scott Fitzgerald", Price = 12.99m, Category = "Books" },
                new() { Id = 3, Name = "Wireless Headphones", Description = "Noise-cancelling wireless headphones", Price = 199.99m, Category = "Electronics" },
                new() { Id = 4, Name = "Cotton T-Shirt", Description = "Comfortable 100% cotton t-shirt", Price = 24.99m, Category = "Clothing" },
                new() { Id = 5, Name = "To Kill a Mockingbird", Description = "Pulitzer Prize-winning novel by Harper Lee", Price = 14.99m, Category = "Books" },
                new() { Id = 6, Name = "Smartphone X", Description = "Latest generation smartphone with advanced features", Price = 899.99m, Category = "Electronics" },
                new() { Id = 7, Name = "Denim Jeans", Description = "Classic fit denim jeans", Price = 69.99m, Category = "Clothing" },
                new() { Id = 8, Name = "1984", Description = "Dystopian social science fiction novel by George Orwell", Price = 13.99m, Category = "Books" },
                new() { Id = 9, Name = "Tablet Ultra", Description = "10-inch tablet with high-resolution display", Price = 449.99m, Category = "Electronics" },
                new() { Id = 10, Name = "Wool Sweater", Description = "Warm wool sweater for winter", Price = 89.99m, Category = "Clothing" },
                new() { Id = 11, Name = "Pride and Prejudice", Description = "Romantic novel by Jane Austen", Price = 11.99m, Category = "Books" },
                new() { Id = 12, Name = "Gaming Mouse", Description = "High-precision gaming mouse", Price = 59.99m, Category = "Electronics" },
                new() { Id = 13, Name = "Running Shoes", Description = "Lightweight running shoes for athletes", Price = 129.99m, Category = "Clothing" },
                new() { Id = 14, Name = "The Catcher in the Rye", Description = "Coming-of-age novel by J.D. Salinger", Price = 15.99m, Category = "Books" },
                new() { Id = 15, Name = "Bluetooth Speaker", Description = "Portable Bluetooth speaker with excellent sound", Price = 79.99m, Category = "Electronics" },
                new() { Id = 16, Name = "Winter Jacket", Description = "Insulated winter jacket for cold weather", Price = 199.99m, Category = "Clothing" },
                new() { Id = 17, Name = "Harry Potter Set", Description = "Complete Harry Potter book series", Price = 149.99m, Category = "Books" },
                new() { Id = 18, Name = "Mechanical Keyboard", Description = "RGB mechanical keyboard for gaming", Price = 119.99m, Category = "Electronics" },
                new() { Id = 19, Name = "Summer Dress", Description = "Light summer dress in floral pattern", Price = 49.99m, Category = "Clothing" },
                new() { Id = 20, Name = "Lord of the Rings", Description = "Epic fantasy trilogy by J.R.R. Tolkien", Price = 39.99m, Category = "Books" }
            };
        }
    }
}