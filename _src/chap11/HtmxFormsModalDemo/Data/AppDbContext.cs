using System;
using Microsoft.EntityFrameworkCore;

namespace HtmxFormsModalDemo.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Comment> Comments { get; set; }
}

public class Comment
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}