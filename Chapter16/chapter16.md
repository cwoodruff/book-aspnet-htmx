---
order: 15
icon: stack
label: Chap 16 - Scoped Updates with hx-select and hx-select-oob
meta:
title: "Scoped Updates with hx-select and hx-select-oob"
visibility: protected
---
# Scoped Updates with `hx-select` and `hx-select-oob`


By now, you've built a solid foundation with htmx, swapping content, managing triggers, and crafting dynamic forms that just work. But as your pages grow more interactive, you'll inevitably hit a point where updating a single target isn’t enough. Maybe you want to update a sidebar while changing the main content, or inject a modal without touching the rest of the page. That’s where scoped updates come in, and `hx-select` and `hx-select-oob` are your new best friends.

This chapter is all about refining how updates flow through your application. Instead of relying on a single target to receive server responses, `hx-select` lets you narrow down the specific part of the returned HTML to inject. It gives you surgical precision, which is ideal when your response contains more than one usable fragment. Meanwhile, `hx-select-oob` opens the door to out-of-band updates, letting you update multiple, unrelated areas of the DOM in one go, without tying them to a single interaction or target.

If you’ve ever wished you could keep your UI responsive without duplicating server logic or bloating your Razor Pages with conditionals, this is your chapter. We'll walk through how to structure responses for selective updates, coordinate multiple components, and keep your frontend logic minimal and clear. These techniques help you break free from rigid page structures and unlock more flexible interactions, without resorting to JavaScript frameworks.

Mastering scoped updates is your bridge to building rich, responsive layouts with htmx. Once you're comfortable here, you’ll be ready to tackle advanced form enhancements in the next chapter, taking everything you've learned so far and making it even more powerful.

## Understanding Scoped Updates with `hx-select`

As your Razor Pages application grows, you’ll find yourself working with more complex responses, maybe a modal, a notification bar, and some freshly fetched content, all delivered in a single response. By default, htmx will drop the entire response into the target you specify. That works great for simple interactions. But what if your response contains more than one part, and you only want to insert a specific piece? That’s where `hx-select` comes in.

`hx-select` gives you surgical control over what gets swapped into the DOM. Instead of injecting the full response, you can instruct htmx to extract just the part you want using a CSS selector. This small change makes your interactions more efficient, especially when returning a large chunk of HTML but only updating a single widget or section on the page.

Let’s say you have a blog post page with a comment section at the bottom. When a user submits a new comment, you don’t want to re-render the entire post; you just want to refresh the comments. You could structure your Razor Page handler to return the entire page, or even a full layout. But with `hx-select`, you can return a full HTML document or section, and still update only the relevant part on the client side.

Here’s how that might look. In your form, you define where the response should be inserted and how much of it to use:

```html
<form hx-post="/Post?handler=AddComment"
      hx-target="#comments"
      hx-select="#comments">
    <textarea name="commentText"></textarea>
    <button type="submit">Add Comment</button>
</form>
```

The important part here is `hx-select="#comments"`. This tells htmx to scan the returned HTML and grab only the element that matches that CSS selector, in this case, the `#comments` div. That element then updates the target, replacing its contents with the fresh comment list.

Your Razor Page handler could return the full comment section, possibly as part of a partial view:

```csharp
public IActionResult OnPostAddComment(string commentText)
{
    // Save the comment
    _commentService.Add(commentText);

    var comments = _commentService.GetAll();
    return Partial("_CommentSection", comments);
}
```

The _CommentSection.cshtml partial might look like this:

```html
<div id="comments">
    @foreach (var comment in Model)
    {
        <div class="comment">
            <p>@comment.Text</p>
            <small>@comment.Timestamp.ToString("g")</small>
        </div>
    }
</div>
```

Even if the server ends up returning more than just the comment list, maybe due to shared layout wrappers or helper components, `hx-select` ensures that only the `#comments` portion is actually used in the swap. This not only keeps your page efficient but also prevents flickers or layout shifts caused by unnecessary updates.

By relying on standard CSS selectors, `hx-select` gives you a familiar and flexible way to grab nested elements. You can use IDs, classes, or even tag names to identify the part of the response you care about. This keeps your Razor Pages logic clean and your frontend interactions highly targeted, which is precisely the kind of control you need when building maintainable, server-driven applications.

## Implementing Out-of-Band Updates with `hx-select-oob`

