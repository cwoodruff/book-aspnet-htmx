---
order: 15
icon: stack
label: Chap 16 - Scoped Updates with hx-select and Out-of-Band Swaps
meta:
title: "Scoped Updates with hx-select and Out-of-Band Swaps"
---

# Scoped Updates with `hx-select` and Out-of-Band Swaps

Most htmx interactions follow a simple pattern: trigger a request, receive HTML, swap it into a target. But real applications rarely update just one thing. Adding an item to a cart should update the cart count in the header. Completing a task should update the task list and the completion percentage. Sending a message should refresh the conversation and show a toast notification.

htmx provides two mechanisms for these multi-element updates: `hx-select` extracts specific content from a response, and out-of-band (OOB) swaps update elements outside your primary target. This chapter covers both patterns with complete working examples.

## Understanding the Two Approaches

Before writing code, understand the distinction:

**`hx-select`** filters the response before swapping. If your server returns a full page or a large fragment, `hx-select` extracts only the part you want. This happens on the client side.

**Out-of-band swaps** update multiple elements from a single response. The server returns the primary content plus additional elements marked with `hx-swap-oob`. htmx swaps the primary content into `hx-target`, then processes each OOB element separately.

Use `hx-select` when you can't control the response format or need to strip wrapper elements. Use OOB swaps when you control the server and need to update multiple unrelated page sections.

## Filtering Responses with `hx-select`

The `hx-select` attribute takes a CSS selector and extracts matching content from the response before swapping.

### When `hx-select` is Useful

Consider a blog that returns full pages for SEO. When navigating via htmx, you want just the article content, not the header and footer:

```html
<a hx-get="/articles/htmx-patterns"
   hx-target="#main-content"
   hx-select="article.post-content"
   hx-push-url="true"
   hx-indicator="#page-loader">
    htmx Patterns for Razor Pages
</a>

<main id="main-content">
    <!-- Article content swaps here -->
</main>
```

The server returns the full page (header, navigation, article, footer), but htmx extracts only `article.post-content` and swaps that into `#main-content`.

### Stripping Wrapper Elements

If your partial view includes a wrapper you don't want:

**Server returns:**
```html
<div class="partial-wrapper">
    <ul id="task-list">
        <li>Task 1</li>
        <li>Task 2</li>
    </ul>
</div>
```

**Client extracts just the list:**
```html
<button hx-get="/Tasks?handler=List"
        hx-target="#tasks-container"
        hx-select="#task-list">
    Refresh Tasks
</button>

<div id="tasks-container">
    <!-- Only the ul#task-list gets inserted, not the wrapper -->
</div>
```

### Complete `hx-select` Example

**Pages/Articles.cshtml:**

```html
@page
@model ArticlesModel

<h1>Articles</h1>

<nav class="article-nav">
    @foreach (var article in Model.Articles)
    {
        <a hx-get="/Articles/@article.Slug"
           hx-target="#article-content"
           hx-select=".article-body"
           hx-swap="innerHTML"
           hx-push-url="true"
           hx-indicator="#article-loader"
           class="article-link">
            @article.Title
        </a>
    }
</nav>

<div id="article-loader" class="htmx-indicator">Loading article...</div>

<div id="article-content">
    <p class="text-muted">Select an article to read.</p>
</div>
```

**Pages/Articles.cshtml.cs:**

```csharp
public class ArticlesModel : PageModel
{
    private readonly IArticleService _articleService;

    public ArticlesModel(IArticleService articleService)
    {
        _articleService = articleService;
    }

    public List<Article> Articles { get; set; } = new();

    public void OnGet()
    {
        Articles = _articleService.GetAll();
    }

    public IActionResult OnGet(string slug)
    {
        var article = _articleService.GetBySlug(slug);
        if (article == null) return NotFound();

        // Return full page for direct navigation, partial for htmx
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return Partial("_ArticleFull", article);
        }

        return Page();
    }
}
```

**Pages/Shared/_ArticleFull.cshtml:**

