---
order: 24
icon: stack
label: Chap 7 - Working with hx-target and hx-swap
meta:
title: "Working with hx-target and hx-swap"
---

# Working with `hx-target` and `hx-swap`

Every htmx request updates something on the page. The question is: what gets updated and how? By default, htmx replaces the inner content of the element that triggered the request. That works for simple cases. But real applications need precision. You need to update a sidebar when a form submits. You need to append items to a list without clearing existing content. You need to remove elements entirely when users delete them.

The `hx-target` attribute controls where the response goes. The `hx-swap` attribute controls how the response is inserted. Together, they give you complete control over DOM updates. This chapter covers both attributes in depth, including relative targeting, all swap strategies, swap modifiers, and out-of-band updates for complex multi-element changes.

## Understanding `hx-target`

The `hx-target` attribute accepts a CSS selector that identifies where the server response should be placed. Without it, htmx targets the element that triggered the request.

### Basic ID Targeting

The most common pattern targets an element by ID:

```html
<button hx-get="/Products?handler=List" hx-target="#product-list">
    Load Products
</button>

<div id="product-list">
    <!-- Products will appear here -->
</div>
```

When clicked, the button fetches content from the server and inserts it into `#product-list`. The button itself remains unchanged.

### CSS Selector Targeting

Any valid CSS selector works with `hx-target`:

```html
<!-- Target by class (first matching element) -->
<button hx-get="/Content" hx-target=".content-area">Load</button>

<!-- Target by attribute -->
<button hx-get="/Content" hx-target="[data-content-area]">Load</button>

<!-- Target by tag -->
<button hx-get="/Content" hx-target="main">Load</button>
```

### The `this` Keyword

Use `this` to target the triggering element itself:

```html
<button hx-get="/Button?handler=Toggle"
        hx-target="this"
        hx-swap="outerHTML">
    Click to Toggle
</button>
```

The server returns a replacement button, and htmx swaps out the entire element. This pattern works well for toggle states, inline editing, and self-updating components.

### Relative Targeting

htmx provides special selectors for targeting elements relative to the trigger. These are invaluable when you have repeating components like list items or table rows.

**`closest`** finds the nearest ancestor matching the selector:

```html
<div class="card">
    <h3>Product Name</h3>
    <button hx-delete="/Products?handler=Remove&amp;id=5"
            hx-target="closest .card"
            hx-swap="outerHTML">
        Delete
    </button>
</div>
```

Clicking delete removes the entire card, including its contents.

**`find`** targets a descendant of the triggering element:

```html
<div class="card">
    <button hx-get="/Products?handler=Details&amp;id=5"
            hx-target="find .card-body">
        Load Details
    </button>
    <div class="card-body">
        <!-- Details load here -->
    </div>
</div>
```

**`next`** targets the next sibling matching the selector:

```html
<button hx-get="/Help?handler=Topic&amp;id=intro"
        hx-target="next .help-content">
    Show Help
</button>
<div class="help-content"></div>
```

**`previous`** targets the previous sibling matching the selector:

```html
<div class="status-message"></div>
<button hx-post="/Actions?handler=Run"
        hx-target="previous .status-message">
    Run Action
</button>
```

### Targeting the Document Body

For full-page updates or navigation-style interactions:

```html
<a hx-get="/Dashboard" 
   hx-target="body" 
   hx-push-url="true">
    Go to Dashboard
</a>
```

This replaces the entire body content and updates the browser URL, simulating navigation without a full page reload.

## Understanding `hx-swap`

The `hx-swap` attribute determines how the response content is inserted into the target. The default is `innerHTML`, but htmx provides many options for different scenarios.

### `innerHTML` (Default)

Replaces the target's inner content while keeping the target element itself:

```html
<div hx-get="/Content" hx-target="#container" hx-swap="innerHTML">
    Load Content
</div>

<div id="container">
    <p>This paragraph will be replaced</p>
    <p>This one too</p>
</div>
```