Sometimes, one update just isn’t enough. You might need to update multiple parts of a page as the result of a single interaction like refreshing a list and adjusting a notification badge simultaneously. That’s where out-of-band (OOB) updates come into play. With `hx-select-oob`, htmx lets you reach beyond your designated `hx-target` and update other elements elsewhere in the DOM, all from the same server response.

Out-of-band updates work by embedding special HTML fragments inside your response, marked with the `hx-oob` attribute. These elements are processed separately and matched to elements already on the page based on their ID. It’s like sending little update packets along with your main response, each one destined for a different part of the UI.

Let’s walk through a practical example. Imagine you’re building a simple messaging interface. When a user submits a message, the message list is updated, but you also want to increment a badge in the navigation bar that shows the count of unread messages. The message list lives in one section of the page; the badge lives in another. Rather than firing off two separate requests or bundling extra JavaScript logic, you can handle both in a single htmx interaction using `hx-select-oob`.

Here’s what the form might look like:

```html
<form hx-post="/Messages?handler=Send"
      hx-target="#message-list"
      hx-swap="outerHTML"
      hx-select="#message-list">
    <input type="text" name="text" />
    <button type="submit">Send</button>
</form>
```

This setup updates the `#message-list` div with the new list of messages. But in your server-side response, you can include an additional fragment targeting the badge, like this:

```html
<div id="message-list">
    <!-- Rendered list of messages -->
</div>

<div id="unread-count" hx-oob="true">
    3
</div>
```

The Razor Page handler could return this via a partial or even inline:

```csharp
public IActionResult OnPostSend(string text)
{
    _messageService.Send(text);
    var messages = _messageService.GetAll();
    var unreadCount = _messageService.GetUnreadCount();

    var html = $@"
        {RenderPartialView("_MessageList", messages)}
        <div id='unread-count' hx-oob='true'>{unreadCount}</div>
    ";

    return Content(html, "text/html");
}
```

When the response comes back, htmx swaps the `#message-list` as usual. Then it finds the `hx-oob="true"` element, looks for an element on the page with a matching ID (`unread-count`), and swaps it using the default strategy (`innerHTML`). The best part is: you didn’t need any client-side code to handle it.

If you need finer control over how the OOB content is inserted, you can combine `hx-select-oob` with `hx-swap`. For instance, if your badge is a container that wraps more than just a number, you can return:

```html
<div id="unread-count" hx-oob="true" hx-swap="outerHTML">
    <span class="badge">3</span>
</div>
```

This ensures the entire badge element gets replaced rather than just its inner content.

Out-of-band updates shine when your server already knows what needs to change. Instead of orchestrating multiple client-initiated requests or tightly coupling components, you let the server dictate the update story in one go. It’s a powerful, clean way to keep your UI in sync, especially in more complex Razor Pages apps where form submissions often affect more than just one element on the screen.

## Combining `hx-select` and `hx-select-oob` for Complex UI Interactions

As you build more interactive and refined web applications with Razor Pages and htmx, you'll eventually need to update several parts of your UI from a single user action. It could be as simple as refreshing a portion of a form or as involved as coordinating updates between a form, a sidebar, and a notification area. Combining `hx-select` and `hx-select-oob` gives you the flexibility to structure complex interactions cleanly, all without layering in a bunch of JavaScript.

A good rule of thumb is to keep your Razor Pages responses modular and self-contained. That means separating different UI components, like a cart summary, a product form, and a badge, into their own partial views. This makes it easier to return just the parts you need selectively and keeps your server logic easy to maintain. When using `hx-select`, you should structure your response to include a container for each section you might target. When adding out-of-band updates, remember that the returned elements must include the `hx-oob="true"` attribute and an ID that matches something already rendered on the page.

Let’s look at a practical example: updating a shopping cart summary when a user adds an item to their cart. The user interacts with a form that adds the product, but you want the cart summary in the sidebar to reflect the new item count and price totals. Here's what the form might look like:

```html
<form hx-post="/Cart?handler=AddItem"
      hx-target="#add-to-cart-form"
      hx-select="#add-to-cart-form">
    <input type="hidden" name="productId" value="42" />
    <button type="submit">Add to Cart</button>
</form>
```

This setup uses `hx-select` to replace only the form markup returned from the server, allowing things like button states or inline validation messages to be updated while keeping the rest of the page intact. The server, however, also returns an out-of-band update for the cart summary:

```html
<div id="add-to-cart-form">
    <button disabled>Added!</button>
</div>

<div id="cart-summary" hx-oob="true">
    <p>Items: 3</p>
    <p>Total: $89.97</p>
</div>
```

