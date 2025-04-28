
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace HtmxHyperscriptDemo.Pages.Status
{
    public class ToggleModel : PageModel
    {
        private const string SessionKey = "Status";

        public string CurrentStatus { get; set; } = "Inactive";

        public IActionResult OnPost()
        {
            var status = HttpContext.Session.GetString(SessionKey);
            if (string.IsNullOrEmpty(status) || status == "Inactive")
            {
                status = "Active";
            }
            else
            {
                status = "Inactive";
            }

            HttpContext.Session.SetString(SessionKey, status);
            CurrentStatus = status;

            return Page();
        }
    }
}
