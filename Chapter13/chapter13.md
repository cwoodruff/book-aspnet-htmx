---
order: 18
icon: stack
label: Chap 13 - Building Blocks with HTML Components- Lists and Tables
meta:
title: "Building Blocks with HTML Components: Lists and Tables"
---

# Building Blocks with HTML Components: Lists and Tables

Lists and tables display the data that drives business applications: product catalogs, user directories, transaction histories, order queues, audit logs. Traditional server-rendered tables require full page reloads to sort, filter, or paginate. That experience feels dated. Users expect to click a column header and see results instantly. They expect to scroll and load more items without navigating to a new page. They expect to delete a row and watch it disappear.

htmx delivers these interactions with server-rendered HTML. Your Razor Pages return table rows and list items as partial views. htmx swaps them into the page. No JavaScript frameworks. No client-side data binding. No duplicated business logic. The server owns the data, the filtering, the sorting, the pagination. The client displays what the server sends.

This chapter covers the patterns you need: dynamic table updates, sorting with clickable headers, pagination with page controls, infinite scroll for continuous loading, inline editing and deletion, and the combination of all these features into a cohesive data grid.

## Anti-Forgery Configuration

Before building lists with delete and edit operations, configure anti-forgery token handling. Add this to your layout so all htmx requests include the token:

**Pages/Shared/_Layout.cshtml (in the head or before closing body):**

```html
@Html.AntiForgeryToken()

<script>
document.body.addEventListener('htmx:configRequest', function(event) {
    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    if (token) {
        event.detail.headers['RequestVerificationToken'] = token.value;
    }
});
</script>
```

This ensures DELETE, PUT, and POST requests from htmx include the anti-forgery token.

## Dynamic Table Updates

Tables that refresh without page reloads create a responsive experience. Use `hx-get` to fetch updated rows and `hx-trigger` to control when updates happen.

### Live Leaderboard

A leaderboard that updates every few seconds:

**Pages/Leaderboard.cshtml:**

```html
@page
@model LeaderboardModel

<h1>Leaderboard</h1>

<div class="leaderboard-container">
    <div class="leaderboard-header">
        <span id="last-update">Last updated: @DateTime.Now.ToString("h:mm:ss tt")</span>
        <button hx-get="/Leaderboard?handler=Rows"
                hx-target="#leaderboard-body"
                hx-swap="innerHTML"
                hx-indicator="#refresh-spinner"
                class="btn btn-outline-secondary btn-sm">
            Refresh Now
            <span id="refresh-spinner" class="htmx-indicator spinner-border spinner-border-sm"></span>
        </button>
    </div>

    <table class="table">
        <thead>
            <tr>
                <th>Rank</th>
                <th>Player</th>
                <th>Score</th>
                <th>Games</th>
            </tr>
        </thead>
        <tbody id="leaderboard-body"
               hx-get="/Leaderboard?handler=Rows"
               hx-trigger="load, every 10s"
               hx-swap="innerHTML"
               hx-indicator="#table-loading">
            <!-- Rows load here -->
        </tbody>
    </table>
    
    <div id="table-loading" class="htmx-indicator text-center py-3">
        <span class="spinner-border"></span> Updating...
    </div>
</div>
```

**Pages/Leaderboard.cshtml.cs:**

```csharp
public class LeaderboardModel : PageModel
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardModel(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    public List<PlayerScore> TopPlayers { get; set; } = new();

    public void OnGet()
    {
        TopPlayers = _leaderboardService.GetTopPlayers(10);
    }

    public IActionResult OnGetRows()
    {
        var players = _leaderboardService.GetTopPlayers(10);
        return Partial("_LeaderboardRows", players);
    }
}
```

**Pages/Shared/_LeaderboardRows.cshtml:**

```html
@model List<PlayerScore>

@if (Model.Any())
{
    var rank = 1;
    @foreach (var player in Model)
    {
        <tr class="@(rank <= 3 ? "table-warning" : "")">
            <td>
                @if (rank == 1) { <span class="badge bg-gold">1st</span> }
                else if (rank == 2) { <span class="badge bg-silver">2nd</span> }
                else if (rank == 3) { <span class="badge bg-bronze">3rd</span> }
                else { @rank }
            </td>
            <td>@player.Name</td>
            <td>@player.Score.ToString("N0")</td>
            <td>@player.GamesPlayed</td>
        </tr>
        rank++;
    }
}
else
{
    <tr>
        <td colspan="4" class="text-center text-muted py-4">
            No players yet. Be the first to play!
        </td>
    </tr>
}
```