After the swap, `#container` still exists but contains only the server response.

### `outerHTML`

Replaces the entire target element, including the element itself:

```html
<div id="task-5" class="task">
    <span>Complete project</span>
    <button hx-patch="/Tasks?handler=Complete&amp;id=5"
            hx-target="#task-5"
            hx-swap="outerHTML">
        Complete
    </button>
</div>
```

The server returns a new version of the task element (perhaps with a "completed" class), and it replaces the entire `#task-5` div.

### Position-Based Swaps

These options insert content relative to the target without replacing it:

**`beforebegin`** inserts before the target element:

```html
<ul id="notifications">
    <li>Existing notification</li>
</ul>

<button hx-post="/Notifications?handler=Add"
        hx-target="#notifications"
        hx-swap="beforebegin">
    Add Alert Above List
</button>
```

The new content appears before the `<ul>`, as a sibling.

**`afterbegin`** inserts inside the target, before its first child:

```html
<ul id="messages">
    <li>Older message</li>
</ul>

<button hx-get="/Messages?handler=Latest"
        hx-target="#messages"
        hx-swap="afterbegin">
    Load New Messages
</button>
```

New messages appear at the top of the list.

**`beforeend`** inserts inside the target, after its last child:

```html
<ul id="log">
    <li>First entry</li>
</ul>

<button hx-get="/Log?handler=More"
        hx-target="#log"
        hx-swap="beforeend">
    Load More
</button>
```

New entries append to the bottom. This is the standard pattern for infinite scroll and "load more" features.

**`afterend`** inserts after the target element:

```html
<div id="comment-5">
    <p>Original comment</p>
    <button hx-get="/Comments?handler=Replies&amp;parentId=5"
            hx-target="#comment-5"
            hx-swap="afterend">
        Show Replies
    </button>
</div>
```

Replies appear after the comment, as siblings.

### `delete`

Removes the target element entirely:

```html
<div id="item-5" class="item">
    <span>Item to remove</span>
    <button hx-delete="/Items?handler=Remove&amp;id=5"
            hx-target="closest .item"
            hx-swap="delete">
        Remove
    </button>
</div>
```

The target element disappears from the DOM. The server response is ignored (though you should still return a 200 status).

### `none`

Performs no swap. The server response is received but not inserted into the DOM:

```html
<button hx-post="/Analytics?handler=Track"
        hx-vals='{"event": "button_click"}'
        hx-swap="none">
    Track This Click
</button>
```

Use this for side-effect-only requests like analytics, logging, or triggering background processes.

## Swap Modifiers

You can add modifiers to `hx-swap` to control timing, scrolling, and focus behavior.

### Timing Modifiers

**`swap:`** delays the swap operation:

```html
<div hx-get="/Content" hx-swap="innerHTML swap:500ms">
    <!-- Content swaps 500ms after response arrives -->
</div>
```

**`settle:`** delays the settle step (when htmx adds classes like `htmx-settling`):

```html
<div hx-get="/Content" hx-swap="innerHTML settle:300ms">
    <!-- Settling classes applied 300ms after swap -->
</div>
```

### Scroll Modifiers

**`scroll:`** scrolls the target or window after the swap:

```html
<!-- Scroll target to top -->
<div hx-get="/Content" hx-swap="innerHTML scroll:top">

<!-- Scroll target to bottom -->
<div hx-get="/Content" hx-swap="innerHTML scroll:bottom">

<!-- Scroll window to top -->
<div hx-get="/Content" hx-swap="innerHTML scroll:window:top">
```

**`show:`** ensures the target is visible after the swap:

```html
<!-- Show target at top of viewport -->
<div hx-get="/Content" hx-swap="innerHTML show:top">

<!-- Show target at bottom of viewport -->
<div hx-get="/Content" hx-swap="innerHTML show:bottom">
```

### Focus Modifier

