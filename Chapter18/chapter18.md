---
order: 13
icon: stack
label: Chap 18 - Caching and History with hx-history and hx-history-elt
meta:
title: "Caching and History with hx-history and hx-history-elt"
visibility: protected
---

# Caching and History with `hx-history` and `hx-history-elt`

Sometimes the best performance boost isn’t just about loading less, it’s about not loading at all. If you’ve ever hit the back button on a web app and watched it completely reload a page you just visited, you know how jarring that experience can be. In this chapter, we’ll explore how `hx-history` and `hx-history-elt` let us eliminate that pain by caching prior responses in the browser’s history stack. With just a little planning, you can make your htmx-powered Razor Pages feel as snappy and intuitive as a single-page app without the JavaScript baggage.

You’ve already seen how htmx simplifies form handling, dynamic interactions, and server-driven updates. Now it’s time to give your users a smoother journey through your application by preserving state across navigation. These tools don’t just enhance performance, they also improve usability by allowing users to move backward and forward without re-fetching data or losing their place. It’s a natural next step after mastering scoped updates, real-time interactions, and Hyperscript-based drag-and-drop.

We’ll start by demystifying how browser history and caching work in an htmx context. From there, you’ll learn how to enable smart caching for specific elements, which helps reduce server calls and makes your app feel faster. Whether you're building a content-heavy dashboard or a multi-step form, understanding how to cache intelligently can dramatically enhance the user experience.

By the end of this chapter, you’ll have the tools to create rich, state-aware applications that feel fast and modern without giving up the simplicity of Razor Pages. Let’s see how htmx helps you make the most of the browser’s built-in navigation features, and how a little caching can go a long way.

## Making Back Feel Fast Again: History Management with `hx-history`

In traditional server-rendered apps, clicking the back button after an interaction typically means reloading the entire page, which can result in losing the user's state, such as a form input or a list of search results. That jarring behavior has become so common that users often avoid the back button altogether. With htmx, however, we can reclaim the back button and give it superpowers using `hx-history`.

The `hx-history` attribute enables htmx to store and retrieve content from the browser’s history cache. When enabled, htmx saves the response from a request in the history stack. If the user navigates back or forward, htmx can reuse that response without needing to re-request it from the server. It’s one of those subtle features that makes your app feel lightning fast and incredibly user-friendly.

Imagine a user searches for products in your app. They click a result, read the details, and then hit the back button, expecting to return to their previous list. Without `hx-history`, the page reloads, and the search results disappear. But with `hx-history="true"` added to your request, htmx caches the result list. When the user navigates back, the list reappears instantly, just as they left it. No extra load time. No server call. No frustration.

Let’s look at a simple Razor Pages example that demonstrates this. First, we create a basic search form on the `Search.cshtml` page:

```html
<form hx-get="/Search?handler=Results"
      hx-target="#search-results"
      hx-push-url="true"
      hx-history="true">
    <input type="text" name="query" placeholder="Search products..." />
    <button type="submit">Search</button>
</form>

<div id="search-results"></div>
```

The important part here is `hx-history="true"`. This tells htmx to cache the server’s response. The `hx-push-url="true"` ensures that the browser's URL is updated with the search query, which plays nicely with navigation and bookmarking.

On the server side, we handle the search logic like this:

```C#
public class SearchModel : PageModel
{
    public IActionResult OnGetResults(string query)
    {
        var products = ProductRepository.Search(query); // Assume we get results here
        return Partial("_SearchResults", products);
    }
}
```

The _SearchResults.cshtml partial might return a simple product list:

```html
<ul>
    @foreach (var product in Model)
    {
    <li><a href="/Product/@product.Id" hx-get="/Product/@product.Id" hx-target="body" hx-push-url="true">@product.Name</a></li>
    }
</ul>
```

When the user performs a search, the results are dynamically inserted into #search-results. Now, if they click on a product and then hit the back button, htmx restores the cached result list immediately without refetching it. The UI appears just as smooth and snappy.

This technique significantly enhances perceived performance, particularly in applications with numerous drill-down or detail pages. Users feel like the app remembers their place, which is precisely the kind of UX that keeps people happy and engaged.

