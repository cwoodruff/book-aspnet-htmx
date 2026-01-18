---
order: 13
icon: stack
label: Chap 18 - Caching and History with hx-history and hx-history-elt
meta:
title: "Caching and History with hx-history and hx-history-elt"
---

# Caching and History with `hx-history` and `hx-history-elt`

The fastest request is the one you never make. If you have ever hit the back button on a web app and watched it reload a page you visited seconds ago, you know the frustration. That reload breaks your flow, wastes your time, and makes the app feel sluggish. This chapter shows you how htmx eliminates that pain by caching responses in the browser's history stack. With minimal effort, your Razor Pages can feel as fluid as a single-page app without the JavaScript complexity.

You have already seen how htmx handles form submissions, dynamic interactions, and server-driven updates. Now you can give users a smoother navigation experience by preserving state across page transitions. These features do more than improve performance. They let users move backward and forward through your app without losing their place or waiting for redundant server calls. This builds naturally on the scoped updates, real-time interactions, and Hyperscript techniques from earlier chapters.

We will start by examining how htmx manages browser history and caching. You will learn how to control caching behavior for specific elements, reducing server load while making your app feel more responsive. A content-heavy dashboard benefits from caching just as much as a multi-step workflow does. In both cases, intelligent caching dramatically improves the user experience.

By the end of this chapter, you will have the tools to create state-aware applications that feel fast and modern while keeping the simplicity of Razor Pages. Let us see how htmx puts the browser's built-in navigation features to work for you.

## Understanding htmx History Caching

Before we look at specific attributes and code, you need to understand how htmx handles history. When you use `hx-push-url="true"` on a request, htmx automatically does two things: it updates the browser's URL, and it saves a snapshot of the current page content to a local cache. This cache lives in the browser's `localStorage` and holds the HTML content that was on the page before the navigation occurred.

When a user clicks the back button, htmx intercepts that navigation. Instead of making a new server request, it pulls the cached HTML from `localStorage` and restores it to the page. This restoration happens almost instantly because no network round-trip is required.

The key insight here is that htmx caches by default when you push URLs. The `hx-history` attribute exists primarily to opt out of this behavior, not to opt in. Setting `hx-history="false"` tells htmx to skip caching for that particular element or request. This becomes important when you have sensitive content like personal information, checkout flows, or admin panels that should not persist in the browser's cache.

You can control how many pages htmx keeps in its history cache through configuration:

```javascript
htmx.config.historyCacheSize = 10; // Default is 10 pages
```

Setting this to zero disables history caching entirely. For most applications, the default works well, but you might increase it for apps where users frequently navigate back through many pages.

## Making Back Feel Fast Again: History Management with `hx-history`

In traditional server-rendered apps, clicking the back button typically reloads the entire page. Users lose form inputs, search results disappear, and any scroll position resets. This behavior is so common that many users avoid the back button altogether. With htmx, you can reclaim the back button and make it work the way users expect.

When `hx-push-url="true"` is set on a request, htmx stores the current page content before swapping in the new response. If the user navigates back, htmx restores that stored content without hitting the server. The result feels instant.

Consider a user searching for products in your app. They browse results, click into a product detail page, then hit the back button expecting to see their search results. Without htmx history caching, the page reloads and the results vanish. With `hx-push-url="true"`, the result list reappears immediately, exactly as they left it. No server call. No loading spinner. No frustration.

Here is a search form on a `Search.cshtml` page that demonstrates this pattern:

```html
<form hx-get="/Search?handler=Results"
      hx-target="#search-results"
      hx-push-url="true"
      hx-swap="innerHTML">
    <input type="text" name="query" placeholder="Search products..." />
    <button type="submit">Search</button>
</form>

<div id="search-results"></div>
```

The `hx-push-url="true"` attribute handles the caching work. It updates the browser URL with the search query and triggers htmx to cache the page state. When the user navigates away and returns, htmx restores the cached results.

On the server side, the handler returns a partial view:

```csharp
public class SearchModel : PageModel
{
    private readonly IProductRepository _productRepository;

    public SearchModel(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public IActionResult OnGetResults(string query)
    {
        var products = _productRepository.Search(query);
        return Partial("_SearchResults", products);
    }
}
```