**`focus-scroll:`** controls whether to scroll when focusing an element:

```html
<div hx-get="/Form" hx-swap="innerHTML focus-scroll:true">
    <!-- After swap, scroll to focused element -->
</div>
```

### Combining Modifiers

Multiple modifiers can be combined:

```html
<div hx-get="/Content" 
     hx-swap="innerHTML swap:200ms settle:100ms scroll:top show:top">
    Load Content
</div>
```

## Filtering Responses with `hx-select`

Sometimes the server returns more HTML than you need. The `hx-select` attribute extracts specific content from the response:

```html
<button hx-get="/FullPage" 
        hx-select="#just-this-section" 
        hx-target="#container">
    Load Section Only
</button>
```

Even if `/FullPage` returns an entire page, htmx extracts only the element matching `#just-this-section` and swaps that into `#container`.

This is useful when:
- Reusing existing page handlers that return full pages
- Extracting specific content from external sources
- Avoiding duplicate handler methods for partial vs full responses

## Out-of-Band Updates

Standard htmx updates target a single element. But what if one action needs to update multiple unrelated parts of the page? A form submission might need to update a success message, a notification count in the header, and an item in a sidebar.

Out-of-band (OOB) updates solve this. Elements in the server response marked with `hx-swap-oob="true"` are swapped into matching elements on the page by ID, regardless of the original target.

### Basic OOB Example

**Main page:**

```html
<header>
    <span id="notification-count">5</span> notifications
</header>

<main>
    <form hx-post="/Messages?handler=Send" hx-target="#result">
        @Html.AntiForgeryToken()
        <textarea name="message" required></textarea>
        <button type="submit">Send Message</button>
    </form>
    <div id="result"></div>
</main>
```

**Server response:**

```html
<!-- Primary response goes to #result -->
<div class="success">Message sent!</div>

<!-- OOB update goes to #notification-count -->
<span id="notification-count" hx-swap-oob="true">6</span>
```

htmx processes the response as follows:
1. The first element (without `hx-swap-oob`) goes to the target (`#result`)
2. Elements with `hx-swap-oob="true"` find their matching elements by ID and replace them

### OOB with Server-Side Code

Here is a complete Razor Pages example:

```csharp
public class MessagesModel : PageModel
{
    private readonly IMessageService _messageService;
    private readonly INotificationService _notificationService;

    public MessagesModel(IMessageService messageService, INotificationService notificationService)
    {
        _messageService = messageService;
        _notificationService = notificationService;
    }

    public IActionResult OnPostSend(string message)
    {
        _messageService.Send(message);
        var notificationCount = _notificationService.GetUnreadCount();

        var html = new StringBuilder();
        
        // Primary response
        html.Append("<div class=\"success\">Message sent successfully!</div>");
        
        // OOB update for notification count
        html.Append($"<span id=\"notification-count\" hx-swap-oob=\"true\">{notificationCount}</span>");

        return Content(html.ToString(), "text/html");
    }
}
```

### OOB Swap Modes

You can specify how OOB elements are swapped:

```html
<!-- Replace the element (default) -->
<div id="sidebar" hx-swap-oob="true">New sidebar content</div>

<!-- Append to the element -->
<ul id="activity-log" hx-swap-oob="beforeend">
    <li>New activity entry</li>
</ul>

<!-- Prepend to the element -->
<div id="alerts" hx-swap-oob="afterbegin">
    <div class="alert">New alert!</div>
</div>

<!-- Replace inner content only -->
<div id="status" hx-swap-oob="innerHTML">Updated status</div>
```

### Multiple OOB Updates

A single response can include multiple OOB elements:

```html
<!-- Primary response -->
<tr id="order-123">
    <td>Order #123</td>
    <td>Shipped</td>
</tr>

<!-- OOB: Update order count in sidebar -->
<span id="pending-count" hx-swap-oob="true">4</span>

<!-- OOB: Update status in header -->
<div id="last-update" hx-swap-oob="true">Updated just now</div>

<!-- OOB: Append to activity log -->
<li hx-swap-oob="beforeend:#activity-log">Order #123 marked as shipped</li>
```

