---
order: 21
icon: stack
label: Chap 10 - Navigating with hx-push-url and hx-replace-url
meta:
title: "Navigating with hx-push-url and hx-replace-url"
---

# Navigating with `hx-push-url` and `hx-replace-url`

htmx requests update content without page reloads. That speed comes with a cost: the browser's address bar stays frozen. Users cannot bookmark the current view. The back button does nothing useful. Shared links land on the wrong content. These problems make htmx applications feel broken despite their responsiveness.

The `hx-push-url` and `hx-replace-url` attributes solve this. They synchronize the browser's address bar with your dynamic content, enabling bookmarks, shareable links, and proper back/forward navigation. Your application gains single-page-app responsiveness while keeping multi-page-app navigation behavior.

This chapter covers both attributes in depth, shows when to use each, and demonstrates complete working examples with Razor Pages. You will learn to handle the critical server-side logic that makes URL-driven navigation work for both htmx requests and direct page loads.

## Understanding Browser History

Before diving into htmx attributes, understand how browser history works.

The browser maintains a history stack. Each navigation pushes a new entry onto the stack. The back button pops entries off. The forward button moves through entries you've backed past.

**Push** adds a new entry:
```
[Page A] -> [Page B] -> [Page C]  (user is at C)
Back button goes to B, then A
```

**Replace** modifies the current entry without adding:
```
[Page A] -> [Page B]  (user is at B)
Replace with C:
[Page A] -> [Page C]  (user is still at "top", but it's now C)
Back button goes to A (B is gone)
```

htmx gives you control over which behavior to use for each interaction.

## The `hx-push-url` Attribute

The `hx-push-url` attribute pushes a new URL onto the browser's history stack when the htmx request completes successfully. This creates a navigation trail users can traverse with back/forward buttons.

### Basic Usage

```html
<a href="/Blog?handler=Post&amp;slug=intro-to-htmx"
   hx-get="/Blog?handler=Post&amp;slug=intro-to-htmx"
   hx-target="#content"
   hx-push-url="true">
    Introduction to htmx
</a>

<div id="content">
    <!-- Post content loads here -->
</div>
```

When clicked:
1. htmx sends a GET request to the handler
2. The response swaps into `#content`
3. The browser's address bar updates to the URL
4. A new entry is added to the history stack

The `href` attribute ensures the link works without JavaScript. The `hx-get` tells htmx to intercept the click and handle it dynamically.

### Custom URLs

You can push a different URL than the request URL:

```html
<button hx-get="/Search?handler=Execute&amp;q=htmx"
        hx-target="#results"
        hx-push-url="/search/htmx">
    Search for htmx
</button>
```

The request goes to the handler with query parameters, but the browser shows the cleaner URL `/search/htmx`.

### When to Use `hx-push-url`

Use `hx-push-url` when:
- Users should be able to bookmark the current view
- The back button should return to the previous view
- Links should be shareable
- The view represents a distinct "page" or "state"

Examples:
- Tab navigation (each tab is a bookmarkable view)
- Blog post links
- Search results pages
- Product detail views
- Dashboard sections

## The `hx-replace-url` Attribute

The `hx-replace-url` attribute updates the browser's address bar without adding a history entry. The current entry is replaced.

### Basic Usage

```html
<select hx-get="/Products?handler=Filter"
        hx-target="#product-list"
        hx-replace-url="true"
        name="category">
    <option value="">All Categories</option>
    <option value="electronics">Electronics</option>
    <option value="clothing">Clothing</option>
</select>
```

When the selection changes:
1. htmx sends a GET request with the selected value
2. The response swaps into `#product-list`
3. The browser's address bar updates
4. The current history entry is replaced (no new entry added)

### When to Use `hx-replace-url`

Use `hx-replace-url` when:
- The URL should reflect current state for bookmarking
- But the back button should skip this intermediate state
- The change is a refinement, not a navigation

Examples:
- Filter selections (sort, category, date range)
- Pagination within a list
- Form steps in a wizard (back should exit the wizard, not go to previous step)
- Modal content changes
- Auto-save states

## Configuring History Restoration

For back/forward navigation to work properly, htmx needs to know which element contains the restorable content. Add `hx-history-elt` to the container that holds your dynamic content:

