---
order: 16
icon: stack
label: Chap 15 - Visual Feedback with hx-indicator and hx-preserve
meta:
title: "Visual Feedback with hx-indicator and hx-preserve"
---

# Visual Feedback with `hx-indicator` and `hx-preserve`

Users hate uncertainty. When they click a button and nothing happens for 500 milliseconds, they wonder: Did it work? Should I click again? Is the page frozen? That half-second of silence erodes trust in your application. Visual feedback fills that gap. A spinner, a loading overlay, a subtle animation tells users "I heard you, I'm working on it."

htmx provides two attributes for managing visual feedback: `hx-indicator` shows loading states during requests, and `hx-preserve` protects specific elements from being replaced during swaps. This chapter covers both with complete, working examples that include proper CSS patterns, accessibility considerations, and common use cases like button spinners, table overlays, and skeleton loaders.

## How `hx-indicator` Works

When htmx sends a request, it adds the `htmx-request` CSS class to elements. By default, this class is added to the element that triggered the request. The `hx-indicator` attribute lets you specify additional elements that should receive this class during the request.

Here's the key insight: **htmx adds `htmx-request` directly to the indicator element**, not to a parent. Your CSS must account for this.

### Basic Indicator CSS Pattern

```css
/* Indicator starts hidden */
.htmx-indicator {
    display: none;
}

/* When htmx-request class is added, show the indicator */
.htmx-indicator.htmx-request {
    display: inline-block;
}
```

For indicators **inside** the triggering element, use a descendant selector:

```css
.htmx-indicator {
    display: none;
}

/* Show indicator when parent has htmx-request */
.htmx-request .htmx-indicator {
    display: inline-block;
}
```

### Fade Transitions

For smoother appearance, use opacity with transitions:

```css
.htmx-indicator {
    opacity: 0;
    transition: opacity 200ms ease-in-out;
}

.htmx-indicator.htmx-request {
    opacity: 1;
}
```

## Button Loading States

The most common indicator pattern shows a spinner inside a button during form submission.

### Button with Inline Spinner

**Pages/Tasks.cshtml:**

```html
@page
@model TasksModel

<h1>Tasks</h1>

<form hx-post="/Tasks?handler=Create"
      hx-target="#task-list"
      hx-swap="beforeend"
      hx-disabled-elt="find button"
      class="mb-4">
    @Html.AntiForgeryToken()
    
    <div class="input-group">
        <input type="text" name="name" class="form-control" placeholder="New task name" required />
        <button type="submit" class="btn btn-primary">
            <span class="btn-text">Add Task</span>
            <span class="btn-loading htmx-indicator">
                <span class="spinner-border spinner-border-sm"></span>
                Adding...
            </span>
        </button>
    </div>
</form>

<ul id="task-list" class="list-group">
    @foreach (var task in Model.Tasks)
    {
        <partial name="_TaskItem" model="task" />
    }
</ul>
```

**Button CSS:**

```css
/* Default state: show text, hide loading */
.btn-text {
    display: inline;
}

.btn-loading {
    display: none;
    align-items: center;
    gap: 0.5rem;
}

/* During request: hide text, show loading */
.htmx-request .btn-text {
    display: none;
}

.htmx-request .btn-loading {
    display: inline-flex;
}

/* Disabled appearance during request */
button.htmx-request,
.htmx-request button {
    opacity: 0.7;
    cursor: wait;
}
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

    public void OnGet()
    {
        Tasks = _taskService.GetAll();
    }

    public async Task<IActionResult> OnPostCreate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Response.StatusCode = 400;
            return Content("<li class=\"list-group-item text-danger\">Task name is required</li>", "text/html");
        }

        // Simulate slow operation to see the spinner
        await Task.Delay(500);

        var task = new TaskItem { Name = name };
        _taskService.Add(task);

        return Partial("_TaskItem", task);
    }
}
```

**Pages/Shared/_TaskItem.cshtml:**

