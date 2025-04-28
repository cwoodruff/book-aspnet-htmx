
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HtmxHyperscriptDemo.Pages.Status
{
    public class SummaryModel : PageModel
    {
        private const string SessionKey = "Status";

        public string CurrentStatus { get; set; } = "Inactive";

        public void OnGet()
        {
            var status = HttpContext.Session.GetString(SessionKey);
            CurrentStatus = string.IsNullOrEmpty(status) ? "Inactive" : status;
        }
    }
}
