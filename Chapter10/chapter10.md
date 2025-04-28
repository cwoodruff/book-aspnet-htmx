---
order: 21
icon: stack
label: Chap 10 - Navigating with hx-push-url and hx-replace-url
meta:
title: "Navigating with hx-push-url and hx-replace-url"
---

# Navigating with hx-push-url and hx-replace-url

In previous chapters, we explored how to enhance interactivity in ASP.NET Core Razor Pages using htmx—without the weight of client-side frameworks. We tackled partials, triggers, and response handling, all in service of creating richer, more dynamic user experiences. Now, we turn our attention to a pair of features that subtly yet powerfully influence user navigation: hx-push-url and hx-replace-url.

At first glance, these attributes may seem minor—just toggles for browser history, perhaps. But used thoughtfully, they become key players in making your htmx-powered applications feel seamless and intuitive. Whether you're building a single-page app experience or simply want back and forward buttons to behave as users expect, understanding how htmx manipulates the browser’s URL bar without a full-page reload is essential.

In this chapter, we’ll explore how to integrate these attributes into your htmx workflows and discuss the practical differences between pushing and replacing URLs. Along the way, we’ll cover when and why you'd use each option, how they affect browser history, and how to handle the subtle edge cases they introduce. Let’s get into how to shape navigation behavior that feels natural, even when you're not reloading the page.

## Modern Navigation Patterns with htmx

In traditional web applications, navigating from one page to another typically involves a full-page reload. That means tearing down the DOM, reinitializing scripts, re-fetching layout elements, and generally making the browser do more work than necessary. This isn't just inefficient—it disrupts the user experience. Even with a fast connection, the visible flicker and brief loss of context during a reload make an app feel sluggish and clunky, especially compared to native apps or modern SPAs.

htmx offers a middle ground. With attributes like hx-push-url and hx-replace-url, you can load new content dynamically via AJAX while still updating the browser’s URL bar—no full reload required. This allows you to maintain context, improve perceived performance, and make better use of browser navigation features like the back and forward buttons. Users stay on the same page, technically speaking, but the content and URL change as if they weren’t.

The distinction between hx-push-url and hx-replace-url is subtle but important. When you use hx-push-url, htmx appends the new URL to the browser history stack, just like a normal navigation event. That means hitting the back button will return the user to the previous content. On the other hand, hx-replace-url updates the current entry in the history stack without creating a new one—ideal for flows where you want to reflect a state change (like filtering a list) but not necessarily let the user “go back” to the unfiltered view.

Let’s look at an example. Suppose you're building a blog using Razor Pages and htmx. Instead of reloading the entire page when someone clicks a blog post title, you want to dynamically load the post content and update the URL. Here’s the markup in your Index.cshtml view where the list of blog post titles is rendered:

```html
<a href="/blog/post-1"
        hx-get="/blog/post-1"
        hx-target="#post-container"
        hx-swap="innerHTML"
        hx-push-url="true">
    First Post
</a>
```

When clicked, htmx will make an AJAX request to /blog/post-1, swap the response into the #post-container element, and update the browser's address bar to reflect the new URL. Most importantly, the user doesn’t experience a full reload—they stay right where they are, and the back button works as expected.

If instead you were doing something like selecting a filter or paging through results and didn’t want to pollute the browser history, hx-replace-url="true" would be a better fit:

```html
<a href="/blog?page=2"
        hx-get="/blog?page=2"
        hx-target="#blog-list"
        hx-swap="innerHTML"
        hx-replace-url="true">
    Next Page
</a>
```
These navigation enhancements don’t just make your web app feel faster—they also help align user expectations with behavior. They allow your app to behave more like a traditional multi-page app in terms of navigation, while still delivering the responsiveness users expect from modern interfaces. Understanding how and when to use each option will help you build more polished, intuitive Razor Pages applications with htmx.

## Enhancing Statefulness with hx-push-url

One of the challenges when building dynamic web interfaces is preserving user expectations around navigation. Users expect the back and forward buttons to work. If they click on a tab, view its content, and then hit the back button, they expect to land back where they were—not be stuck on the same tab wondering if their click registered. htmx’s hx-push-url makes it possible to create dynamic, AJAX-driven interactions that integrate cleanly with the browser’s history stack.

The hx-push-url attribute tells htmx to push a new entry into the browser history whenever the request is made and a response is successfully swapped. That means every time you load new content via hx-get, you can optionally update the URL, creating a breadcrumb trail the browser understands. This works well with features like window.onpopstate or htmx’s own built-in support for re-triggering requests when the user navigates back or forward.

Let’s say you’re building a profile page with tab-based navigation—tabs for Overview, Posts, and Settings. You want clicking each tab to load its content without refreshing the page, but you also want the URL to change and support the browser's back button. Here’s how the markup might look in your Profile.cshtml Razor Page:

```html
<div class="tabs">
    <a href="/profile/overview"
       hx-get="/profile/overview"
       hx-target="#tab-content"
       hx-swap="innerHTML"
       hx-push-url="true">
       Overview
    </a>
    <a href="/profile/posts"
       hx-get="/profile/posts"
       hx-target="#tab-content"
       hx-swap="innerHTML"
       hx-push-url="true">
       Posts
    </a>
    <a href="/profile/settings"
       hx-get="/profile/settings"
       hx-target="#tab-content"
       hx-swap="innerHTML"
       hx-push-url="true">
       Settings
    </a>
</div>

<div id="tab-content">
    <!-- Tab content will be loaded here -->
</div>
```
Now, as users click between tabs, the content updates dynamically, the URL changes, and the browser history grows accordingly. More importantly, htmx listens for history navigation events and can automatically re-fetch the appropriate content when the user clicks the back or forward buttons. This creates a more cohesive and polished experience, especially in multi-view interfaces where users expect their path through the app to be remembered.

By embracing hx-push-url, you're effectively combining the smoothness of client-side interactivity with the robustness of server-side rendering and traditional navigation. It’s a great tool for making your ASP.NET Core Razor Pages feel just as interactive as any SPA, but without sacrificing simplicity or maintainability.

## Streamlining Navigation with hx-replace-url

While hx-push-url is perfect when you want the user’s journey to be traceable via the browser’s back and forward buttons, sometimes you don’t want that. Some page transitions—like switching between steps in a form or showing a modal—are temporary or contextual and shouldn’t leave a breadcrumb trail in the history stack. That’s where hx-replace-url comes in. Instead of adding a new entry to the browser’s history, it quietly replaces the current one with the new URL, making the change seamless from the user’s perspective.

Imagine you're building a modal-based interface in Razor Pages where clicking on a product loads details into a modal. You want the URL to reflect the product being viewed (so someone could bookmark it), but you don’t want the back button to go through every modal the user opened. Using hx-replace-url lets you show the correct URL for the current view while keeping history clean. Here's how that might look:

```html
<a href="/products/42"
        hx-get="/products/42"
        hx-target="#modal-content"
        hx-swap="innerHTML"
        hx-replace-url="true">
    View Product
</a>

<div id="modal-content">
    <!-- Modal will be filled dynamically -->
</div>
```

Now let’s take this a step further—literally. Suppose you're building a multi-step form, like a checkout process, and you want each step to update the URL to reflect progress (/checkout/shipping, /checkout/payment, etc.) without leaving a trail for each step in the browser’s history. Users expect to go back to the previous page they visited, not each intermediate form step. Here's how each step link might be defined:

```html
<a href="/checkout/payment"
        hx-get="/checkout/payment"
        hx-target="#form-content"
        hx-swap="innerHTML"
        hx-replace-url="true">
    Continue to Payment
</a>

<div id="form-content">
    <!-- Current form step gets loaded here -->
</div>
```

Each new form step replaces the URL in-place, keeping the user experience tight and focused. When they complete the process, they can still hit the back button to leave the form entirely, rather than backing through every form stage. This keeps the navigation behavior intuitive and avoids bloating the history stack with temporary state changes.

By using hx-replace-url in the right scenarios, you enhance user experience without overwhelming browser history. It’s a subtle touch, but one that communicates a deep respect for user intent and browser conventions—something that elevates your application from functional to delightful.

## Deep-Linking with State: Making URLs Work for You

One of the biggest wins with htmx is how easily you can create dynamic, fast-loading UIs. But there’s a catch: if you’re not careful, your app can lose state when someone refreshes the page or shares a link. That’s where deep-linking comes in. You want every view your user lands on—whether they click, refresh, or paste a URL into a new tab—to render the expected content. This is especially important in filterable lists or tabbed interfaces where the content is tied to specific state values in the URL.

Razor Pages plays well with deep-linking, thanks to route and query parameters. When combined with htmx, you can build interfaces that respond to URL changes and restore the expected content. Let’s walk through an example: a filterable product listing where the selected category is stored in the URL so users can share or bookmark it.

Suppose you have a page at /products and you want to support filtering by category with links like /products?category=Books. First, set up your Products.cshtml.cs Razor Page model to accept the query parameter:

```csharp
public class ProductsModel : PageModel
{
    public string? Category { get; set; }
    
    public void OnGet(string? category)
    {
        Category = category;
        // Use the category to load filtered products from the database
    }
}
```

Next, in the Razor Page view, render the filter links and product listing container:

```html
<a href="/products?category=Books"
        hx-get="/products?category=Books"
        hx-target="#product-list"
        hx-swap="innerHTML"
        hx-push-url="true">
    Books
</a>

<a href="/products?category=Games"
        hx-get="/products?category=Games"
        hx-target="#product-list"
        hx-swap="innerHTML"
        hx-push-url="true">
    Games
</a>

<div id="product-list">
    @* Initial product list will render here *@
    @await Html.PartialAsync("_ProductList", Model.FilteredProducts)
</div>
```

The key piece is hx-push-url="true", which updates the browser’s address bar when users filter the list. htmx handles the AJAX request, and if someone reloads the page or lands directly on /products?category=Books, Razor Pages will rehydrate the correct product list thanks to the parameter handling in OnGet.

By implementing deep-linking like this, you get the best of both worlds: the fluid interactivity of htmx and the reliability of traditional server-rendered pages. You’re also preparing your app for more complex UI interactions—something we’ll dig into in the next chapter, “Building Blocks with HTML Components: Forms and Modal Forms.” There, we’ll use the navigation strategies you've just learned to drive highly interactive UI components like multi-step forms and modals, all while keeping things sharable, bookmarkable, and refresh-proof.