As we continue through this chapter, you’ll learn how to cache only the necessary parts of your UI using hx-history-elt and how to fine-tune the behavior when state needs to persist or reset. But for now, know this: the back button doesn’t have to be a problem anymore.

## Locking In the Experience: Retaining State with `hx-history-elt`

So far, you've seen how `hx-history` provides your app with a memory for previous interactions, allowing users to navigate back without waiting for the server. But sometimes, preserving just the response isn’t enough. What if you need to remember which tab was selected, how far the user had scrolled, or which filter was checked? That’s where `hx-history-elt` comes in. It allows you to define specific elements whose state should be cached and restored across navigation.

The idea behind `hx-history-elt` is simple but powerful. When a request is made and the response is cached in the browser’s history, htmx will also snapshot any element that includes this attribute. When the user navigates back or forward, htmx will rehydrate those elements exactly as they were. That includes scroll position, input values, and even which tab was active. It turns a good experience into a seamless one.

Let’s walk through a typical example. Suppose you have a dashboard with several tabs, and each tab displays dynamic content. Users might click into one of those tabs, scroll down, view some details, and then navigate to another part of the app. When they hit the back button, you want everything to be exactly as they left it. Without `hx-history-elt`, they’d be dumped back to the top of the default tab, losing context. With it, their state is fully preserved.

Here’s what that might look like in Razor Pages. First, the tabs component:

```html
<div id="tabs" hx-history-elt>
    <button class="tab-button active"
            hx-get="/Dashboard?handler=Tab1"
            hx-target="#tab-content"
            _="on click add .active to me then remove .active from .tab-button then remove .active from me">
        Tab 1
    </button>
    <button class="tab-button"
            hx-get="/Dashboard?handler=Tab2"
            hx-target="#tab-content"
            _="on click add .active to me then remove .active from .tab-button then remove .active from me">
        Tab 2
    </button>
    <div id="tab-content" hx-history-elt>
        <div style="height: 600px; overflow-y: scroll;">
            <p>Default content...</p>
        </div>
    </div>
</div>
```

We mark the entire tabs wrapper with `hx-history-elt`. This tells htmx to remember the state of this DOM element when the browser’s history snapshot is taken. That includes which tab had the .active class and any scroll position inside the content area. The next time the user navigates back to this page, htmx restores it as if they had never left.

To complement this, make sure the content updates for the tab don’t overwrite the entire page. You might dynamically load tab content using `hx-get` like this:

```html
<button hx-get="/Dashboard?handler=Tab1" hx-target="#tab1-content" hx-push-url="true">Load Tab 1</button>
<div id="tab1-content" hx-history-elt></div>
```

In this case, you’re not just caching the tab’s content but also preserving the user’s interaction within it. Even form input values typed into a text field inside this region will be retained.

You can take this even further by placing `hx-history-elt` on form wrappers. That way, if the user fills out part of a form, navigates away, and returns, they won’t have to start over. It’s a minor feature that feels magical in practice.

By combining `hx-history` for routing and `hx-history-elt` for state retention, you can make your Razor Pages apps feel effortless to navigate. Together, they create the kind of fluid, reliable experience users expect from high-quality applications.

## Speed in the Stack: Supercharging UX with History-Based Caching

Performance is not just about server response time. It is also about how quickly your app responds to user input. htmx gives us a unique way to optimize responsiveness through history-based caching. Instead of reloading content or hitting the server again when a user navigates back, `hx-history="true"` can serve the previous state instantly from memory. It is one of the fastest wins you can get without writing a line of backend logic.

When a user browses paginated content like search results or data tables, each page is typically fetched through an `hx-get` request. Without caching, if they click into an item and then return to the list, the browser would make another request to reload the same page. With `hx-history` enabled, the previous list view is retrieved from the browser’s history stack instead. The perceived speed improvement is dramatic.

To take control over what gets cached and reused, you can pair `hx-history="true"` with `hx-select`. This combination allows you to scope which parts of the server response are stored and later re-injected. For example, in a paginated table, you might want to cache only the results grid while allowing headers or sidebars to update dynamically.

Here’s what that could look like in Razor Pages:

```html
<div id="results" hx-get="/Products?handler=Page" hx-target="#results" hx-push-url="true" hx-history="true" hx-select="#page-content">
</div>
```