The `hx-trigger="load, every 10s"` loads data immediately and refreshes every 10 seconds. The manual refresh button lets users update on demand.

## Sorting

Clickable column headers that sort data require tracking the current sort state and toggling direction on repeated clicks.

### Sortable Table

**Pages/Tasks.cshtml:**

```html
@page
@model TasksModel

<h1>Tasks</h1>

<div id="task-table-container">
    <table class="table table-hover">
        <thead>
            <tr>
                <th>
                    <a hx-get="/Tasks?handler=List&sortBy=name&sortDir=@Model.NextSortDir("name")"
                       hx-target="#task-table-body"
                       hx-swap="innerHTML"
                       hx-push-url="true"
                       hx-indicator="#sort-spinner"
                       class="sort-header @(Model.SortBy == "name" ? "active" : "")">
                        Name
                        @if (Model.SortBy == "name")
                        {
                            <span class="sort-icon">@(Model.SortDir == "asc" ? "▲" : "▼")</span>
                        }
                    </a>
                </th>
                <th>
                    <a hx-get="/Tasks?handler=List&sortBy=dueDate&sortDir=@Model.NextSortDir("dueDate")"
                       hx-target="#task-table-body"
                       hx-swap="innerHTML"
                       hx-push-url="true"
                       hx-indicator="#sort-spinner"
                       class="sort-header @(Model.SortBy == "dueDate" ? "active" : "")">
                        Due Date
                        @if (Model.SortBy == "dueDate")
                        {
                            <span class="sort-icon">@(Model.SortDir == "asc" ? "▲" : "▼")</span>
                        }
                    </a>
                </th>
                <th>
                    <a hx-get="/Tasks?handler=List&sortBy=status&sortDir=@Model.NextSortDir("status")"
                       hx-target="#task-table-body"
                       hx-swap="innerHTML"
                       hx-push-url="true"
                       hx-indicator="#sort-spinner"
                       class="sort-header @(Model.SortBy == "status" ? "active" : "")">
                        Status
                        @if (Model.SortBy == "status")
                        {
                            <span class="sort-icon">@(Model.SortDir == "asc" ? "▲" : "▼")</span>
                        }
                    </a>
                </th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody id="task-table-body">
            <partial name="_TaskRows" model="Model" />
        </tbody>
    </table>
    
    <span id="sort-spinner" class="htmx-indicator">Sorting...</span>
</div>
```

**Pages/Tasks.cshtml.cs:**

```csharp
public class TasksModel : PageModel
{
    private readonly ITaskService _taskService;

    public TasksModel(ITaskService taskService)
    {
        _taskService = taskService;
    }

    public List<TaskItem> Tasks { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "dueDate";
    
    [BindProperty(SupportsGet = true)]
    public string SortDir { get; set; } = "asc";

    public IActionResult OnGet()
    {
        Tasks = GetSortedTasks();

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return Partial("_TaskRows", this);
        }

        return Page();
    }

    public IActionResult OnGetList(string sortBy = "dueDate", string sortDir = "asc")
    {
        SortBy = sortBy;
        SortDir = sortDir;
        Tasks = GetSortedTasks();
        return Partial("_TaskRows", this);
    }

    private List<TaskItem> GetSortedTasks()
    {
        var query = _taskService.GetAll().AsQueryable();

        query = (SortBy, SortDir) switch
        {
            ("name", "asc") => query.OrderBy(t => t.Name),
            ("name", "desc") => query.OrderByDescending(t => t.Name),
            ("dueDate", "asc") => query.OrderBy(t => t.DueDate),
            ("dueDate", "desc") => query.OrderByDescending(t => t.DueDate),
            ("status", "asc") => query.OrderBy(t => t.Status),
            ("status", "desc") => query.OrderByDescending(t => t.Status),
            _ => query.OrderBy(t => t.DueDate)
        };

        return query.ToList();
    }

    public string NextSortDir(string column)
    {
        if (SortBy == column)
        {
            return SortDir == "asc" ? "desc" : "asc";
        }
        return "asc";
    }
}
```