### `hx-select-oob`

For more control, use `hx-select-oob` on the triggering element to specify which parts of the response should be treated as OOB:

```html
<button hx-get="/Dashboard?handler=Refresh"
        hx-target="#main-content"
        hx-select-oob="#sidebar-stats,#header-alerts">
    Refresh Dashboard
</button>
```

htmx will:
1. Swap the response into `#main-content` (the target)
2. Extract `#sidebar-stats` from the response and swap it into the matching element on the page
3. Extract `#header-alerts` from the response and swap it into the matching element on the page

## Practical Examples

### Infinite Scroll

Load more content as the user scrolls:

```html
@page
@model FeedModel

<div id="feed">
    @foreach (var post in Model.Posts)
    {
        <partial name="_PostCard" model="post" />
    }
</div>

<!-- Trigger element at bottom of list -->
<div hx-get="/Feed?handler=More&amp;page=@(Model.CurrentPage + 1)"
     hx-target="#feed"
     hx-swap="beforeend"
     hx-trigger="revealed"
     hx-indicator="#loading">
    <span id="loading" class="htmx-indicator">Loading more posts...</span>
</div>
```

```csharp
public IActionResult OnGetMore(int page)
{
    var posts = _postService.GetPage(page, pageSize: 10);
    
    if (!posts.Any())
    {
        return Content("", "text/html"); // No more posts
    }

    var html = new StringBuilder();
    foreach (var post in posts)
    {
        html.Append(RenderPartial("_PostCard", post));
    }
    
    // Include next trigger if more posts exist
    if (_postService.HasMore(page))
    {
        html.Append($@"
            <div hx-get=""/Feed?handler=More&page={page + 1}""
                 hx-target=""#feed""
                 hx-swap=""beforeend""
                 hx-trigger=""revealed"">
            </div>");
    }

    return Content(html.ToString(), "text/html");
}
```

### Live Search with Results Panel

```html
<div class="search-container">
    <input type="text" 
           name="query"
           placeholder="Search products..."
           hx-get="/Search?handler=Results"
           hx-target="#search-results"
           hx-trigger="keyup changed delay:300ms"
           hx-indicator="#search-spinner" />
    <span id="search-spinner" class="htmx-indicator">Searching...</span>
</div>

<div id="search-results"></div>
```

```csharp
public IActionResult OnGetResults(string query)
{
    if (string.IsNullOrWhiteSpace(query))
    {
        return Content("", "text/html");
    }

    var results = _searchService.Search(query);
    return Partial("_SearchResults", results);
}
```

### Inline Edit with Cancel

```html
<!-- Display mode -->
<div id="user-name-5" class="editable">
    <span>John Doe</span>
    <button hx-get="/Users?handler=EditName&amp;id=5"
            hx-target="#user-name-5"
            hx-swap="outerHTML">
        Edit
    </button>
</div>
```

**_UserNameEdit.cshtml:**

```html
@model UserNameEditViewModel

<div id="user-name-@Model.Id" class="editing">
    <form hx-put="/Users?handler=UpdateName&amp;id=@Model.Id"
          hx-target="#user-name-@Model.Id"
          hx-swap="outerHTML">
        @Html.AntiForgeryToken()
        <input type="text" name="name" value="@Model.Name" required />
        <button type="submit">Save</button>
        <button type="button"
                hx-get="/Users?handler=CancelEdit&amp;id=@Model.Id"
                hx-target="#user-name-@Model.Id"
                hx-swap="outerHTML">
            Cancel
        </button>
    </form>
</div>
```