```html
@model TaskItem

<li class="list-group-item d-flex justify-content-between align-items-center" id="task-@Model.Id">
    <span>@Model.Name</span>
    <button hx-delete="/Tasks?handler=Delete&amp;id=@Model.Id"
            hx-target="#task-@Model.Id"
            hx-swap="outerHTML"
            hx-confirm="Delete this task?"
            class="btn btn-sm btn-outline-danger">
        <span class="btn-text">Delete</span>
        <span class="btn-loading htmx-indicator">
            <span class="spinner-border spinner-border-sm"></span>
        </span>
    </button>
</li>
```

### Standalone Button Spinner

When you want the spinner next to the button rather than inside:

```html
<div class="d-flex align-items-center gap-2">
    <button hx-post="/Reports?handler=Generate"
            hx-target="#report-output"
            hx-indicator="#generate-spinner"
            hx-disabled-elt="this"
            class="btn btn-primary">
        Generate Report
    </button>
    <span id="generate-spinner" class="htmx-indicator">
        <span class="spinner-border spinner-border-sm"></span>
        Generating...
    </span>
</div>

<div id="report-output"></div>
```

```css
#generate-spinner {
    display: none;
    align-items: center;
    gap: 0.5rem;
    color: #6c757d;
}

#generate-spinner.htmx-request {
    display: inline-flex;
}
```

## Table and List Loading Overlays

For tables and lists that refresh data, overlay the entire container with a loading state.

### Table Overlay Pattern

**Pages/Products.cshtml:**

```html
@page
@model ProductsModel

<h1>Products</h1>

<div class="table-wrapper">
    <div id="table-overlay" class="loading-overlay">
        <div class="loading-content">
            <span class="spinner-border"></span>
            <span>Loading products...</span>
        </div>
    </div>

    <div class="table-controls mb-3">
        <input type="search" 
               name="search"
               placeholder="Search products..."
               hx-get="/Products?handler=Filter"
               hx-target="#product-tbody"
               hx-trigger="keyup changed delay:300ms"
               hx-indicator="#table-overlay"
               class="form-control" />
    </div>

    <table class="table table-hover">
        <thead>
            <tr>
                <th>
                    <a hx-get="/Products?handler=Sort&amp;sortBy=name"
                       hx-target="#product-tbody"
                       hx-indicator="#table-overlay">
                        Name
                    </a>
                </th>
                <th>
                    <a hx-get="/Products?handler=Sort&amp;sortBy=price"
                       hx-target="#product-tbody"
                       hx-indicator="#table-overlay">
                        Price
                    </a>
                </th>
                <th>Category</th>
            </tr>
        </thead>
        <tbody id="product-tbody">
            <partial name="_ProductRows" model="Model.Products" />
        </tbody>
    </table>
</div>
```

**Overlay CSS:**

```css
.table-wrapper {
    position: relative;
    min-height: 200px;
}

.loading-overlay {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(255, 255, 255, 0.85);
    display: none;
    align-items: center;
    justify-content: center;
    z-index: 10;
    border-radius: 4px;
}

.loading-overlay.htmx-request {
    display: flex;
}

.loading-content {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
    color: #495057;
}
```

**Pages/Products.cshtml.cs:**

```csharp
public class ProductsModel : PageModel
{
    private readonly IProductService _productService;

    public ProductsModel(IProductService productService)
    {
        _productService = productService;
    }

    public List<Product> Products { get; set; } = new();

    public void OnGet()
    {
        Products = _productService.GetAll();
    }

    public async Task<IActionResult> OnGetFilter(string search)
    {
        // Simulate network delay
        await Task.Delay(300);

        var products = string.IsNullOrEmpty(search)
            ? _productService.GetAll()
            : _productService.Search(search);

        return Partial("_ProductRows", products);
    }

    public async Task<IActionResult> OnGetSort(string sortBy)
    {
        await Task.Delay(200);

        var products = _productService.GetAll();
        products = sortBy switch
        {
            "price" => products.OrderBy(p => p.Price).ToList(),
            "name" => products.OrderBy(p => p.Name).ToList(),
            _ => products
        };

        return Partial("_ProductRows", products);
    }
}
```

**Pages/Shared/_ProductRows.cshtml:**

```html
@model List<Product>

@if (Model.Any())
{
    @foreach (var product in Model)
    {
        <tr>
            <td>@product.Name</td>
            <td>@product.Price.ToString("C")</td>
            <td>@product.Category</td>
        </tr>
    }
}
else
{
    <tr>
        <td colspan="3" class="text-center text-muted py-4">
            No products found.
        </td>
    </tr>
}
```