```html
<body>
    <nav>
        <!-- Navigation stays constant -->
    </nav>
    
    <main id="main-content" hx-history-elt>
        <!-- This content changes and should be restored -->
    </main>
    
    <footer>
        <!-- Footer stays constant -->
    </footer>
</body>
```

When the user clicks back, htmx:
1. Intercepts the browser's popstate event
2. Checks its history cache for the previous URL's content
3. Restores the cached content to the `hx-history-elt` element
4. If not cached, makes a fresh request to the URL

Without `hx-history-elt`, back button behavior is unpredictable. Add it to the outermost container that changes during navigation.

## Handling Direct vs htmx Requests

Here's the critical piece many tutorials miss: your server must handle both types of requests.

**htmx request**: User clicks a link with `hx-get`. Server should return partial content only.

**Direct request**: User types the URL, refreshes the page, or clicks a shared link. Server should return the complete page with layout.

### Detecting htmx Requests

htmx adds an `HX-Request` header to all requests. Check for it in your PageModel:

```csharp
public class BlogModel : PageModel
{
    private readonly IBlogService _blogService;

    public BlogModel(IBlogService blogService)
    {
        _blogService = blogService;
    }

    public BlogPost? CurrentPost { get; set; }
    public List<BlogPost> RecentPosts { get; set; } = new();

    public IActionResult OnGet()
    {
        RecentPosts = _blogService.GetRecent(10);
        return Page();
    }

    public IActionResult OnGetPost(string slug)
    {
        var post = _blogService.GetBySlug(slug);
        if (post == null) return NotFound();

        // htmx request - return partial only
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return Partial("_BlogPost", post);
        }

        // Direct request - return full page
        CurrentPost = post;
        RecentPosts = _blogService.GetRecent(10);
        return Page();
    }
}
```

### The Razor Page View

```html
@page
@model BlogModel

<div class="blog-layout">
    <aside class="sidebar">
        <h3>Recent Posts</h3>
        <ul>
            @foreach (var post in Model.RecentPosts)
            {
                <li>
                    <a href="/Blog?handler=Post&amp;slug=@post.Slug"
                       hx-get="/Blog?handler=Post&amp;slug=@post.Slug"
                       hx-target="#blog-content"
                       hx-push-url="true">
                        @post.Title
                    </a>
                </li>
            }
        </ul>
    </aside>

    <main id="blog-content" hx-history-elt>
        @if (Model.CurrentPost != null)
        {
            <partial name="_BlogPost" model="Model.CurrentPost" />
        }
        else
        {
            <h1>Welcome to the Blog</h1>
            <p>Select a post from the sidebar.</p>
        }
    </main>
</div>
```

### The Partial View

**Pages/Shared/_BlogPost.cshtml:**

```html
@model BlogPost

<article>
    <h1>@Model.Title</h1>
    <time datetime="@Model.PublishedAt.ToString("yyyy-MM-dd")">
        @Model.PublishedAt.ToString("MMMM d, yyyy")
    </time>
    <div class="content">
        @Html.Raw(Model.Content)
    </div>
</article>
```

This setup works for:
- htmx clicks (returns partial, pushes URL)
- Direct URL access (returns full page)
- Page refresh (returns full page)
- Shared/bookmarked links (returns full page)
- Back button (restores from htmx cache or fetches fresh)

## Using `hx-boost` for Simpler Navigation

If you want all links in a section to behave like htmx links with `hx-push-url`, use `hx-boost` instead of adding attributes to every link:

```html
<nav hx-boost="true" hx-target="#main-content" hx-swap="innerHTML">
    <a href="/Dashboard">Dashboard</a>
    <a href="/Profile">Profile</a>
    <a href="/Settings">Settings</a>
</nav>

<main id="main-content" hx-history-elt>
    <!-- Content loads here -->
</main>
```

With `hx-boost="true"`:
- All links and forms in that container automatically use htmx
- URLs are automatically pushed to history
- Links degrade gracefully without JavaScript

This is often the simplest approach for traditional navigation patterns.

### Excluding Links from Boost

Add `hx-boost="false"` to links that should navigate normally:

```html
<nav hx-boost="true">
    <a href="/Dashboard">Dashboard</a>
    <a href="/Profile">Profile</a>
    <a href="/logout" hx-boost="false">Logout</a> <!-- Full page navigation -->
</nav>
```

## Complete Example: Tabbed Interface

Here's a complete working example of a tabbed profile page:

### PageModel