**Pages/Shared/_TaskRows.cshtml:**

```html
@model TasksModel

@if (Model.Tasks.Any())
{
    @foreach (var task in Model.Tasks)
    {
        <tr id="task-@task.Id">
            <td>@task.Name</td>
            <td>
                @if (task.DueDate.HasValue)
                {
                    <span class="@(task.DueDate < DateTime.Today ? "text-danger" : "")">
                        @task.DueDate.Value.ToString("MMM d, yyyy")
                    </span>
                }
                else
                {
                    <span class="text-muted">No due date</span>
                }
            </td>
            <td>
                <span class="badge @GetStatusBadgeClass(task.Status)">
                    @task.Status
                </span>
            </td>
            <td>
                <button hx-get="/Tasks?handler=EditForm&id=@task.Id"
                        hx-target="#task-@task.Id"
                        hx-swap="outerHTML"
                        class="btn btn-sm btn-outline-primary">
                    Edit
                </button>
                <button hx-delete="/Tasks?handler=Delete&id=@task.Id"
                        hx-target="#task-@task.Id"
                        hx-swap="outerHTML"
                        hx-confirm="Delete '@task.Name'?"
                        class="btn btn-sm btn-outline-danger">
                    Delete
                </button>
            </td>
        </tr>
    }
}
else
{
    <tr>
        <td colspan="4" class="text-center text-muted py-4">
            <p>No tasks found.</p>
            <button hx-get="/Tasks?handler=CreateForm" 
                    hx-target="#modal-content"
                    class="btn btn-primary">
                Create your first task
            </button>
        </td>
    </tr>
}

@functions {
    string GetStatusBadgeClass(string status) => status switch
    {
        "Complete" => "bg-success",
        "In Progress" => "bg-primary",
        "Blocked" => "bg-danger",
        _ => "bg-secondary"
    };
}
```

### Sort Header CSS

```css
.sort-header {
    text-decoration: none;
    color: inherit;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
}

.sort-header:hover {
    color: #0d6efd;
}

.sort-header.active {
    font-weight: 600;
}

.sort-icon {
    font-size: 0.75rem;
}
```

## Pagination

Server-side pagination returns a subset of data based on page number and page size.

### Paginated Product List

**Pages/Products.cshtml:**

```html
@page
@model ProductsModel

<h1>Products</h1>

<div id="products-container">
    <div class="products-header">
        <span>Showing @Model.StartItem - @Model.EndItem of @Model.TotalItems products</span>
    </div>

    <div id="product-list">
        <partial name="_ProductList" model="Model" />
    </div>
</div>
```

**Pages/Products.cshtml.cs:**

```csharp
public class ProductsModel : PageModel
{
    private readonly IProductService _productService;
    private const int PageSize = 12;

    public ProductsModel(IProductService productService)
    {
        _productService = productService;
    }

    public List<Product> Products { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int StartItem => (CurrentPage - 1) * PageSize + 1;
    public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);

    public IActionResult OnGet(int currentPage = 1)
    {
        CurrentPage = currentPage;
        LoadProducts();

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return Partial("_ProductList", this);
        }

        return Page();
    }

    public IActionResult OnGetPage(int page = 1)
    {
        CurrentPage = page;
        LoadProducts();
        return Partial("_ProductList", this);
    }

    private void LoadProducts()
    {
        var allProducts = _productService.GetAll();
        TotalItems = allProducts.Count;
        TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);
        
        Products = allProducts
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }
}
```

**Pages/Shared/_ProductList.cshtml:**