## Skeleton Loaders

Skeleton loaders show placeholder shapes that mimic the content layout. They feel faster than spinners because they set expectations about what's coming.

### Profile Card Skeleton

```html
<div id="user-profile"
     hx-get="/Profile?handler=Card"
     hx-trigger="load"
     hx-swap="innerHTML">
    <!-- Skeleton shown while loading -->
    <div class="profile-skeleton">
        <div class="skeleton-avatar"></div>
        <div class="skeleton-details">
            <div class="skeleton-line skeleton-name"></div>
            <div class="skeleton-line skeleton-email"></div>
            <div class="skeleton-line skeleton-short"></div>
        </div>
    </div>
</div>
```

**Skeleton CSS:**

```css
.profile-skeleton {
    display: flex;
    gap: 1rem;
    padding: 1rem;
}

.skeleton-avatar {
    width: 80px;
    height: 80px;
    border-radius: 50%;
    background: linear-gradient(90deg, #f0f0f0 25%, #e8e8e8 50%, #f0f0f0 75%);
    background-size: 200% 100%;
    animation: skeleton-shimmer 1.5s infinite;
}

.skeleton-details {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.skeleton-line {
    height: 1rem;
    border-radius: 4px;
    background: linear-gradient(90deg, #f0f0f0 25%, #e8e8e8 50%, #f0f0f0 75%);
    background-size: 200% 100%;
    animation: skeleton-shimmer 1.5s infinite;
}

.skeleton-name {
    width: 60%;
    height: 1.25rem;
}

.skeleton-email {
    width: 80%;
}

.skeleton-short {
    width: 40%;
}

@keyframes skeleton-shimmer {
    0% {
        background-position: 200% 0;
    }
    100% {
        background-position: -200% 0;
    }
}
```

**Server handler:**

```csharp
public async Task<IActionResult> OnGetCard()
{
    // Simulate data fetch
    await Task.Delay(800);

    var profile = _profileService.GetCurrentUser();
    return Partial("_ProfileCard", profile);
}
```

**Pages/Shared/_ProfileCard.cshtml:**

```html
@model UserProfile

<div class="profile-card">
    <img src="@Model.AvatarUrl" alt="@Model.Name" class="profile-avatar" />
    <div class="profile-details">
        <h3 class="profile-name">@Model.Name</h3>
        <p class="profile-email">@Model.Email</p>
        <p class="profile-role">@Model.Role</p>
    </div>
</div>
```

### Table Skeleton

```html
<table class="table">
    <thead>
        <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Status</th>
        </tr>
    </thead>
    <tbody id="users-tbody"
           hx-get="/Users?handler=List"
           hx-trigger="load"
           hx-swap="innerHTML">
        <!-- Skeleton rows while loading -->
        @for (int i = 0; i < 5; i++)
        {
            <tr class="skeleton-row">
                <td><div class="skeleton-cell" style="width: 60%"></div></td>
                <td><div class="skeleton-cell" style="width: 80%"></div></td>
                <td><div class="skeleton-cell" style="width: 40%"></div></td>
            </tr>
        }
    </tbody>
</table>
```

```css
.skeleton-cell {
    height: 1rem;
    border-radius: 4px;
    background: linear-gradient(90deg, #f0f0f0 25%, #e8e8e8 50%, #f0f0f0 75%);
    background-size: 200% 100%;
    animation: skeleton-shimmer 1.5s infinite;
}
```

## Understanding `hx-preserve`

The `hx-preserve` attribute prevents an element from being replaced during a swap. This is critical to understand: **the element is never updated, even if the server sends new content for it.**

### When to Use `hx-preserve`

Use `hx-preserve` for elements that:
- Would break or reset if replaced (video players, audio players)
- Contain third-party widgets that initialize once (chat widgets, maps)
- Have complex client-side state that can't be recreated

### Video Player Example