The Razor Page handler might look like this:

```csharp
public IActionResult OnPostAddItem(int productId)
{
    _cartService.AddItem(productId);
    var cart = _cartService.GetSummary();

    var formHtml = RenderPartialView("_AddToCartForm", productId);
    var summaryHtml = RenderPartialView("_CartSummary", cart);

    return Content($"{formHtml}{summaryHtml}", "text/html");
}
```

If something goes wrong during development, say, the cart summary doesn’t update, it’s usually a mismatch between the Id of the returned `hx-oob` element and the existing element on the page. Double-check that both IDs are present and that the out-of-band element includes `hx-oob="true"`. Also, ensure the content type returned by the server is text/html, not JSON or plain text, as htmx expects HTML fragments it can parse and inject.

To debug more efficiently, use your browser’s developer tools to inspect the response body and see exactly what HTML was returned. htmx will log messages in the console when it's in debug mode, and these messages can help you spot misconfigured targets or missing elements. You can enable debug mode globally by setting the `HX-DEBUG` header or adding `hx-debug="true"` on any triggering element.

By combining `hx-select` and `hx-select-oob`, you can manage sophisticated UI updates in a way that keeps your Razor Pages clean and your front end reactive. This approach encourages good separation of concerns: your server builds the HTML, your client inserts it where needed, and you never have to touch a JavaScript framework. This approach scales naturally as your app grows, giving you the confidence to tackle more interactive features without increasing front-end complexity.

## Optimizing Scoped Updates for Performance and Usability

Scoped updates can breathe life into a static page, but without thoughtful implementation, they can also create unexpected side effects. While htmx makes dynamic updates remarkably easy, the best experiences come from using `hx-select` and `hx-select-oob` with performance and usability in mind. It’s not just about what you update, it’s about updating only what you need, and doing so in a way that keeps your app fast, accessible, and maintainable.

Every DOM swap has a cost. When you return large HTML fragments and swap entire sections of the page, even if they haven’t really changed, browsers still have to parse and re-render that content. This can lead to flickers, layout jumps, or loss of scroll position. With scoped updates, you can fine-tune your responses so htmx only injects small, relevant parts. For example, instead of updating a whole div that contains both a form and a result list, consider splitting that into two separate containers and updating only what actually changes.

Here’s a quick illustration. Suppose you have a search form that returns results and also updates a count badge. Instead of wrapping both the form and results in a single target, you might use this layout:

```html
<form hx-post="/Search"
      hx-target="#search-results"
      hx-select="#search-results">
    <input type="text" name="query" />
    <button type="submit">Search</button>
</form>

<div id="search-results">
    <!-- Results rendered here -->
</div>

<div id="result-count" aria-live="polite">
    <!-- Count badge updated here -->
</div>
```

The server returns the full markup for both `#search-results` and `#result-count`, with the latter marked as out-of-band. This way, the visible update is fast and scoped, while screen readers are notified of the change via aria-live, helping maintain accessibility.

Speaking of accessibility, it’s easy to overlook how updates affect keyboard users and screen reader navigation. Swapping elements without preserving focus can disorient users or reset their progress. Always test how your updates behave when tabbing through inputs, using screen readers, or resizing the page. For dynamic forms, avoid resetting inputs unnecessarily, and consider preserving focus using a bit of server-side logic to track the active element and reapply it post-render. Razor Pages gives you enough control to render attributes conditionally, like autofocus, to help with this.

Reusable components are key to managing scoped updates effectively. Define your partials with clear entry points, like wrapping lists, summaries, or notifications in distinct IDs. Avoid reusing the same ID for different purposes, and ensure each component handles its own rendering logic. By doing this, you make your server responses composable and predictable, which in turn simplifies your testing and debugging workflow.

And when debugging, lean on the htmx dev tools. Enable debug mode to see what elements are being swapped, which `hx-select` or `hx-oob` selectors are being used, and whether any errors were thrown during the update. If an element doesn’t update, it’s usually a selector mismatch or a missing ID on the client side. Keep your developer tools open and inspect the actual response payload to ensure it contains the expected elements.

As you continue refining your UI with htmx, the next natural step is to deepen how forms behave. You’ve already used `hx-post` and basic `hx-select` interactions, but htmx offers even more control through attributes like `hx-params`, `hx-vals`, and `hx-validate`. In the next chapter, we’ll explore how to use these features to add context, client-side validation, and dynamic parameter tweaking, turning your Razor Pages forms into powerful, responsive, and flexible components.