```html
@model ProductsModel

<div class="product-grid">
    @foreach (var product in Model.Products)
    {
        <div class="product-card">
            <img src="@product.ImageUrl" alt="@product.Name" class="product-image" />
            <h3 class="product-name">@product.Name</h3>
            <p class="product-price">@product.Price.ToString("C")</p>
            <button class="btn btn-primary btn-sm">Add to Cart</button>
        </div>
    }
</div>

@if (Model.TotalPages > 1)
{
    <nav class="pagination-container" aria-label="Product pagination">
        <ul class="pagination">
            @* Previous button *@
            <li class="page-item @(Model.CurrentPage == 1 ? "disabled" : "")">
                @if (Model.CurrentPage > 1)
                {
                    <a class="page-link"
                       hx-get="/Products?handler=Page&page=@(Model.CurrentPage - 1)"
                       hx-target="#product-list"
                       hx-swap="innerHTML"
                       hx-push-url="/Products?currentPage=@(Model.CurrentPage - 1)"
                       hx-indicator="#page-spinner">
                        Previous
                    </a>
                }
                else
                {
                    <span class="page-link">Previous</span>
                }
            </li>

            @* Page numbers *@
            @for (int i = 1; i <= Model.TotalPages; i++)
            {
                <li class="page-item @(i == Model.CurrentPage ? "active" : "")">
                    <a class="page-link"
                       hx-get="/Products?handler=Page&page=@i"
                       hx-target="#product-list"
                       hx-swap="innerHTML"
                       hx-push-url="/Products?currentPage=@i"
                       hx-indicator="#page-spinner"
                       @(i == Model.CurrentPage ? "aria-current=page" : "")>
                        @i
                    </a>
                </li>
            }

            @* Next button *@
            <li class="page-item @(Model.CurrentPage == Model.TotalPages ? "disabled" : "")">
                @if (Model.CurrentPage < Model.TotalPages)
                {
                    <a class="page-link"
                       hx-get="/Products?handler=Page&page=@(Model.CurrentPage + 1)"
                       hx-target="#product-list"
                       hx-swap="innerHTML"
                       hx-push-url="/Products?currentPage=@(Model.CurrentPage + 1)"
                       hx-indicator="#page-spinner">
                        Next
                    </a>
                }
                else
                {
                    <span class="page-link">Next</span>
                }
            </li>
        </ul>
        
        <span id="page-spinner" class="htmx-indicator">Loading...</span>
    </nav>
}
```

## Infinite Scroll

Infinite scroll loads more content as the user scrolls down, creating a continuous browsing experience.

### Infinite Product Feed

**Pages/Feed.cshtml:**

```html
@page
@model FeedModel

<h1>Product Feed</h1>

<div id="feed-container">
    <partial name="_FeedItems" model="Model" />
</div>
```

**Pages/Feed.cshtml.cs:**

```csharp
public class FeedModel : PageModel
{
    private readonly IProductService _productService;
    private const int PageSize = 20;

    public FeedModel(IProductService productService)
    {
        _productService = productService;
    }

    public List<Product> Products { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    
    public bool HasMore { get; set; }

    public void OnGet()
    {
        LoadProducts();
    }

    public IActionResult OnGetMore(int page = 1)
    {
        CurrentPage = page;
        LoadProducts();
        return Partial("_FeedItems", this);
    }

    private void LoadProducts()
    {
        var totalCount = _productService.GetCount();
        
        Products = _productService.GetPage(CurrentPage, PageSize);
        HasMore = CurrentPage * PageSize < totalCount;
    }
}
```

**Pages/Shared/_FeedItems.cshtml:**

```html
@model FeedModel

@foreach (var product in Model.Products)
{
    <div class="feed-item">
        <img src="@product.ImageUrl" alt="@product.Name" />
        <div class="feed-item-content">
            <h3>@product.Name</h3>
            <p>@product.Description</p>
            <span class="price">@product.Price.ToString("C")</span>
        </div>
    </div>
}

@if (Model.HasMore)
{
    <div hx-get="/Feed?handler=More&page=@(Model.CurrentPage + 1)"
         hx-trigger="revealed"
         hx-swap="outerHTML"
         hx-indicator="#load-more-spinner"
         class="load-more-trigger">
        <div id="load-more-spinner" class="htmx-indicator text-center py-4">
            <span class="spinner-border"></span>
            <p>Loading more products...</p>
        </div>
    </div>
}
else if (Model.CurrentPage > 1)
{
    <div class="end-of-feed text-center py-4 text-muted">
        <p>You've reached the end!</p>
    </div>
}
```