```html
<div id="page-content"
     hx-get="/Videos?handler=Content"
     hx-trigger="revealed">
    
    <!-- This video player will NOT be replaced during swaps -->
    <div id="video-player" hx-preserve>
        <video controls>
            <source src="/videos/intro.mp4" type="video/mp4">
        </video>
    </div>
    
    <!-- This content WILL be replaced -->
    <div id="video-info">
        <h2>@Model.Title</h2>
        <p>@Model.Description</p>
    </div>
</div>
```

If htmx swaps `#page-content`, the video player keeps playing while the video info updates.

### Third-Party Widget

```html
<div class="dashboard">
    <!-- Chat widget initializes once and should never be replaced -->
    <div id="intercom-container" hx-preserve>
        <!-- Intercom widget loads here -->
    </div>
    
    <!-- Dashboard content can refresh -->
    <div id="dashboard-content"
         hx-get="/Dashboard?handler=Refresh"
         hx-trigger="every 30s">
        <partial name="_DashboardStats" model="Model.Stats" />
    </div>
</div>
```

### When NOT to Use `hx-preserve`

Do not use `hx-preserve` for:
- Form inputs you want to validate (server can't update them)
- Content that should refresh from the server
- Elements that need server-side updates

**Wrong approach for forms:**

```html
<!-- DON'T DO THIS - inputs will never receive validation messages -->
<form hx-post="/Contact?handler=Submit" hx-target="this" hx-swap="outerHTML">
    <input name="email" hx-preserve />  <!-- WRONG -->
    <span class="error"></span>
</form>
```

**Correct approach for forms:**

```html
<!-- Target only the results, not the form -->
<form hx-post="/Contact?handler=Submit" hx-target="#result">
    @Html.AntiForgeryToken()
    <input name="email" />
    <button type="submit">Submit</button>
</form>
<div id="result"></div>
```

Or use `hx-swap-oob` to update specific elements:

```html
<form hx-post="/Contact?handler=Submit" hx-target="#result">
    @Html.AntiForgeryToken()
    <input name="email" id="email-input" />
    <span id="email-error"></span>
    <button type="submit">Submit</button>
</form>
```

Server response can include OOB update for error:

```html
<div id="result">Success!</div>
<span id="email-error" hx-swap-oob="true" class="text-danger">Invalid email</span>
```

## Accessibility

Loading indicators must work for all users, including those using screen readers.

### ARIA Attributes for Loading States

```html
<div id="search-results" 
     aria-live="polite"
     aria-busy="false">
    <!-- Results here -->
</div>

<button hx-get="/Search?handler=Results"
        hx-target="#search-results"
        hx-indicator="#search-spinner"
        _="on htmx:beforeRequest set #search-results[@aria-busy] to 'true'
           on htmx:afterRequest set #search-results[@aria-busy] to 'false'">
    Search
</button>

<span id="search-spinner" class="htmx-indicator" role="status">
    <span class="spinner-border spinner-border-sm"></span>
    <span class="visually-hidden">Loading results...</span>
</span>
```

Key accessibility patterns:
- `aria-live="polite"` announces content changes to screen readers
- `aria-busy="true"` during loading tells assistive technology content is updating
- `role="status"` on spinners makes them announce to screen readers
- `visually-hidden` class provides text for screen readers without visual display

### Screen Reader CSS

```css
.visually-hidden {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border: 0;
}
```

## Complete Example: Search with All Patterns

**Pages/Search.cshtml:**

```html
@page
@model SearchModel

<h1>Product Search</h1>

<div class="search-container">
    <form hx-get="/Search?handler=Results"
          hx-target="#search-results"
          hx-indicator="#search-indicator"
          hx-trigger="submit, keyup changed delay:300ms from:#search-query">
        
        <div class="input-group mb-3">
            <input type="search" 
                   id="search-query"
                   name="query" 
                   value="@Model.Query"
                   class="form-control form-control-lg" 
                   placeholder="Search products..."
                   aria-label="Search products" />
            <button type="submit" class="btn btn-primary">
                <span class="btn-text">Search</span>
                <span class="btn-loading htmx-indicator">
                    <span class="spinner-border spinner-border-sm"></span>
                </span>
            </button>
        </div>
    </form>

    <div id="search-indicator" class="search-status htmx-indicator">
        <span class="spinner-border spinner-border-sm"></span>
        Searching...
    </div>

    <div id="search-results" 
         aria-live="polite"
         aria-busy="false"
         class="results-container">
        @if (Model.Results.Any())
        {
            <partial name="_SearchResults" model="Model.Results" />
        }
        else if (!string.IsNullOrEmpty(Model.Query))
        {
            <p class="text-muted">No products found for "@Model.Query"</p>
        }
        else
        {
            <p class="text-muted">Enter a search term to find products.</p>
        }
    </div>
</div>
```

**Pages/Search.cshtml.cs:**

```csharp
public class SearchModel : PageModel
{
    private readonly IProductService _productService;

    public SearchModel(IProductService productService)
    {
        _productService = productService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    public List<Product> Results { get; set; } = new();

    public void OnGet()
    {
        if (!string.IsNullOrEmpty(Query))
        {
            Results = _productService.Search(Query);
        }
    }

    public async Task<IActionResult> OnGetResults(string? query)
    {
        Query = query;

        // Simulate search delay
        await Task.Delay(300);

        if (string.IsNullOrEmpty(query))
        {
            return Content("<p class=\"text-muted\">Enter a search term to find products.</p>", "text/html");
        }

        Results = _productService.Search(query);

        if (!Results.Any())
        {
            return Content($"<p class=\"text-muted\">No products found for \"{query}\"</p>", "text/html");
        }

        return Partial("_SearchResults", Results);
    }
}
```

**Pages/Shared/_SearchResults.cshtml:**

```html
@model List<Product>

<p class="results-count">Found @Model.Count product(s)</p>

<div class="product-grid">
    @foreach (var product in Model)
    {
        <div class="product-card">
            <img src="@product.ImageUrl" alt="@product.Name" />
            <h3>@product.Name</h3>
            <p class="price">@product.Price.ToString("C")</p>
            <button hx-post="/Cart?handler=Add&amp;productId=@product.Id"
                    hx-target="#cart-count"
                    hx-swap="innerHTML"
                    class="btn btn-sm btn-outline-primary">
                <span class="btn-text">Add to Cart</span>
                <span class="btn-loading htmx-indicator">
                    <span class="spinner-border spinner-border-sm"></span>
                </span>
            </button>
        </div>
    }
</div>
```

**Complete CSS:**

```css
/* Button loading states */
.btn-text {
    display: inline;
}

.btn-loading {
    display: none;
    align-items: center;
    gap: 0.25rem;
}

.htmx-request .btn-text {
    display: none;
}

.htmx-request .btn-loading {
    display: inline-flex;
}

/* Search status indicator */
.search-status {
    display: none;
    align-items: center;
    gap: 0.5rem;
    color: #6c757d;
    font-size: 0.875rem;
    margin-bottom: 1rem;
}

.search-status.htmx-request {
    display: flex;
}

/* Results container fade during loading */
.results-container {
    transition: opacity 150ms ease;
}

.htmx-request ~ .results-container,
.htmx-request .results-container {
    opacity: 0.5;
}

/* Product grid */
.product-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 1rem;
}

.product-card {
    border: 1px solid #dee2e6;
    border-radius: 8px;
    padding: 1rem;
    text-align: center;
}

.product-card img {
    max-width: 100%;
    height: 150px;
    object-fit: contain;
}
```

## Summary

This chapter covered visual feedback with htmx:

- **`hx-indicator`** specifies elements that receive the `htmx-request` class during requests
- **CSS patterns** differ based on indicator placement (inside vs. outside trigger)
- **Button spinners** replace button text with loading state during submission
- **Table overlays** cover content with semi-transparent loading indicators
- **Skeleton loaders** show placeholder shapes that mimic expected content
- **`hx-preserve`** prevents elements from being replaced during swaps (use for video players, widgets)
- **Accessibility** requires ARIA attributes (`aria-busy`, `aria-live`, `role="status"`)

Visual feedback makes the difference between an application that feels broken and one that feels responsive. Users should never wonder if their action was received.

## Preview of Next Chapter

Chapter 16 covers `hx-select` and `hx-select-oob` for precise content selection. You will learn to extract specific elements from server responses, update multiple page sections with a single request, and build notification systems that update independently from main content.