On the server, your handler might return a complete page structure, including the header, filters, and content. But because `hx-select="#page-content"` is specified, only the content inside the #page-content element is swapped into the DOM and cached.

```html
<div id="page-content">
    <table>
        @foreach (var item in Model.Products)
        {
            <tr><td>@item.Name</td></tr>
        }
    </table>
    <div class="pagination">
        <a href="/Products?handler=Page&page=1" hx-get hx-target="#results">1</a>
        <a href="/Products?handler=Page&page=2" hx-get hx-target="#results">2</a>
    </div>
</div>
```

This scoped caching avoids bloating the history stack with parts of the page that do not need to be remembered. It also means that when the user hits the back button, the product list appears immediately, complete with pagination, and without a round-trip to the server.

As powerful as this feature is, it is also essential to test it. Use your browser’s developer tools to simulate navigation and inspect how cached content is restored. Toggle caching behaviors on and off to see their impact. You may also want to temporarily enable debugging in htmx using the `hx-debug="true"` attribute to get a clear picture of what is being cached, restored, or reloaded.

When combined with smart UX patterns, such as maintaining the scroll position or utilizing active filters with `hx-history-elt`, this caching strategy can significantly enhance the user experience of your application. Pages stop feeling like isolated transactions and start behaving like a continuous, fluid experience. That is the kind of performance users notice and appreciate.

## Designing for Memory: Best Practices with History and Performance

Once you start using `hx-history` and `hx-history-elt` to retain state and reduce server load, you quickly see how much smoother your app can feel. But like any performance feature, it comes with some trade-offs. If you are not careful, you can accidentally cache stale or unwanted content, especially in dynamic applications where the data changes frequently or needs to reflect user-specific logic. Knowing when and where to apply history caching is just as important as knowing how to do so.

One way to avoid these pitfalls is to scope your cached responses deliberately. If your page includes real-time elements, such as notifications, cart counts, or live updates, ensure that these are excluded from the cached region. You can use `hx-select` to isolate the dynamic content from the rest of the response, caching only what should remain the same when navigating back. When building partials for Razor Pages, keep those sections modular so you can selectively render or swap them.

Accessibility also deserves attention when you are working with history-based caching. Preserving state across navigation is a huge win, but users navigating via keyboard or screen readers must also be considered. For example, make sure that dynamically reinserted content does not break focus or skip heading hierarchies. Using `aria-live` regions and managing focus programmatically after history restoration can ensure usability remains strong for everyone.

Reusable components, like search forms or tab interfaces, should be built with state caching in mind from the start. Structure your Razor Partials so that each component includes the necessary context to restore its view. Wrap persistent UI elements in containers that use `hx-history-elt`, and test your components in different navigation scenarios. A well-designed search bar or tab section should return exactly to its last-known state without needing to reload or reset anything.

Let’s take a practical example. Suppose you have a filterable product list, a sidebar of checkboxes, and a search box. Structure the layout so the sidebar and search bar are wrapped in a component marked with `hx-history-elt`. The product results get loaded via `hx-get`, and only that portion uses `hx-history`. Now your filters and input remain intact, even when navigating forward and back between products and result pages. It's not just fast; it feels thoughtful.

Make sure your Razor Pages and handlers return consistent markup for repeatable caching. If the returned HTML structure differs depending on the context or user state, you could end up with mismatched snapshots. Lean into server-side rendering with predictable patterns. Use partial views and tag helpers to keep your component layout uniform and easy to maintain across requests.

Test often. History-related issues can be subtle and vary by browser. Use htmx’s debugging tools or simply open dev tools and walk through the navigation stack manually. You will catch inconsistencies early, like scroll jumps, missing data, or rehydration quirks that might frustrate users if left unchecked.

As you incorporate these patterns, think about history as more than just a browser feature. It is part of your UX strategy. When used correctly, it lets users explore confidently, knowing the app will remember their place and get out of their way. When misused, it can create confusing inconsistencies or expose outdated content.

In the next chapter, we will go deeper into fine-tuning request behavior with `hx-disable` and `hx-request`. These tools let you control when requests are made, how they are sent, and what side effects they trigger. Together with the history features you now know, they provide a complete toolkit for building responsive, efficient applications with Razor Pages and htmx.