The trigger div at the bottom uses `hx-trigger="revealed"` to fire when it scrolls into view. It replaces itself with the next batch of items plus a new trigger (if more items exist). When no more items exist, it shows an end message instead.

### Infinite Scroll CSS

```css
.feed-item {
    display: flex;
    gap: 1rem;
    padding: 1rem;
    border-bottom: 1px solid #dee2e6;
}

.feed-item img {
    width: 120px;
    height: 120px;
    object-fit: cover;
    border-radius: 8px;
}

.load-more-trigger {
    min-height: 100px;
}

.end-of-feed {
    color: #6c757d;
}
```

## Filtering

Filters narrow down displayed data based on user criteria.

### Filterable Task List

**Pages/Tasks.cshtml (updated with filters):**

```html
@page
@model TasksModel

<h1>Tasks</h1>

<div class="filters-bar mb-3">
    <div class="row g-2">
        <div class="col-md-3">
            <select name="status" 
                    class="form-select"
                    hx-get="/Tasks?handler=List"
                    hx-target="#task-table-body"
                    hx-include=".filters-bar"
                    hx-push-url="true">
                <option value="">All Statuses</option>
                <option value="Pending" selected="@(Model.StatusFilter == "Pending")">Pending</option>
                <option value="In Progress" selected="@(Model.StatusFilter == "In Progress")">In Progress</option>
                <option value="Complete" selected="@(Model.StatusFilter == "Complete")">Complete</option>
                <option value="Blocked" selected="@(Model.StatusFilter == "Blocked")">Blocked</option>
            </select>
        </div>
        <div class="col-md-4">
            <input type="search" 
                   name="search"
                   value="@Model.SearchTerm"
                   class="form-control"
                   placeholder="Search tasks..."
                   hx-get="/Tasks?handler=List"
                   hx-target="#task-table-body"
                   hx-include=".filters-bar"
                   hx-trigger="keyup changed delay:300ms, search"
                   hx-push-url="true" />
        </div>
        <div class="col-md-2">
            <button type="button"
                    class="btn btn-outline-secondary"
                    hx-get="/Tasks?handler=List"
                    hx-target="#task-table-body"
                    hx-push-url="/Tasks">
                Clear Filters
            </button>
        </div>
    </div>
    
    @* Hidden inputs to preserve sort state *@
    <input type="hidden" name="sortBy" value="@Model.SortBy" />
    <input type="hidden" name="sortDir" value="@Model.SortDir" />
</div>

<table class="table">
    <!-- thead with sortable headers -->
    <tbody id="task-table-body">
        <partial name="_TaskRows" model="Model" />
    </tbody>
</table>
```

**Updated PageModel with filtering:**

```csharp
public class TasksModel : PageModel
{
    private readonly ITaskService _taskService;

    public TasksModel(ITaskService taskService)
    {
        _taskService = taskService;
    }

    public List<TaskItem> Tasks { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "dueDate";
    
    [BindProperty(SupportsGet = true)]
    public string SortDir { get; set; } = "asc";
    
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public IActionResult OnGet()
    {
        Tasks = GetFilteredAndSortedTasks();

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return Partial("_TaskRows", this);
        }

        return Page();
    }

    public IActionResult OnGetList(
        string? status = null, 
        string? search = null,
        string sortBy = "dueDate", 
        string sortDir = "asc")
    {
        StatusFilter = status;
        SearchTerm = search;
        SortBy = sortBy;
        SortDir = sortDir;
        
        Tasks = GetFilteredAndSortedTasks();
        return Partial("_TaskRows", this);
    }

    private List<TaskItem> GetFilteredAndSortedTasks()
    {
        var query = _taskService.GetAll().AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(StatusFilter))
        {
            query = query.Where(t => t.Status == StatusFilter);
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            query = query.Where(t => 
                t.Name.ToLower().Contains(term) || 
                (t.Description != null && t.Description.ToLower().Contains(term)));
        }

        // Apply sorting
        query = (SortBy, SortDir) switch
        {
            ("name", "asc") => query.OrderBy(t => t.Name),
            ("name", "desc") => query.OrderByDescending(t => t.Name),
            ("dueDate", "asc") => query.OrderBy(t => t.DueDate),
            ("dueDate", "desc") => query.OrderByDescending(t => t.DueDate),
            ("status", "asc") => query.OrderBy(t => t.Status),
            ("status", "desc") => query.OrderByDescending(t => t.Status),
            _ => query.OrderBy(t => t.DueDate)
        };

        return query.ToList();
    }

    public string NextSortDir(string column) =>
        SortBy == column && SortDir == "asc" ? "desc" : "asc";
}
```