```html
@model Article

<article class="article-page">
    <header class="article-header">
        <h1>@Model.Title</h1>
        <p class="meta">By @Model.Author on @Model.PublishedAt.ToString("MMMM d, yyyy")</p>
    </header>
    
    <div class="article-body">
        @Html.Raw(Model.HtmlContent)
    </div>
    
    <footer class="article-footer">
        <p>Tags: @string.Join(", ", Model.Tags)</p>
    </footer>
</article>
```

The client uses `hx-select=".article-body"` to extract only the content div, leaving out the header and footer.

## Out-of-Band Swaps

Out-of-band (OOB) swaps update elements outside your `hx-target`. The server includes extra elements in the response marked with `hx-swap-oob`, and htmx processes them after the primary swap.

### How OOB Works

1. htmx sends request to server
2. Server returns primary content plus OOB elements
3. htmx swaps primary content into `hx-target`
4. htmx finds elements with `hx-swap-oob` attribute
5. For each OOB element, htmx finds matching element on page by ID
6. htmx swaps the OOB content into the matching element

### Basic OOB Pattern

**Page structure:**

```html
<header>
    <span id="cart-count">0</span> items in cart
</header>

<main>
    <div id="product-list">
        <!-- Products here -->
    </div>
</main>
```

**Add to cart button:**

```html
<button hx-post="/Cart?handler=Add&amp;productId=@product.Id"
        hx-target="#cart-feedback"
        hx-swap="innerHTML">
    Add to Cart
</button>
<span id="cart-feedback"></span>
```

**Server response includes OOB update:**

```html
<!-- Primary content for #cart-feedback -->
<span class="text-success">Added to cart!</span>

<!-- OOB update for cart count in header -->
<span id="cart-count" hx-swap-oob="true">3</span>
```

The feedback message goes into `#cart-feedback` (the target). The cart count updates `#cart-count` in the header via OOB.

### OOB Swap Strategies

The `hx-swap-oob` attribute accepts swap strategies:

| Value | Behavior |
|-------|----------|
| `true` | Replace innerHTML of matching element |
| `outerHTML` | Replace the entire matching element |
| `beforeend` | Append to matching element |
| `afterbegin` | Prepend to matching element |
| `innerHTML` | Replace innerHTML (same as `true`) |
| `outerHTML:#other-id` | Replace a different element by ID |

Examples:

```html
<!-- Replace innerHTML (default) -->
<span id="count" hx-swap-oob="true">5</span>

<!-- Replace entire element -->
<span id="count" hx-swap-oob="outerHTML">
    <span id="count" class="badge">5</span>
</span>

<!-- Append to element -->
<div id="notifications" hx-swap-oob="beforeend">
    <div class="notification">New message!</div>
</div>
```

## Complete OOB Example: Task Management

A task list where completing a task updates the list, completion count, and shows a toast notification.

**Pages/Tasks.cshtml:**

```html
@page
@model TasksPageModel

<div class="task-header">
    <h1>Tasks</h1>
    <div class="task-stats">
        <span id="completion-stats">
            @Model.CompletedCount / @Model.TotalCount completed
        </span>
        <div class="progress" style="width: 200px;">
            <div id="progress-bar" 
                 class="progress-bar" 
                 style="width: @Model.CompletionPercentage%">
            </div>
        </div>
    </div>
</div>

<ul id="task-list" class="list-group">
    @foreach (var task in Model.Tasks)
    {
        <partial name="_TaskItem" model="task" />
    }
</ul>

<!-- Toast container for notifications -->
<div id="toast-container" class="toast-container"></div>
```

**Pages/Tasks.cshtml.cs:**

