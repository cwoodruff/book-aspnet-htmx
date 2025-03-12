---
order: 24
icon: stack
label: Chap 7 - Working with hx-target and hx-swap
meta:
title: "Working with hx-target and hx-swap"
visibility: hidden
---
# 7

# Working with `hx-target` and `hx-swap`

Using `hx-target` and `hx-swap` in htmx is akin to fine-tuning the steering and suspension of a high-performance car. While you can manage the basics, adjusting the precise location and method of content injection reveals how much more efficient and smooth your page interactions can become. These two attributes provide you with targeted control over which elements are updated and how those updates are applied, allowing you to create dynamic interfaces that feel intentional and polished.

With `hx-target`, you can direct server responses to specific parts of the page, ensuring that users see exactly what they need in the locations they expect. When combined with `hx-swap`, you can determine where the content will go and whether it should be replaced, wrapped around, or inserted before existing elements. This user-centric approach encourages careful consideration of your UI design—eliminating clutter and unnecessary movement, resulting in clean, targeted updates that keep users engaged.

As you explore the potential of `hx-target` and `hx-swap`, you'll discover how these attributes can significantly reduce unnecessary page reloads and heavy front-end scripting, which refers to using client-side scripts to update the user interface dynamically. In this chapter, we will examine how to use these features effectively, explore various swapping strategies, and discuss best practices to ensure that your dynamic updates remain intuitive and maintainable. The outcome is a browsing experience that feels fluid and polished, requiring minimal effort on your part.

## Refining Page Updates with hx-target and hx-swap

Many developers first encounter htmx by returning snippets of HTML that automatically replace the button or link that triggered the request. This default behavior feels magical at first, but once you move beyond basic demos, you’ll want more fine-grained control over where and how your new content is displayed. That’s where `hx-target` and `hx-swap` step in, letting you decide which element should be updated and exactly how the update should happen.

By default, htmx injects the server’s response into the element that triggered the request. This works fine for small-scale interactions but can quickly become confusing if you have multiple clickable elements. Explicit targeting removes that guesswork. You can direct the response to the element you have in mind, whether it's a specific container, a section of the page, or a floating modal. The key is the `hx-target` attribute, which tells htmx exactly where to place the returned HTML.

`hx-target` accepts a CSS selector or an element ID. If you specify `hx-target="#result"`, the content returned from the server will be swapped into an element whose `id="result"`. An example in Razor Pages might look like this:

```HTML
<button hx-get="/Index?handler=Details" hx-target="#detailsSection">
    Show Details
</button>

<div id="detailsSection"></div>
```

Here, clicking “Show Details” sends a request to the `OnGetDetails()` handler in your Index page model, and whatever HTML is returned lands in the `detailsSection` div. This approach keeps the rest of your page intact and allows you to update only the part the user cares about.

`hx-swap` determines how the new content is inserted. The default value, innerHTML, replaces the target’s contents while preserving the target’s outer container. Other options like `outerHTML` or `beforebegin` can be handy for special layouts. If you don’t specify `hx-swap`, htmx uses `innerHTML` by default, but it’s good practice to set it explicitly when building more complex UIs. For instance, `hx-swap="beforeend"` would append to the existing content instead of replacing it entirely.

A common real-world example involves updating a page section. Suppose you have a partial view called `_UserProfile.cshtml` that returns a snippet of HTML. You might add a button in your main page with:

```HTML
<button hx-get="/Profile?handler=LoadProfile" hx-target="#profileContainer" hx-swap="innerHTML">
    View Profile
</button>

<div id="profileContainer">
    <!-- Profile details will be injected here -->
</div>
```

Your `Profile.cshtml.cs` file might contain:

```C#
public IActionResult OnGetLoadProfile()
{
    var userProfile = ... // Load from database or another source
    return Partial("_UserProfile", userProfile);
}
```

This setup ensures only the content inside profileContainer is replaced whenever the user clicks View Profile, leaving the rest of the page unchanged.

Another place these attributes shine is when you’re working with lists and want to replace or append a single item without rerendering the entire container. Imagine a to-do list that you can update one entry at a time. Your htmx-triggered request can send only the new or edited item from the server, inserting it exactly where it belongs. For instance, `hx-swap="outerHTML"` could replace one list item in place, while `hx-swap="beforeend"` could be used to add newly created items to the bottom of the list without clearing out what’s already there.

Once you start refining your page with `hx-target` and `hx-swap`, you’ll see how neatly they pair with Razor Pages. Instead of building an entire client-side framework for partial updates, you can let htmx handle the fetching and DOM manipulation while focusing on server-side rendering. This approach leads to a clean division of responsibilities, a maintainable codebase, and—most importantly—pages that feel fast and responsive.

As you continue experimenting, you’ll find that `hx-target` and `hx-swap` open up a world of flexible options, letting you craft interactive experiences with minimal JavaScript overhead. It’s a step up from basic demos and a natural progression toward building larger, more dynamic applications where you control every subtlety of the user interface.