The `_SearchResults.cshtml` partial renders the product list:

```html
@model IEnumerable<Product>

<ul>
    @foreach (var product in Model)
    {
        <li>
            <a href="/Products/Details?id=@product.Id"
               hx-get="/Products/Details?id=@product.Id"
               hx-target="#main-content"
               hx-push-url="true">
                @product.Name
            </a>
        </li>
    }
</ul>
```

Notice that the product links use `hx-target="#main-content"` rather than targeting the body. This scoped approach gives you more control over what gets replaced and avoids potential issues with scripts or styles in the document head.

When should you disable history caching? Use `hx-history="false"` in these situations:

- Checkout or payment pages where sensitive data should not persist
- User profile or account settings pages
- Admin panels with privileged information
- Any content that becomes stale quickly and must be fresh on every view

```html
<div hx-get="/Checkout?handler=Summary"
     hx-target="#checkout-content"
     hx-push-url="true"
     hx-history="false">
    <!-- Checkout content should never be cached -->
</div>
```

This pattern keeps secure content out of the browser's localStorage while still updating the URL for bookmarking purposes.

## Locking In the Experience: Retaining State with `hx-history-elt`

The `hx-history-elt` attribute solves a specific problem: which element's innerHTML should htmx snapshot when saving to the history cache? By default, htmx snapshots the body element. This works for simple pages, but complex layouts often need more precision.

Place `hx-history-elt` on the container element whose content represents the "main" state of your page. When htmx takes a history snapshot, it captures the innerHTML of this element. When the user navigates back, htmx restores that specific content.

Consider a dashboard with a main content area and persistent navigation:

```html
<body>
    <nav id="sidebar">
        <!-- Navigation stays constant -->
    </nav>
    
    <main id="app-content" hx-history-elt>
        <!-- This content gets cached and restored -->
    </main>
    
    <aside id="notifications">
        <!-- Live content that should not be cached -->
    </aside>
</body>
```

With `hx-history-elt` on `#app-content`, htmx only caches the main content area. The sidebar stays untouched, and the notifications panel can update independently without being overwritten by stale cached data.

Here is a tabbed interface that uses this pattern:

```html
<div id="dashboard-content" hx-history-elt>
    <div class="tab-buttons">
        <button class="tab-button active"
                hx-get="/Dashboard?handler=Tab1"
                hx-target="#tab-content"
                hx-push-url="true"
                _="on click remove .active from .tab-button then add .active to me">
            Tab 1
        </button>
        <button class="tab-button"
                hx-get="/Dashboard?handler=Tab2"
                hx-target="#tab-content"
                hx-push-url="true"
                _="on click remove .active from .tab-button then add .active to me">
            Tab 2
        </button>
    </div>
    
    <div id="tab-content">
        <p>Default content loads here...</p>
    </div>
</div>
```

The Hyperscript on each button handles the active state: first it removes the `.active` class from all tab buttons, then adds it to the clicked button. This sequence ensures only one tab appears active at a time.

When the user clicks Tab 2, views its content, navigates to another page, and then hits back, htmx restores the entire `#dashboard-content` element including the active tab state and the tab content. The experience feels continuous rather than reset.

You can also preserve form input values by placing the form inside your history element:

```html
<div id="search-section" hx-history-elt>
    <form hx-get="/Products?handler=Filter"
          hx-target="#product-list"
          hx-push-url="true">
        <input type="text" name="query" placeholder="Search..." value="@Model.Query" />
        <label>
            <input type="checkbox" name="category" value="books" checked="@Model.HasBooks" />
            Books
        </label>
        <label>
            <input type="checkbox" name="category" value="electronics" checked="@Model.HasElectronics" />
            Electronics
        </label>
        <button type="submit">Apply</button>
    </form>
    
    <div id="product-list">
        <!-- Filtered results appear here -->
    </div>
</div>
```

When users apply filters, navigate to a product, and return, their filter selections and search text remain intact. This small detail eliminates the frustration of re-entering search criteria.

## Speed in the Stack: Supercharging UX with History-Based Caching