```csharp
public class TasksPageModel : PageModel
{
    private readonly ITaskService _taskService;

    public TasksPageModel(ITaskService taskService)
    {
        _taskService = taskService;
    }

    public List<TaskItem> Tasks { get; set; } = new();
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
    public int CompletionPercentage => TotalCount > 0 ? (CompletedCount * 100) / TotalCount : 0;

    public void OnGet()
    {
        LoadTasks();
    }

    public IActionResult OnPostToggle(int id)
    {
        var task = _taskService.GetById(id);
        if (task == null) return NotFound();

        task.IsComplete = !task.IsComplete;
        _taskService.Update(task);

        LoadTasks();

        // Return partial with OOB updates
        return Partial("_TaskToggleResponse", new TaskToggleResponse
        {
            Task = task,
            CompletedCount = CompletedCount,
            TotalCount = TotalCount,
            CompletionPercentage = CompletionPercentage,
            ToastMessage = task.IsComplete 
                ? $"'{task.Name}' marked complete!" 
                : $"'{task.Name}' marked incomplete"
        });
    }

    public IActionResult OnDeleteDelete(int id)
    {
        var task = _taskService.GetById(id);
        if (task == null) return NotFound();

        var taskName = task.Name;
        _taskService.Delete(id);

        LoadTasks();

        return Partial("_TaskDeleteResponse", new TaskDeleteResponse
        {
            DeletedTaskId = id,
            CompletedCount = CompletedCount,
            TotalCount = TotalCount,
            CompletionPercentage = CompletionPercentage,
            ToastMessage = $"'{taskName}' deleted"
        });
    }

    private void LoadTasks()
    {
        Tasks = _taskService.GetAll();
        TotalCount = Tasks.Count;
        CompletedCount = Tasks.Count(t => t.IsComplete);
    }
}

public class TaskToggleResponse
{
    public TaskItem Task { get; set; } = null!;
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
    public int CompletionPercentage { get; set; }
    public string ToastMessage { get; set; } = "";
}

public class TaskDeleteResponse
{
    public int DeletedTaskId { get; set; }
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
    public int CompletionPercentage { get; set; }
    public string ToastMessage { get; set; } = "";
}
```

**Pages/Shared/_TaskItem.cshtml:**

```html
@model TaskItem

<li id="task-@Model.Id" class="list-group-item d-flex justify-content-between align-items-center">
    <div class="d-flex align-items-center gap-2">
        <button hx-post="/Tasks?handler=Toggle&amp;id=@Model.Id"
                hx-target="#task-@Model.Id"
                hx-swap="outerHTML"
                class="btn btn-sm @(Model.IsComplete ? "btn-success" : "btn-outline-secondary")">
            @(Model.IsComplete ? "✓" : "○")
        </button>
        <span class="@(Model.IsComplete ? "text-decoration-line-through text-muted" : "")">
            @Model.Name
        </span>
    </div>
    <button hx-delete="/Tasks?handler=Delete&amp;id=@Model.Id"
            hx-target="#task-@Model.Id"
            hx-swap="outerHTML"
            hx-confirm="Delete '@Model.Name'?"
            class="btn btn-sm btn-outline-danger">
        Delete
    </button>
</li>
```

**Pages/Shared/_TaskToggleResponse.cshtml:**

```html
@model TaskToggleResponse

<!-- Primary content: Updated task item -->
<partial name="_TaskItem" model="Model.Task" />

<!-- OOB: Update completion stats -->
<span id="completion-stats" hx-swap-oob="true">
    @Model.CompletedCount / @Model.TotalCount completed
</span>

<!-- OOB: Update progress bar -->
<div id="progress-bar" hx-swap-oob="outerHTML">
    <div id="progress-bar" 
         class="progress-bar @(Model.CompletionPercentage == 100 ? "bg-success" : "")" 
         style="width: @Model.CompletionPercentage%">
    </div>
</div>

<!-- OOB: Append toast notification -->
<div id="toast-container" hx-swap-oob="beforeend">
    <div class="toast show" role="alert">
        @Model.ToastMessage
        <button type="button" class="btn-close" onclick="this.parentElement.remove()"></button>
    </div>
</div>
```

**Pages/Shared/_TaskDeleteResponse.cshtml:**

```html
@model TaskDeleteResponse

<!-- Primary content: Empty (removes the task item) -->

<!-- OOB: Update completion stats -->
<span id="completion-stats" hx-swap-oob="true">
    @Model.CompletedCount / @Model.TotalCount completed
</span>

<!-- OOB: Update progress bar -->
<div id="progress-bar" hx-swap-oob="outerHTML">
    <div id="progress-bar" 
         class="progress-bar @(Model.CompletionPercentage == 100 ? "bg-success" : "")" 
         style="width: @Model.CompletionPercentage%">
    </div>
</div>

<!-- OOB: Append toast notification -->
<div id="toast-container" hx-swap-oob="beforeend">
    <div class="toast show" role="alert">
        @Model.ToastMessage
        <button type="button" class="btn-close" onclick="this.parentElement.remove()"></button>
    </div>
</div>
```