## Mastering `hx-swap` for Smooth UI Updates

One of htmx’s superpowers lies in its ability to update web page content seamlessly using different swap strategies. The `hx-swap` attribute tells htmx exactly how to inject the server’s response into the DOM, allowing you to tailor the user experience for each interaction. Knowing which swap mode to use—and when—gives you a higher level of control, transforming static pages into snappy, dynamic interfaces.

At its simplest, `hx-swap` can be left off entirely, which defaults to innerHTML. This approach replaces only the inside of the targeted element, leaving the outer container intact. Often, that’s precisely what you need. For example, if your button triggers an update to a div with id="results", htmx will replace the contents of that div but keep the div itself in place, allowing you to change the data without losing existing container styles or attributes.

outerHTML takes it one step further by swapping out the entire element, including the container itself. This is handy when you need to remove or replace an element altogether, such as swapping a placeholder container with a newly rendered component. You might see a code snippet like this in your Razor Page:

```HTML
<button hx-get="/Index?handler=CompleteTask" hx-target="#taskItem" hx-swap="outerHTML">
    Complete
</button>
<div id="taskItem">
    <p>Task in progress...</p>
</div>
```

In this scenario, once the server returns the updated markup, the #taskItem div is replaced in its entirety, which is exactly what you’d want for removing or transforming a completed item.

The modes beforebegin, afterbegin, beforeend, and afterend let you insert new content in specific positions around the target element without overriding its existing content. For example, if you have a dynamic list and want to add new items at the end, you might use `hx-swap="beforeend"`. Suppose your list is rendered in a partial view named `_ItemList.cshtml`. A simple approach could be:

```HTML
<button hx-get="/List?handler=AddItem" hx-target="#listContainer" hx-swap="beforeend">
    Add More Items
</button>
<div id="listContainer">
    <!-- The existing list items go here -->
</div>
```

When the user clicks the button, the server returns a snippet for a new list item, which is then added to the bottom of the existing list, preserving whatever was there before.

Replacing form content after submission is a common use case for these modes. Say you have a form that posts data to the server, and upon successful save, you want to replace the form with a thank-you message. You might do something like:

```HTML
<form hx-post="/Index?handler=SaveData" hx-target="#formContainer" hx-swap="innerHTML">
    <input type="text" name="UserName" />
    <button type="submit">Submit</button>
</form>
<div id="formContainer"></div>
```

The OnPostSaveData method can return a partial or a simple string. Whatever comes back is inserted into #formContainer, effectively replacing the original form content without a page reload.

Adding items to a list is straightforward with insertion modes. If you want newly added items to appear at the top, set `hx-swap` to afterbegin. If you need them to appear at the bottom, use `beforeend`. The combination of `hx-get` for retrieving new items and `hx-post` for saving them makes it easy to craft intuitive list-based interfaces without resorting to complex JavaScript frameworks.

Implementing an Infinite Scrolling list can take these ideas even further. You can attach an `hx-trigger` to a scroll event or a “Load More” button that appends new items to your existing collection whenever the user reaches the bottom of the page. For instance, setting `hx-get="/Items?handler=LoadNextPage" hx-swap="beforeend"` on a button that appears below your list means the server only sends the next chunk of items, and htmx appends them so the user can seamlessly keep scrolling.

```HTML
@page
@model InfiniteScrollModel
<div id="scrollable">
    <ul id="feed">
        @foreach (var post in Model.Posts)
        {
            <li>@post</li>
        }
    </ul>
</div>
<div hx-get="/InfiniteScroll?handler=LoadMore" hx-target="#feed" hx-swap="beforeend" hx-trigger="revealed">
    <!-- This element triggers loading new items when it is revealed -->
</div>

@code {
    public List<string> Posts { get; set; } = new() { "Post 1", "Post 2" };

    public IActionResult OnGetLoadMore()
    {
        return Content("<li>Post " + DateTime.Now.Ticks + "</li>", "text/html");
    }
}
```

Choosing the right swap mode is all about preserving context while making room for the new. Whether you’re replacing a form with a confirmation message, removing an outdated element, or appending new content, `hx-swap` gives you the flexibility to handle any scenario. The more you use these modes, the more confident you’ll become in orchestrating sophisticated UI updates without writing extra JavaScript.

## Orchestrating Multiple UI Updates with `hx-target` and `hx-swap`

Sometimes a single element swap isn’t enough. You might need to update multiple parts of your page, or even perform background changes that the user doesn’t directly trigger. By combining `hx-target` and `hx-swap` in creative ways, you can build surprisingly sophisticated interfaces without resorting to heavy JavaScript frameworks.

One powerful technique is the out-of-band (OOB) update, which lets you apply DOM changes in parts of the page that weren’t explicitly targeted. You can include an element in your server’s response with `hx-swap-oob="true"`, which signals htmx to find that element elsewhere on the page and replace it. This allows you to refresh, for example, a status bar or navigation badge in the background, even when the user interacts with a completely different section of the site.