```csharp
public class ProfileModel : PageModel
{
    private readonly IUserService _userService;

    public ProfileModel(IUserService userService)
    {
        _userService = userService;
    }

    public string ActiveTab { get; set; } = "overview";
    public UserProfile? Profile { get; set; }
    public List<UserPost>? Posts { get; set; }
    public UserSettings? Settings { get; set; }

    public IActionResult OnGet(string tab = "overview")
    {
        ActiveTab = tab;
        
        // Load data based on active tab
        switch (tab)
        {
            case "posts":
                Posts = _userService.GetUserPosts(User.Identity.Name);
                break;
            case "settings":
                Settings = _userService.GetSettings(User.Identity.Name);
                break;
            default:
                Profile = _userService.GetProfile(User.Identity.Name);
                break;
        }

        // htmx request - return partial only
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return tab switch
            {
                "posts" => Partial("_ProfilePosts", Posts),
                "settings" => Partial("_ProfileSettings", Settings),
                _ => Partial("_ProfileOverview", Profile)
            };
        }

        // Direct request - return full page
        return Page();
    }
}
```

### Razor Page

```html
@page
@model ProfileModel

<div class="profile-page">
    <nav class="tabs">
        <a href="/Profile?tab=overview"
           hx-get="/Profile?tab=overview"
           hx-target="#tab-content"
           hx-push-url="true"
           class="@(Model.ActiveTab == "overview" ? "active" : "")">
            Overview
        </a>
        <a href="/Profile?tab=posts"
           hx-get="/Profile?tab=posts"
           hx-target="#tab-content"
           hx-push-url="true"
           class="@(Model.ActiveTab == "posts" ? "active" : "")">
            Posts
        </a>
        <a href="/Profile?tab=settings"
           hx-get="/Profile?tab=settings"
           hx-target="#tab-content"
           hx-push-url="true"
           class="@(Model.ActiveTab == "settings" ? "active" : "")">
            Settings
        </a>
    </nav>

    <div id="tab-content" hx-history-elt>
        @switch (Model.ActiveTab)
        {
            case "posts":
                <partial name="_ProfilePosts" model="Model.Posts" />
                break;
            case "settings":
                <partial name="_ProfileSettings" model="Model.Settings" />
                break;
            default:
                <partial name="_ProfileOverview" model="Model.Profile" />
                break;
        }
    </div>
</div>
```

### Updating Active Tab Styling

When htmx loads content, the tab styling needs to update. Use `hx-on` or handle the `htmx:afterSwap` event:

```html
<nav class="tabs" 
     hx-on::after-request="updateActiveTabs(event)">
    <!-- tabs -->
</nav>

<script>
function updateActiveTabs(event) {
    var url = new URL(event.detail.pathInfo.requestPath, window.location.origin);
    var tab = url.searchParams.get('tab') || 'overview';
    
    document.querySelectorAll('.tabs a').forEach(function(link) {
        var linkUrl = new URL(link.href);
        var linkTab = linkUrl.searchParams.get('tab') || 'overview';
        link.classList.toggle('active', linkTab === tab);
    });
}
</script>
```

## Complete Example: Filterable List

A product list with category filtering and pagination:

### PageModel

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
    public List<string> Categories { get; set; } = new();
    public string? SelectedCategory { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }

    public IActionResult OnGet(string? category = null, int page = 1)
    {
        SelectedCategory = category;
        CurrentPage = page;
        Categories = _productService.GetCategories();

        var query = string.IsNullOrEmpty(category)
            ? _productService.GetAll()
            : _productService.GetByCategory(category);

        TotalPages = (int)Math.Ceiling(query.Count() / (double)PageSize);
        Products = query.Skip((page - 1) * PageSize).Take(PageSize).ToList();

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return Partial("_ProductGrid", this);
        }

        return Page();
    }
}
```

### Razor Page

```html
@page
@model ProductsModel

<div class="products-page">
    <aside class="filters">
        <h3>Categories</h3>
        <ul hx-target="#product-grid" hx-push-url="true">
            <li>
                <a href="/Products"
                   hx-get="/Products"
                   class="@(Model.SelectedCategory == null ? "active" : "")">
                    All Products
                </a>
            </li>
            @foreach (var category in Model.Categories)
            {
                <li>
                    <a href="/Products?category=@category"
                       hx-get="/Products?category=@category"
                       class="@(Model.SelectedCategory == category ? "active" : "")">
                        @category
                    </a>
                </li>
            }
        </ul>
    </aside>

    <main id="product-grid" hx-history-elt>
        <partial name="_ProductGrid" model="Model" />
    </main>