**Toast CSS:**

```css
.toast-container {
    position: fixed;
    bottom: 1rem;
    right: 1rem;
    z-index: 1050;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.toast {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 0.75rem 1rem;
    background: #333;
    color: white;
    border-radius: 4px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    animation: slideIn 0.3s ease;
}

@keyframes slideIn {
    from {
        transform: translateX(100%);
        opacity: 0;
    }
    to {
        transform: translateX(0);
        opacity: 1;
    }
}

.toast .btn-close {
    filter: invert(1);
}
```

## Shopping Cart with Multiple OOB Updates

A product page where adding to cart updates the cart count, shows confirmation, and updates stock display.

**Pages/Product.cshtml:**

```html
@page "{id:int}"
@model ProductPageModel

<header class="site-header">
    <nav>
        <a href="/">Home</a>
        <a href="/cart">
            Cart (<span id="cart-count">@Model.CartCount</span>)
        </a>
    </nav>
</header>

<main class="product-page">
    <div class="product-images">
        <img src="@Model.Product.ImageUrl" alt="@Model.Product.Name" />
    </div>
    
    <div class="product-info">
        <h1>@Model.Product.Name</h1>
        <p class="price">@Model.Product.Price.ToString("C")</p>
        <p class="description">@Model.Product.Description</p>
        
        <div id="stock-status" class="stock-info">
            @if (Model.Product.StockCount > 0)
            {
                <span class="in-stock">@Model.Product.StockCount in stock</span>
            }
            else
            {
                <span class="out-of-stock">Out of stock</span>
            }
        </div>
        
        <form hx-post="/Product/@Model.Product.Id?handler=AddToCart"
              hx-target="#add-to-cart-feedback"
              hx-swap="innerHTML"
              hx-disabled-elt="find button">
            @Html.AntiForgeryToken()
            
            <div class="quantity-selector">
                <label for="quantity">Quantity:</label>
                <input type="number" id="quantity" name="quantity" value="1" min="1" max="@Model.Product.StockCount" />
            </div>
            
            <button type="submit" 
                    class="btn btn-primary btn-lg"
                    @(Model.Product.StockCount == 0 ? "disabled" : "")>
                <span class="btn-text">Add to Cart</span>
                <span class="btn-loading htmx-indicator">Adding...</span>
            </button>
        </form>
        
        <div id="add-to-cart-feedback" class="feedback-area"></div>
    </div>
</main>

<div id="toast-container" class="toast-container"></div>
```

**Pages/Product.cshtml.cs:**

```csharp
public class ProductPageModel : PageModel
{
    private readonly IProductService _productService;
    private readonly ICartService _cartService;

    public ProductPageModel(IProductService productService, ICartService cartService)
    {
        _productService = productService;
        _cartService = cartService;
    }

    public Product Product { get; set; } = null!;
    public int CartCount { get; set; }

    public IActionResult OnGet(int id)
    {
        Product = _productService.GetById(id);
        if (Product == null) return NotFound();

        CartCount = _cartService.GetItemCount();
        return Page();
    }

    public IActionResult OnPostAddToCart(int id, int quantity = 1)
    {
        var product = _productService.GetById(id);
        if (product == null) return NotFound();

        if (product.StockCount < quantity)
        {
            return Partial("_AddToCartError", "Not enough stock available");
        }

        _cartService.Add(product.Id, quantity);
        _productService.DecrementStock(product.Id, quantity);

        // Reload product to get updated stock
        product = _productService.GetById(id);

        return Partial("_AddToCartSuccess", new AddToCartResponse
        {
            ProductName = product.Name,
            Quantity = quantity,
            NewStockCount = product.StockCount,
            CartCount = _cartService.GetItemCount()
        });
    }
}

public class AddToCartResponse
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public int NewStockCount { get; set; }
    public int CartCount { get; set; }
}
```

**Pages/Shared/_AddToCartSuccess.cshtml:**