The `hx-include=".filters-bar"` ensures all filter values are sent with each request, preserving filter state when sorting or searching.

## Inline Editing

Edit rows in place without opening a modal or navigating to a new page.

### Inline Edit Row

**Pages/Shared/_TaskRow.cshtml (view mode):**

```html
@model TaskItem

<tr id="task-@Model.Id">
    <td>@Model.Name</td>
    <td>@Model.DueDate?.ToString("MMM d, yyyy")</td>
    <td><span class="badge @GetStatusClass()">@Model.Status</span></td>
    <td>
        <button hx-get="/Tasks?handler=EditForm&id=@Model.Id"
                hx-target="#task-@Model.Id"
                hx-swap="outerHTML"
                class="btn btn-sm btn-outline-primary">
            Edit
        </button>
        <button hx-delete="/Tasks?handler=Delete&id=@Model.Id"
                hx-target="#task-@Model.Id"
                hx-swap="outerHTML"
                hx-confirm="Delete '@Model.Name'?"
                class="btn btn-sm btn-outline-danger">
            Delete
        </button>
    </td>
</tr>

@functions {
    string GetStatusClass() => Model.Status switch
    {
        "Complete" => "bg-success",
        "In Progress" => "bg-primary",
        "Blocked" => "bg-danger",
        _ => "bg-secondary"
    };
}
```

**Pages/Shared/_TaskEditRow.cshtml (edit mode):**

```html
@model TaskItem

<tr id="task-@Model.Id" class="table-active">
    <td>
        <input type="text" 
               name="name" 
               value="@Model.Name" 
               class="form-control form-control-sm" 
               required />
    </td>
    <td>
        <input type="date" 
               name="dueDate" 
               value="@Model.DueDate?.ToString("yyyy-MM-dd")" 
               class="form-control form-control-sm" />
    </td>
    <td>
        <select name="status" class="form-select form-select-sm">
            <option value="Pending" selected="@(Model.Status == "Pending")">Pending</option>
            <option value="In Progress" selected="@(Model.Status == "In Progress")">In Progress</option>
            <option value="Complete" selected="@(Model.Status == "Complete")">Complete</option>
            <option value="Blocked" selected="@(Model.Status == "Blocked")">Blocked</option>
        </select>
    </td>
    <td>
        <button hx-put="/Tasks?handler=Update&id=@Model.Id"
                hx-target="#task-@Model.Id"
                hx-swap="outerHTML"
                hx-include="closest tr"
                class="btn btn-sm btn-success">
            Save
        </button>
        <button hx-get="/Tasks?handler=CancelEdit&id=@Model.Id"
                hx-target="#task-@Model.Id"
                hx-swap="outerHTML"
                class="btn btn-sm btn-secondary">
            Cancel
        </button>
    </td>
</tr>
```

**Task handlers:**

```csharp
public IActionResult OnGetEditForm(int id)
{
    var task = _taskService.GetById(id);
    if (task == null) return NotFound();
    return Partial("_TaskEditRow", task);
}

public IActionResult OnGetCancelEdit(int id)
{
    var task = _taskService.GetById(id);
    if (task == null) return NotFound();
    return Partial("_TaskRow", task);
}

public IActionResult OnPutUpdate(int id, string name, DateTime? dueDate, string status)
{
    var task = _taskService.GetById(id);
    if (task == null) return NotFound();

    if (string.IsNullOrWhiteSpace(name))
    {
        Response.StatusCode = 400;
        return Partial("_TaskEditRow", task);
    }

    task.Name = name;
    task.DueDate = dueDate;
    task.Status = status;
    _taskService.Update(task);

    return Partial("_TaskRow", task);
}

public IActionResult OnDeleteDelete(int id)
{
    var task = _taskService.GetById(id);
    if (task == null)
    {
        Response.StatusCode = 404;
        return Content("", "text/html");
    }

    _taskService.Delete(id);
    return Content("", "text/html"); // Empty response removes the row
}
```