```csharp
public IActionResult OnGetEditName(int id)
{
    var user = _userService.GetById(id);
    return Partial("_UserNameEdit", new UserNameEditViewModel { Id = id, Name = user.Name });
}

public IActionResult OnPutUpdateName(int id, string name)
{
    var user = _userService.GetById(id);
    user.Name = name;
    _userService.Update(user);
    return Partial("_UserNameDisplay", user);
}

public IActionResult OnGetCancelEdit(int id)
{
    var user = _userService.GetById(id);
    return Partial("_UserNameDisplay", user);
}
```

### Shopping Cart with Multiple Updates

```html
<header>
    <span id="cart-count">3</span> items in cart
</header>

<div id="cart-items">
    @foreach (var item in Model.Items)
    {
        <partial name="_CartItem" model="item" />
    }
</div>

<div id="cart-total">
    Total: @Model.Total.ToString("C")
</div>
```

**_CartItem.cshtml:**

```html
@model CartItem

<div id="cart-item-@Model.Id" class="cart-item">
    <span>@Model.Name</span>
    <span>@Model.Price.ToString("C")</span>
    <button hx-delete="/Cart?handler=Remove&amp;id=@Model.Id"
            hx-target="#cart-item-@Model.Id"
            hx-swap="outerHTML">
        Remove
    </button>
</div>
```

```csharp
public IActionResult OnDeleteRemove(int id)
{
    _cartService.RemoveItem(id);
    var cart = _cartService.GetCart();

    var html = new StringBuilder();
    
    // Primary response: empty string to remove the item
    // (outerHTML with empty content removes the element)
    
    // OOB: Update cart count
    html.Append($"<span id=\"cart-count\" hx-swap-oob=\"true\">{cart.ItemCount}</span>");
    
    // OOB: Update cart total
    html.Append($"<div id=\"cart-total\" hx-swap-oob=\"true\">Total: {cart.Total:C}</div>");

    return Content(html.ToString(), "text/html");
}
```

## Debugging Target and Swap Issues

### Common Problems

**Target element not found**

htmx silently fails if the target selector does not match any element. Verify:
- The element exists in the DOM
- The ID or class is spelled correctly
- The element is not inside a template or script tag

**Wrong element updated**

Check for duplicate IDs on the page. Each ID should be unique.

**OOB elements not updating**

Verify:
- The element ID in the response matches an element on the page exactly
- The `hx-swap-oob` attribute is on the element in the response, not on the page

**Content appears but in wrong position**

Review your `hx-swap` value. Common confusion:
- `beforeend` adds inside the target, at the end
- `afterend` adds outside the target, after it

### Browser Developer Tools

Open the Network tab to inspect responses. Check:
- Is the response HTML valid?
- Does it contain the expected content?
- For OOB, do element IDs match?

Use the Elements tab to watch DOM changes in real-time as htmx processes responses.

### htmx Logging

Enable verbose logging:

```html
<script>
htmx.logAll();
</script>
```

This outputs every htmx event to the console, including target resolution and swap operations.

## Summary

This chapter covered `hx-target` and `hx-swap` in depth:

- **`hx-target`** accepts CSS selectors, including `this` and relative selectors (`closest`, `find`, `next`, `previous`)
- **`hx-swap`** controls insertion: `innerHTML`, `outerHTML`, `beforebegin`, `afterbegin`, `beforeend`, `afterend`, `delete`, `none`
- **Swap modifiers** control timing (`swap:`, `settle:`), scrolling (`scroll:`, `show:`), and focus (`focus-scroll:`)
- **`hx-select`** filters which part of the response to use
- **`hx-swap-oob`** enables updating multiple unrelated elements from a single response
- **`hx-select-oob`** provides fine-grained control over OOB element selection

These attributes give you precise control over how your UI responds to server updates, enabling complex interactions without custom JavaScript.

## Preview of Next Chapter

Chapter 8 explores `hx-trigger` and event handling. You will learn how to control when requests fire, respond to custom events, add conditions and modifiers, and build responsive interfaces that react to user behavior in sophisticated ways.