```html
@model AddToCartResponse

<!-- Primary content: Success message -->
<div class="alert alert-success">
    Added @Model.Quantity x @Model.ProductName to cart!
</div>

<!-- OOB: Update cart count in header -->
<span id="cart-count" hx-swap-oob="true">@Model.CartCount</span>

<!-- OOB: Update stock display -->
<div id="stock-status" hx-swap-oob="true" class="stock-info">
    @if (Model.NewStockCount > 0)
    {
        <span class="in-stock">@Model.NewStockCount in stock</span>
    }
    else
    {
        <span class="out-of-stock">Out of stock</span>
    }
</div>

<!-- OOB: Show toast -->
<div id="toast-container" hx-swap-oob="beforeend">
    <div class="toast show">
        Added to cart!
        <button type="button" class="btn-close" onclick="this.parentElement.remove()"></button>
    </div>
</div>
```

## Using `hx-select-oob` on the Trigger

While `hx-swap-oob` goes in the response, `hx-select-oob` goes on the triggering element. It tells htmx which elements to treat as OOB from the response.

This is useful when you can't modify the server response but know it contains elements with IDs that should swap OOB:

```html
<button hx-get="/Dashboard?handler=Refresh"
        hx-target="#main-content"
        hx-select-oob="#notification-count,#user-status">
    Refresh Dashboard
</button>
```

The response might be:
```html
<div id="main-content">
    <!-- Main dashboard content -->
</div>
<span id="notification-count">5</span>
<span id="user-status">Online</span>
```

htmx swaps `#main-content` into the target, then processes `#notification-count` and `#user-status` as OOB swaps.

## Debugging OOB Swaps

When OOB swaps don't work, check these common issues:

### 1. Missing ID on Page Element

The OOB element must have a matching ID on the page:

```html
<!-- Response -->
<span id="cart-count" hx-swap-oob="true">5</span>

<!-- Page must have -->
<span id="cart-count">0</span>
```

### 2. ID Mismatch

IDs are case-sensitive:

```html
<!-- Response has "cartCount" -->
<span id="cartCount" hx-swap-oob="true">5</span>

<!-- Page has "cart-count" - NO MATCH -->
<span id="cart-count">0</span>
```

### 3. Element Not in DOM

If the target element is inside content that gets swapped first, it may not exist when OOB runs. Place OOB targets outside swap areas.

### 4. Using htmx Debug Mode

Enable debug logging:

```javascript
htmx.logAll();
```

Check the console for OOB processing messages.

## Accessibility Considerations

### Announce Dynamic Updates

Use `aria-live` for regions that update:

```html
<div id="toast-container" aria-live="polite" class="toast-container"></div>

<span id="cart-count" aria-live="polite">0</span>
```

### Maintain Focus

When updating content, avoid disrupting keyboard focus:

```html
<!-- Don't swap the focused element -->
<input id="search" name="query" />

<!-- Swap results separately -->
<div id="search-results">...</div>
```

### Screen Reader Feedback

For important updates, include screen-reader-only text:

```html
<div id="toast-container" hx-swap-oob="beforeend">
    <div class="toast" role="alert">
        <span class="visually-hidden">Success: </span>
        Item added to cart!
    </div>
</div>
```

## Summary

This chapter covered scoped updates with htmx:

- **`hx-select`** extracts specific content from responses using CSS selectors
- **`hx-swap-oob`** in responses marks elements for out-of-band swapping
- **`hx-select-oob`** on triggers specifies which response elements are OOB
- **OOB swap strategies** include `true`, `outerHTML`, `beforeend`, `afterbegin`
- **Multiple OOB elements** can update several page sections from one response
- **Toast notifications** append to a container using `hx-swap-oob="beforeend"`
- **Debugging** requires matching IDs and htmx debug logging

OOB swaps let your server tell the full story of what changed, updating every affected element in a single response. This keeps your UI consistent without client-side state management or multiple requests.

## Preview of Next Chapter

Chapter 17 covers advanced form techniques with `hx-params`, `hx-vals`, and `hx-validate`. You will learn to control which parameters get sent, inject computed values into requests, and integrate client-side validation with server-side processing.
