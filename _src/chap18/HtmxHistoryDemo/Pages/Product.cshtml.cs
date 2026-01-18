using HtmxHistoryDemo.Models;
using HtmxHistoryDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HtmxHistoryDemo.Pages
{
    public class ProductModel : PageModel
    {
        private readonly ProductService _productService;

        public ProductModel(ProductService productService)
        {
            _productService = productService;
        }

        public Product? Product { get; set; }

        public IActionResult OnGet(int id)
        {
            Product = _productService.GetById(id);
            
            if (Product == null)
            {
                return NotFound();
            }

            return Page();
        }

        public IActionResult OnGetRelated(int id)
        {
            var product = _productService.GetById(id);
            if (product == null)
            {
                return NotFound();
            }

            // Get related products from the same category
            var related = _productService.Search("", new List<string> { product.Category.ToLower() }, 1, 3)
                .Products.Where(p => p.Id != id).Take(3).ToList();

            return Partial("_RelatedProducts", related);
        }
    }
}