Managing multiple content updates in a single request relies on returning multiple snippets, each tagged with its own OOB attributes. Suppose you have a shopping cart that needs to update an item’s subtotal, the cart total, and the cart count in the header. Your partial could look like this:

```HTML
<div id="cartItem_@(Model.Id)" hx-swap-oob="true">
    <p>Item: @Model.Name - @Model.Quantity x @Model.Price</p>
</div>

<div id="cartTotal" hx-swap-oob="true">
    <p>Total: @Model.CartTotal</p>
</div>

<div id="headerCartCount" hx-swap-oob="true">
    <span>Cart (@Model.ItemCount)</span>
</div>
```

When the user clicks a button to update the cart, the server returns this chunk of HTML. htmx injects each element into its matching target by ID, even though your main `hx-target` might point somewhere else entirely.

To see this in action, imagine a button that removes an item from the cart. The button might include `hx-get="/Cart?handler=RemoveItem&id=123"` and `hx-target="#cartContainer" hx-swap="innerHTML"`. The server would return a response that includes updated HTML for that particular item, the new total, and a header count. Once htmx processes the returned snippet, it uses the `hx-swap-oob` attributes to insert content into the existing elements outside of cartContainer, saving you the trouble of making multiple round trips or writing complex JavaScript.

Sometimes you also need to orchestrate a more interactive flow, where updating one element leads to a fresh look in another. Combining multiple `hx-target` attributes, each paired with different swap modes, can help. For example, you could replace a button’s label with “Updating…” by using `hx-swap="outerHTML"` while also appending new rows to an order summary with `hx-swap="beforeend"`. Each part of the page gets the right kind of update, all triggered by a single server response.

Implementing a fully interactive shopping cart with real-time updates showcases just how far you can push these concepts without diving into a separate frontend framework. You can increment or decrement item quantities, see the totals update immediately, and even watch as the cart icon in the header reflects the new item count, all from one streamlined set of server responses. This consistent, server-driven approach keeps your app’s logic in Razor Pages while still delivering a fluid user experience.

Whether you’re inserting new content, removing elements, or making hidden background changes, combining `hx-target`, `hx-swap`, and out-of-band updates is a robust strategy for building more complex interactions. The ability to chain these updates, orchestrating multiple changes with a single request, is one of htmx’s biggest strengths and a perfect fit for ASP.NET Core Razor Pages.

## Refining and Troubleshooting Targeted Content Updates

One of the most common pitfalls when using `hx-target` and `hx-swap` is accidentally pointing to the wrong element. It’s easy to copy a snippet from another part of your page and forget to update the target’s ID or selector, leaving you wondering why your UI isn’t refreshing. If you see no errors in the console and the page isn’t updating, double-check that you’re referencing the correct element ID and that the server is returning valid HTML. Even a small syntax slip in your Razor partial can silently break the swap.

When debugging, the browser’s developer tools are your best friend. You can watch the Network panel as htmx triggers requests, inspect the response, and confirm whether the returned HTML looks correct. If htmx is updating the page but not as expected, open the Elements panel and see how the DOM changes after each request. This real-time view makes it easy to confirm whether your target element received the new content or if you’re accidentally replacing the wrong part of your layout.

Performance-wise, avoid extra re-renders by returning only the snippet you need. If all you want to change is a single row in a table or a single card in a list, don’t return the entire page or the entire list. Keeping server responses lean and highly focused reduces network overhead and speeds up DOM operations. Your goal is to minimize the payload so that htmx can do its job quickly, leaving the rest of the page untouched.

A simple strategy for checking what your code is doing under the hood is to show comments in the browser’s console. Suppose you have a debugging call inside your Razor Page Model:

```C#
public IActionResult OnGetDebug()
{
    // You can log anything server-side here
    Console.WriteLine("Server says hello!");
    return Content("<script>console.log('Client says hello!')</script>", "text/html");
}
```

When you load that response, htmx inserts the `<script>` tag into the DOM, causing the console message to appear. Although it’s a bit unconventional for production, this trick can come in handy when you need quick insights into what’s happening behind the scenes.

Another key to avoiding unnecessary re-renders is to chain smaller updates rather than cramming multiple big swaps into a single interaction. If you discover that you’re reloading a large part of the UI to make minor changes, consider dividing your layout into multiple targetable sections. That way, each user action updates only what’s strictly necessary, keeping everything snappy.

As you refine and troubleshoot these targeted content updates, you’ll notice your pages gain a certain fluidity that feels closer to a single-page app, yet remains firmly rooted in server-side rendering. htmx lets you combine the best of both worlds, but it also requires careful planning to ensure you’re calling the right endpoints, returning properly scoped partials, and pointing your `hx-target` attributes in the right direction.

In the next chapter, we’ll explore “Handling Events with `hx-trigger` and `hx-on`,” where you’ll learn how to better control when updates happen. That knowledge pairs beautifully with targeted content updates, letting you orchestrate conditional swaps, delay actions, and even respond to custom events as your UI grows in complexity.