Performance extends beyond server response time to include how quickly your app responds to user actions. History caching gives you a performance boost without touching backend code. Instead of reloading content when a user navigates back, htmx serves the previous state from memory. The page appears instantly.

When users browse paginated content like search results or data tables, each page typically requires an `hx-get` request. Without caching, clicking into an item and returning to the list means another server request to reload the same data. With htmx history caching, the previous list view comes from the browser's cache instead. Users perceive a dramatic speed improvement.

Combine `hx-push-url` with `hx-select` to control exactly which parts of the response get stored:

```html
<div id="product-results"
     hx-get="/Products?handler=Page"
     hx-target="#product-results"
     hx-push-url="true"
     hx-select="#page-content"
     hx-trigger="load">
</div>
```

The `hx-trigger="load"` ensures the request fires when the element enters the DOM. The `hx-select="#page-content"` tells htmx to extract only the `#page-content` portion from the server response. This scoped approach prevents caching headers, footers, or other page elements that should remain dynamic.

Your server handler returns a complete response, but htmx extracts only what you specified:

```html
<!-- Server response includes the full structure -->
<div id="page-content">
    <table>
        @foreach (var item in Model.Products)
        {
            <tr>
                <td>
                    <a href="/Products/Details?id=@item.Id"
                       hx-get="/Products/Details?id=@item.Id"
                       hx-target="#product-results"
                       hx-push-url="true">
                        @item.Name
                    </a>
                </td>
            </tr>
        }
    </table>
    <div class="pagination">
        <a href="/Products?handler=Page&amp;page=1"
           hx-get="/Products?handler=Page&amp;page=1"
           hx-target="#product-results"
           hx-push-url="true">1</a>
        <a href="/Products?handler=Page&amp;page=2"
           hx-get="/Products?handler=Page&amp;page=2"
           hx-target="#product-results"
           hx-push-url="true">2</a>
    </div>
</div>
```

When users navigate through pages, each view gets cached. Hitting the back button cycles through those cached pages without server requests. The pagination feels as responsive as flipping through local content.

Test your caching behavior using browser developer tools. Open the Application tab and inspect localStorage to see what htmx has cached. You can also add `htmx.logAll()` to your JavaScript console to watch htmx events in real time, including history save and restore operations.

## Designing for Memory: Best Practices with History and Performance

History caching makes your app feel faster, but it requires thoughtful implementation. Cache the wrong content and users see stale data. Cache too much and you waste browser storage. Here are patterns that work well in production.

### Isolate Dynamic Content

Real-time elements like notification counts, shopping cart totals, or live status indicators should not live inside cached regions. Structure your layout to keep these elements outside the `hx-history-elt` container:

```html
<header>
    <div id="cart-count" hx-get="/Cart?handler=Count" hx-trigger="load">
        <!-- Always fresh -->
    </div>
</header>

<main id="page-content" hx-history-elt>
    <!-- Cached content lives here -->
</main>
```

The cart count loads fresh on every page view while the main content benefits from caching.

### Use hx-select to Scope Cached Responses

When your server returns a full page but you only want to cache part of it, use `hx-select`:

```html
<div id="product-container"
     hx-get="/Products?handler=Page"
     hx-target="#product-list"
     hx-select="#product-list"
     hx-push-url="true"
     hx-trigger="revealed">
</div>

<div id="product-list">
    <!-- Products load here -->
</div>

<div id="cart-summary">
    <!-- Never cached because it is outside the selected element -->
    <partial name="_CartSummary" />
</div>
```

The server returns both the product list and cart summary, but only `#product-list` gets extracted and cached.

### Build Consistent Partials

Razor Partials should return stable HTML structures regardless of request context. Avoid conditional wrappers that change the DOM structure:

```html
<!-- Good: Consistent structure -->
<div id="results-wrapper">
    @if (Model.Products.Any())
    {
        <ul class="product-list">
            @foreach (var product in Model.Products)
            {
                <li>@product.Name</li>
            }
        </ul>
    }
    else
    {
        <p class="no-results">No products found.</p>
    }
</div>

<!-- Avoid: Inconsistent wrapper elements -->
@if (Model.Products.Any())
{
    <ul>...</ul>
}
else
{
    <div>No products</div>
}
```