## Complete Data Grid

Combine sorting, filtering, pagination, and CRUD operations into a complete data grid:

**Pages/Orders.cshtml:**

```html
@page
@model OrdersModel

<h1>Orders</h1>

<div class="data-grid">
    <!-- Filters -->
    <div class="grid-toolbar mb-3">
        <div class="row g-2 align-items-end">
            <div class="col-md-2">
                <label class="form-label">Status</label>
                <select name="status" class="form-select"
                        hx-get="/Orders?handler=List"
                        hx-target="#orders-grid"
                        hx-include=".grid-toolbar">
                    <option value="">All</option>
                    @foreach (var status in Model.AvailableStatuses)
                    {
                        <option value="@status" selected="@(Model.StatusFilter == status)">@status</option>
                    }
                </select>
            </div>
            <div class="col-md-2">
                <label class="form-label">From</label>
                <input type="date" name="fromDate" value="@Model.FromDate?.ToString("yyyy-MM-dd")"
                       class="form-control"
                       hx-get="/Orders?handler=List"
                       hx-target="#orders-grid"
                       hx-include=".grid-toolbar"
                       hx-trigger="change" />
            </div>
            <div class="col-md-2">
                <label class="form-label">To</label>
                <input type="date" name="toDate" value="@Model.ToDate?.ToString("yyyy-MM-dd")"
                       class="form-control"
                       hx-get="/Orders?handler=List"
                       hx-target="#orders-grid"
                       hx-include=".grid-toolbar"
                       hx-trigger="change" />
            </div>
            <div class="col-md-3">
                <label class="form-label">Search</label>
                <input type="search" name="search" value="@Model.SearchTerm"
                       class="form-control" placeholder="Order # or customer..."
                       hx-get="/Orders?handler=List"
                       hx-target="#orders-grid"
                       hx-include=".grid-toolbar"
                       hx-trigger="keyup changed delay:300ms" />
            </div>
            <div class="col-md-2">
                <button class="btn btn-outline-secondary w-100"
                        hx-get="/Orders?handler=List"
                        hx-target="#orders-grid">
                    Clear Filters
                </button>
            </div>
        </div>
        <input type="hidden" name="sortBy" value="@Model.SortBy" />
        <input type="hidden" name="sortDir" value="@Model.SortDir" />
        <input type="hidden" name="page" value="1" />
    </div>

    <!-- Grid content -->
    <div id="orders-grid" hx-history-elt>
        <partial name="_OrdersGrid" model="Model" />
    </div>
</div>
```

**Pages/Shared/_OrdersGrid.cshtml:**