</div>
```

### Product Grid Partial

**Pages/Shared/_ProductGrid.cshtml:**

```html
@model ProductsModel

<div class="product-grid">
    @foreach (var product in Model.Products)
    {
        <div class="product-card">
            <img src="@product.ImageUrl" alt="@product.Name" />
            <h3>@product.Name</h3>
            <p class="price">@product.Price.ToString("C")</p>
        </div>
    }
</div>

@if (Model.TotalPages > 1)
{
    <nav class="pagination">
        @for (int i = 1; i <= Model.TotalPages; i++)
        {
            var url = string.IsNullOrEmpty(Model.SelectedCategory)
                ? $"/Products?page={i}"
                : $"/Products?category={Model.SelectedCategory}&page={i}";
            
            <a href="@url"
               hx-get="@url"
               hx-target="#product-grid"
               hx-replace-url="true"
               class="@(i == Model.CurrentPage ? "active" : "")">
                @i
            </a>
        }
    </nav>
}
```

Note that category links use `hx-push-url` (user might want to go back to previous category), while pagination uses `hx-replace-url` (user probably doesn't want to back through every page).

## History Events

htmx fires events during history operations that you can listen for:

```html
<script>
// Fires when content is restored from history cache
document.body.addEventListener('htmx:historyRestore', function(event) {
    console.log('Restored:', event.detail.path);
    // Re-initialize any JavaScript components
    initializeComponents();
});

// Fires when history cache doesn't have the content
document.body.addEventListener('htmx:historyCacheMiss', function(event) {
    console.log('Cache miss, fetching:', event.detail.path);
});

// Fires after history cache is updated
document.body.addEventListener('htmx:historyCacheUpdated', function(event) {
    console.log('Cache updated for:', event.detail.path);
});

// Fires before htmx replaces content during history navigation
document.body.addEventListener('htmx:beforeHistoryUpdate', function(event) {
    console.log('About to update history');
});
</script>
```

### Re-initializing Components After History Restore

If your pages use JavaScript components that need initialization, handle `htmx:historyRestore`:

```html
<script>
document.body.addEventListener('htmx:historyRestore', function(event) {
    // Re-initialize dropdowns, tooltips, etc.
    initDropdowns();
    initTooltips();
});
</script>
```

## Common Issues and Solutions

### Back Button Does Nothing

**Cause**: Missing `hx-history-elt` attribute.

**Solution**: Add `hx-history-elt` to the container that holds changing content:

```html
<main id="content" hx-history-elt>
    <!-- dynamic content -->
</main>
```

### Direct URL Access Shows Partial Content

**Cause**: Server returns partial for all requests.

**Solution**: Check for `HX-Request` header:

```csharp
if (Request.Headers.ContainsKey("HX-Request"))
{
    return Partial("_Content", data);
}
return Page();
```

### URL Updates But Content Doesn't Load on Refresh

**Cause**: Server doesn't handle the URL parameters on direct requests.

**Solution**: Ensure your `OnGet` method loads the correct content based on URL parameters for both htmx and direct requests.

### History Grows Too Fast

**Cause**: Using `hx-push-url` for operations that should use `hx-replace-url`.

**Solution**: Use `hx-replace-url` for filters, sorts, pagination, and other refinements. Use `hx-push-url` only for distinct views.

## Summary

This chapter covered URL management with htmx:

- **`hx-push-url`** adds entries to browser history (for bookmarkable, distinct views)
- **`hx-replace-url`** modifies the current entry (for refinements like filtering)
- **`hx-history-elt`** tells htmx which container to restore on back/forward
- **`hx-boost`** automatically applies htmx with push-url to all links in a container
- **Server-side handling** must return partials for htmx requests and full pages for direct requests
- **History events** let you respond to navigation and re-initialize components

These tools let you build applications that feel like single-page apps while maintaining the bookmarkability and shareability of traditional multi-page applications.

## Preview of Next Chapter

Chapter 11 covers building reusable HTML components with htmx. You will learn to create forms that submit without page reloads, modal dialogs that load content dynamically, and component patterns that combine the techniques from previous chapters into cohesive, reusable building blocks.