Consistent structures ensure htmx can reliably cache and restore content without layout shifts or broken references.

### Manage Focus for Accessibility

When content restores from cache, users navigating via keyboard or screen reader need proper focus management. Use Hyperscript to restore focus after history restoration:

```html
<div id="search-results" hx-history-elt _="on htmx:historyRestore focus() on #search-input">
    <input type="text" id="search-input" name="query" />
    <!-- Results appear here -->
</div>
```

The `htmx:historyRestore` event fires after htmx restores cached content. The Hyperscript moves focus to the search input, orienting the user within the restored interface.

For screen reader users, include `aria-live` regions to announce dynamic updates:

```html
<div id="results-status" aria-live="polite" class="visually-hidden">
    @Model.Products.Count() products found
</div>
```

### Handle Forms Carefully

Forms inside cached regions retain their values when restored, which is usually what users want. CSRF tokens and other security-sensitive hidden fields can become stale though. Consider refreshing these on restoration:

```html
<form id="product-form" hx-post="/Products?handler=Create" hx-target="#result">
    @Html.AntiForgeryToken()
    <input type="text" name="name" />
    <button type="submit">Create</button>
</form>

<script>
document.body.addEventListener('htmx:historyRestore', function() {
    // Refresh anti-forgery token after history restore
    htmx.ajax('GET', '/Products?handler=Token', {target: '#product-form input[name="__RequestVerificationToken"]', swap: 'outerHTML'});
});
</script>
```

This pattern ensures security tokens stay valid even when forms are restored from cache.

### Test Navigation Thoroughly

History-related bugs are subtle and vary by browser. Build a testing checklist:

1. Navigate forward through several pages
2. Use the back button to return through each page
3. Use the forward button to advance again
4. Verify scroll positions, form values, and active states at each step
5. Test with browser developer tools open to watch for console errors
6. Test on multiple browsers since history handling can differ

Use htmx debugging to trace history operations:

```javascript
htmx.logger = function(elt, event, data) {
    if (event.indexOf('history') > -1) {
        console.log(event, data);
    }
};
```

This logs only history-related events, making it easier to trace caching behavior.

## Common Pitfalls and Solutions

### Stale Data After Server Changes

If server data changes while a user has cached content, they might see outdated information when navigating back. For critical data, consider adding a freshness check:

```html
<div hx-get="/Products?handler=Page"
     hx-target="#product-list"
     hx-push-url="true"
     hx-trigger="load, htmx:historyRestore from:body">
</div>
```

The `htmx:historyRestore from:body` trigger causes a fresh request whenever content is restored from history, ensuring users always see current data. Use this selectively since it negates the performance benefits of caching.

### Cache Size Limits

Browsers limit localStorage to around 5-10MB. If your pages are large or users navigate extensively, you might hit this limit. Monitor cache size and adjust `historyCacheSize` accordingly:

```javascript
htmx.config.historyCacheSize = 5; // Reduce for large page content
```

### URL Mismatch Issues

Ensure your `hx-get` URLs match the pattern you want in the browser address bar. Inconsistent URLs can cause confusion when users bookmark or share links:

```html
<!-- Consistent URL pattern -->
<a hx-get="/Products?handler=Details&amp;id=@product.Id"
   hx-push-url="/Products/Details/@product.Id"
   hx-target="#main-content">
    @product.Name
</a>
```

The `hx-push-url` can specify a different URL than `hx-get`, letting you maintain clean user-facing URLs while using Razor Pages handler routing internally.

## Summary

History caching transforms how users experience navigation in your Razor Pages application. Instead of reloading pages, htmx restores cached content instantly. Instead of losing form inputs and scroll positions, users return to exactly where they left off.

The key concepts to remember:

- `hx-push-url="true"` enables history caching automatically
- `hx-history="false"` disables caching for sensitive content
- `hx-history-elt` specifies which element to snapshot
- `hx-select` scopes which parts of the response get cached
- Consistent HTML structures make caching reliable
- Focus management maintains accessibility
- Testing across browsers catches subtle issues

In the next chapter, we will explore `hx-disable` and `hx-request` for fine-grained control over when and how htmx makes requests. These tools complement history caching by letting you optimize request behavior and manage loading states.