```html
@model OrdersModel

<div class="table-responsive">
    <table class="table table-hover">
        <thead>
            <tr>
                <th>
                    <a hx-get="/Orders?handler=List&sortBy=orderNumber&sortDir=@Model.NextSortDir("orderNumber")"
                       hx-target="#orders-grid"
                       hx-include=".grid-toolbar"
                       class="sort-header @(Model.SortBy == "orderNumber" ? "active" : "")">
                        Order #
                        @if (Model.SortBy == "orderNumber")
                        {
                            <span>@(Model.SortDir == "asc" ? "▲" : "▼")</span>
                        }
                    </a>
                </th>
                <th>
                    <a hx-get="/Orders?handler=List&sortBy=date&sortDir=@Model.NextSortDir("date")"
                       hx-target="#orders-grid"
                       hx-include=".grid-toolbar"
                       class="sort-header @(Model.SortBy == "date" ? "active" : "")">
                        Date
                        @if (Model.SortBy == "date")
                        {
                            <span>@(Model.SortDir == "asc" ? "▲" : "▼")</span>
                        }
                    </a>
                </th>
                <th>Customer</th>
                <th>
                    <a hx-get="/Orders?handler=List&sortBy=total&sortDir=@Model.NextSortDir("total")"
                       hx-target="#orders-grid"
                       hx-include=".grid-toolbar"
                       class="sort-header @(Model.SortBy == "total" ? "active" : "")">
                        Total
                        @if (Model.SortBy == "total")
                        {
                            <span>@(Model.SortDir == "asc" ? "▲" : "▼")</span>
                        }
                    </a>
                </th>
                <th>Status</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @if (Model.Orders.Any())
            {
                @foreach (var order in Model.Orders)
                {
                    <tr id="order-@order.Id">
                        <td><a href="/Orders/@order.Id">@order.OrderNumber</a></td>
                        <td>@order.OrderDate.ToString("MMM d, yyyy")</td>
                        <td>@order.CustomerName</td>
                        <td>@order.Total.ToString("C")</td>
                        <td><span class="badge @GetStatusClass(order.Status)">@order.Status</span></td>
                        <td>
                            <a href="/Orders/@order.Id" class="btn btn-sm btn-outline-primary">View</a>
                        </td>
                    </tr>
                }
            }
            else
            {
                <tr>
                    <td colspan="6" class="text-center py-4 text-muted">
                        No orders found matching your criteria.
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>

@if (Model.TotalPages > 1)
{
    <nav class="d-flex justify-content-between align-items-center">
        <span class="text-muted">
            Showing @Model.StartItem - @Model.EndItem of @Model.TotalItems orders
        </span>
        <ul class="pagination mb-0">
            <li class="page-item @(Model.CurrentPage == 1 ? "disabled" : "")">
                <a class="page-link"
                   hx-get="/Orders?handler=List&page=@(Model.CurrentPage - 1)"
                   hx-target="#orders-grid"
                   hx-include=".grid-toolbar">
                    Previous
                </a>
            </li>
            @for (int i = 1; i <= Model.TotalPages; i++)
            {
                <li class="page-item @(i == Model.CurrentPage ? "active" : "")">
                    <a class="page-link"
                       hx-get="/Orders?handler=List&page=@i"
                       hx-target="#orders-grid"
                       hx-include=".grid-toolbar">
                        @i
                    </a>
                </li>
            }
            <li class="page-item @(Model.CurrentPage == Model.TotalPages ? "disabled" : "")">
                <a class="page-link"
                   hx-get="/Orders?handler=List&page=@(Model.CurrentPage + 1)"
                   hx-target="#orders-grid"
                   hx-include=".grid-toolbar">
                    Next
                </a>
            </li>
        </ul>
    </nav>
}

@functions {
    string GetStatusClass(string status) => status switch
    {
        "Delivered" => "bg-success",
        "Shipped" => "bg-info",
        "Processing" => "bg-primary",
        "Cancelled" => "bg-danger",
        _ => "bg-secondary"
    };
}
```

## Error Handling

Handle failed requests gracefully:

```html
<tbody id="task-table-body"
       hx-on::response-error="handleTableError(event)"
       hx-on::send-error="handleTableError(event)">
</tbody>

<script>
function handleTableError(event) {
    var target = event.detail.target || document.getElementById('task-table-body');
    target.innerHTML = `
        <tr>
            <td colspan="4" class="text-center py-4">
                <div class="alert alert-danger mb-0">
                    <p>Failed to load data. Please try again.</p>
                    <button onclick="location.reload()" class="btn btn-outline-danger btn-sm">
                        Reload Page
                    </button>
                </div>
            </td>
        </tr>
    `;
}
</script>
```

## Summary

This chapter covered dynamic lists and tables with htmx:

- **Anti-forgery tokens** configured globally for all htmx requests
- **Live updates** with `hx-trigger="every Xs"` for real-time data
- **Sorting** with clickable headers and toggle direction
- **Pagination** with complete page controls and URL integration
- **Infinite scroll** using `hx-trigger="revealed"` with dynamic page tracking
- **Filtering** with combined filters using `hx-include`
- **Inline editing** with view/edit row states
- **Delete operations** with confirmation and proper token handling
- **Empty states** and error handling for better UX
- **Complete data grid** combining all features

These patterns form the foundation for any data-driven application, from admin dashboards to e-commerce catalogs.

## Preview of Next Chapter

Chapter 14 covers buttons and action links. You will learn to build one-click actions that update UI instantly, confirmation workflows for dangerous operations, batch actions for multiple items, and status toggles that feel immediate and responsive.
