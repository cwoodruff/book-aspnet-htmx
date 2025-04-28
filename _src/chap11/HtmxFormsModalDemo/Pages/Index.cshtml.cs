using Microsoft.AspNetCore.Mvc.RazorPages;
using HtmxFormsModalDemo.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HtmxFormsModalDemo.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Comment> Comments { get; set; } = new List<Comment>();

    public async Task OnGetAsync()
    {
        Comments = await db.Comments.ToListAsync();
    }
}