## Designing for Memory: Best Practices with History and Performance

Once you start using `hx-history` and `hx-history-elt` to retain state and reduce server load, your app begins to feel smoother and smarter. But without some guardrails, it's easy to cache too much, cache the wrong thing, or end up with inconsistent UI behavior. Like any optimization, it’s most effective when used deliberately and with structure.

A common mistake is caching sections of the page that change frequently or depend heavily on user-specific data. For example, a real-time shopping cart count or notification badge should not be included in a cached region. You can avoid this by isolating those components outside your history-managed containers using `hx-select` to scope what gets swapped in and what stays fresh.

Here’s an example where we scope the response to only replace the product list inside a larger layout:

```html
<div id="product-container"
     hx-get="/Products?handler=Page"
     hx-target="#product-list"
     hx-select="#product-list"
     hx-history="true"
     hx-push-url="true">
</div>

<div id="product-list">
    <!-- Products will load here -->
</div>

<div id="cart-summary">
    <!-- This should not be cached -->
    <partial name="_CartSummary" />
</div>
```

The server returns the entire page, including both the product list and cart summary, but only the #product-list section is updated and cached. This avoids caching stale cart data while preserving the paginated product view.

You can also design your forms and filters as reusable components that persist state. For example, a sidebar with checkbox filters and a search box should retain input values when navigating back. Wrap them in an element marked with `hx-history-elt`:

```html
<div id="filters" hx-history-elt>
    <form hx-get="/Products?handler=Filter"
          hx-target="#product-list"
          hx-push-url="true"
          hx-history="true">
        <input type="text" name="query" placeholder="Search..." value="@Model.Query" />
        <label>
            <input type="checkbox" name="category" value="books" @(Model.HasBooks ? "checked" : "") />
            Books
        </label>
        <label>
            <input type="checkbox" name="category" value="electronics" @(Model.HasElectronics ? "checked" : "") />
            Electronics
        </label>
        <button type="submit">Apply</button>
    </form>
</div>
```

When users apply filters, htmx caches the results and also saves the state of this form. If they view a product and then return, all inputs and selections reappear exactly as before.

To ensure accessibility, manage focus properly when restoring from history. If your content uses dynamic tabbing or modals, you might use a small Hyperscript snippet to restore focus:

```html
<div id="product-list" hx-history-elt _="on htmx:afterSettle set #query to focus">
    <!-- Search results go here -->
</div>

<div id="cart-summary">
    <!-- This should not be cached -->
    <partial name="_CartSummary" />
</div>
```

This ensures the user is returned to the search field or result list, rather than being left disoriented. You can also include aria-live regions to clearly communicate updates to users of screen readers.

Consistency is key when building reusable, history-aware components. Razor Partials should return stable HTML structures regardless of request context. Avoid conditional wrappers or varying layouts. Here’s a good structure for a paginated product grid:

```html
<div id="results"
     hx-get="/Products?handler=Page"
     hx-target="#results"
     hx-push-url="true"
     hx-history="true"
     hx-select="#page-content">
</div>

<!-- Server response should include this structure -->
<div id="page-content">
    <table>
        @foreach (var item in Model.Products)
        {
        <tr><td><a href="/Product/@item.Id">@item.Name</a></td></tr>
        }
    </table>
    <div class="pagination">
        <a href="/Products?handler=Page&page=1"
           hx-get="/Products?handler=Page&page=1"
           hx-target="#results"
           hx-push-url="true"
           hx-history="true">1</a>
        <a href="/Products?handler=Page&page=2"
           hx-get="/Products?handler=Page&page=2"
           hx-target="#results"
           hx-push-url="true"
           hx-history="true">2</a>
    </div>
</div>
```

If every pagination response includes this exact structure, the browser will have no trouble restoring it cleanly from the history cache.

Finally, don’t forget to test your interactions. Use htmx’s debugging features and browser developer tools to simulate forward and back navigation. Look for edge cases, such as scroll jumps or missing UI states, and refine your layout accordingly.

In the next chapter, we’ll look at how `hx-disable` and `hx-request` give you low-level control over when requests are made and what gets sent. These tools help fine-tune performance and behavior, rounding out your understanding of how to keep Razor Pages responsive, efficient, and enjoyable to use.