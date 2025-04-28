using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HtmxFormsModalDemo.Data;
using System.Threading.Tasks;

namespace HtmxFormsModalDemo.Pages.Comments;

public class CommentModel(AppDbContext db) : PageModel
{
    [BindProperty] public string Message { get; set; } = string.Empty;

    public async Task<PartialViewResult> OnPostAsync()
    {
        var comment = new Comment { Message = Request.Form["message"], CreatedAt = DateTime.UtcNow };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        return Partial("_CommentPartial", comment);
    }
}