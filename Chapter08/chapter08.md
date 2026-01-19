---
order: 23
icon: stack
label: Chap 8 - Handling Events with hx-trigger and hx-on
meta:
title: "Handling Events with hx-trigger and hx-on"
---

# Handling Events with `hx-trigger` and `hx-on`

Previous chapters focused on what happens when htmx requests complete: where content goes, how it gets inserted. This chapter focuses on when requests fire in the first place. The `hx-trigger` attribute gives you precise control over the events that initiate requests. The `hx-on` attribute lets you run JavaScript in response to htmx lifecycle events. Together, they let you build interactions that respond to user behavior in sophisticated ways.

Default triggers work for most cases. Buttons fire on click. Forms fire on submit. Inputs fire on change. But real applications need more: debounced search that waits for users to stop typing, polling that checks for updates every few seconds, lazy loading that fetches content when elements scroll into view, keyboard shortcuts that respond to specific key combinations. This chapter covers all of these patterns and more.

## Understanding `hx-trigger`

The `hx-trigger` attribute specifies which events cause htmx to send a request. Without it, htmx uses sensible defaults based on the element type.

### Default Triggers

htmx assigns default triggers based on element type:

| Element | Default Trigger |
|---------|-----------------|
| `<button>` | `click` |
| `<a>` | `click` |
| `<form>` | `submit` |
| `<input>` | `change` |
| `<select>` | `change` |
| `<textarea>` | `change` |
| Other elements | `click` |

For many interactions, you never need to specify `hx-trigger` at all:

```html
<!-- Fires on click (default for button) -->
<button hx-get="/Products?handler=List" hx-target="#product-list">
    Load Products
</button>

<!-- Fires on submit (default for form) -->
<form hx-post="/Contact?handler=Submit" hx-target="#result">
    @Html.AntiForgeryToken()
    <input type="text" name="message" required />
    <button type="submit">Send</button>
</form>
<div id="result"></div>
```

### Standard DOM Events

Any DOM event can trigger a request:

```html
<!-- Mouse events -->
<div hx-get="/Preview" hx-trigger="mouseenter">
    Hover for preview
</div>

<!-- Keyboard events -->
<input hx-get="/Validate" hx-trigger="keyup" hx-target="#validation" />

<!-- Focus events -->
<input hx-get="/Suggestions" hx-trigger="focus" hx-target="#suggestions" />

<!-- Form events -->
<input hx-get="/Search" hx-trigger="input" hx-target="#results" />
```

### Multiple Triggers

Combine multiple events with commas:

```html
<input hx-get="/Search?handler=Results" 
       hx-trigger="keyup, search"
       hx-target="#results" />
```

This fires on both keyup events and when the user clicks the search icon in a search-type input.

## Trigger Modifiers

Modifiers refine when and how triggers fire. Add them after the event name.

### `changed` Modifier

Only fires if the element's value actually changed:

```html
<input hx-get="/Search?handler=Results" 
       hx-trigger="keyup changed"
       hx-target="#results" />
```

Without `changed`, every keyup fires a request, even arrow keys or shift. With `changed`, requests only fire when the input value differs from before.

### `delay:` Modifier

Waits a specified time after the event before firing. If another event occurs during the delay, the timer resets:

```html
<input hx-get="/Search?handler=Results" 
       hx-trigger="keyup changed delay:300ms"
       hx-target="#results"
       placeholder="Search..." />
```

The request fires 300ms after the user stops typing. This debouncing pattern prevents flooding your server with requests on every keystroke.

### `throttle:` Modifier

Limits how often requests can fire. Unlike `delay`, it fires immediately on the first event, then ignores subsequent events for the specified duration:

```html
<div hx-get="/Position?handler=Update" 
     hx-trigger="mousemove throttle:100ms"
     hx-target="#coordinates">
    Move mouse here
</div>
```

This fires at most once every 100ms, even if the mouse moves continuously.

### `once` Modifier

Fires only once, then stops listening:

```html
<div hx-get="/Welcome?handler=Message" 
     hx-trigger="load once"
     hx-target="#welcome-area">
    Loading welcome message...
</div>
```

After the first request completes, the element no longer responds to the trigger.

### `from:` Modifier

Listens for events from a different element:

```html
<div hx-get="/Content?handler=Load" 
     hx-trigger="click from:#load-button"
     hx-target="this">
    Content loads when button is clicked
</div>

<button id="load-button">Load Content</button>
```

The div makes a request when the button is clicked. This decouples the trigger source from the target element.

You can also listen for events from the document or window:

```html
<div hx-get="/Status?handler=Check" 
     hx-trigger="visibilitychange from:document"
     hx-target="this">
    Status updates when tab becomes visible
</div>
```

### `target:` Modifier

Filters events based on the event target:

```html
<table hx-get="/Items?handler=Details" 
       hx-trigger="click target:td"
       hx-target="#details">
    <tr><td>Item 1</td><td>Details 1</td></tr>
    <tr><td>Item 2</td><td>Details 2</td></tr>
</table>
```

Only clicks on `<td>` elements trigger the request, not clicks on the table itself or other elements.

### `consume` Modifier

Prevents the event from propagating to parent elements:

```html
<div hx-get="/Outer" hx-trigger="click">
    <button hx-get="/Inner" hx-trigger="click consume">
        Click Me
    </button>
</div>
```

Clicking the button only fires the inner request. Without `consume`, both requests would fire.

### `queue:` Modifier

Controls how events queue when a request is already in flight:

```html
<!-- Queue all events (default) -->
<button hx-post="/Action" hx-trigger="click queue:all">Submit</button>

<!-- Queue only the first event -->
<button hx-post="/Action" hx-trigger="click queue:first">Submit</button>

<!-- Queue only the last event -->
<button hx-post="/Action" hx-trigger="click queue:last">Submit</button>

<!-- Drop all events while request is in flight -->
<button hx-post="/Action" hx-trigger="click queue:none">Submit</button>
```

For most cases, `queue:last` or `queue:none` prevents duplicate submissions.

## Special Triggers

htmx provides special trigger values for common scenarios.

### `load` Trigger

Fires when the element is loaded into the DOM:

```html
<div hx-get="/Dashboard?handler=Stats" 
     hx-trigger="load"
     hx-target="this">
    Loading dashboard stats...
</div>
```

This makes the request immediately when the page loads, useful for lazy-loading initial content.

### `revealed` Trigger

Fires when the element scrolls into the viewport:

```html
<div hx-get="/Products?handler=More&amp;page=2" 
     hx-trigger="revealed"
     hx-target="#product-list"
     hx-swap="beforeend">
    <span class="htmx-indicator">Loading more products...</span>
</div>
```

This is the foundation for infinite scroll and lazy loading. The request fires only when the user scrolls the element into view.

### `intersect` Trigger

Similar to `revealed` but offers more control through Intersection Observer options:

```html
<!-- Fire when 50% of element is visible -->
<div hx-get="/Content" 
     hx-trigger="intersect threshold:0.5">
    Loading...
</div>

<!-- Fire when element enters root margin -->
<div hx-get="/Content" 
     hx-trigger="intersect root:.container rootMargin:100px">
    Loading...
</div>
```

### `every` Trigger

Creates polling intervals:

```html
<div hx-get="/Notifications?handler=Count" 
     hx-trigger="every 30s"
     hx-target="this">
    0 notifications
</div>
```

This checks for new notifications every 30 seconds. Combine with other triggers:

```html
<div hx-get="/Status?handler=Check" 
     hx-trigger="load, every 10s"
     hx-target="this">
    Checking status...
</div>
```

The status loads immediately, then refreshes every 10 seconds.

## Event Filters

Filter events based on conditions using bracket syntax:

### Key Filters

```html
<!-- Only fire on Enter key -->
<input hx-get="/Search?handler=Results" 
       hx-trigger="keyup[key=='Enter']"
       hx-target="#results" />

<!-- Only fire on Escape key -->
<div hx-get="/Modal?handler=Close" 
     hx-trigger="keyup[key=='Escape'] from:body">
    Modal content
</div>
```

### Modifier Key Filters

```html
<!-- Ctrl+Click -->
<button hx-post="/Save?handler=Quick" 
        hx-trigger="click[ctrlKey]">
    Ctrl+Click to Quick Save
</button>

<!-- Shift+Enter -->
<textarea hx-post="/Message?handler=Send" 
          hx-trigger="keyup[key=='Enter' && shiftKey]">
</textarea>

<!-- Any modifier key -->
<button hx-post="/Action" 
        hx-trigger="click[!ctrlKey && !shiftKey && !altKey]">
    Click (no modifiers)
</button>
```

### Custom Conditions

Any JavaScript expression that returns a boolean works:

```html
<!-- Only when input has content -->
<input id="search" 
       hx-get="/Search?handler=Results" 
       hx-trigger="keyup[this.value.length > 2] changed delay:300ms"
       hx-target="#results" />

<!-- Only for specific targets -->
<ul hx-delete="/Items?handler=Remove" 
    hx-trigger="click[event.target.classList.contains('delete-btn')]"
    hx-target="closest li"
    hx-swap="outerHTML">
    <li>Item 1 <button class="delete-btn">X</button></li>
    <li>Item 2 <button class="delete-btn">X</button></li>
</ul>
```

## Understanding `hx-on`

The `hx-on` attribute executes JavaScript in response to events. It uses the syntax `hx-on:event="javascript"` for standard DOM events and `hx-on::event="javascript"` (double colon) for htmx-specific events.

### Standard DOM Events

```html
<!-- Single colon for DOM events -->
<button hx-get="/Action" 
        hx-on:click="console.log('Button clicked')">
    Click Me
</button>

<input hx-on:focus="this.select()" 
       hx-on:blur="validateField(this)" />
```

### htmx Lifecycle Events

htmx fires events throughout the request lifecycle. Use double colon to listen for them:

```html
<!-- Before request is sent -->
<form hx-post="/Submit" 
      hx-on::before-request="showSpinner()">
    @Html.AntiForgeryToken()
    <!-- fields -->
</form>

<!-- After request completes -->
<div hx-get="/Content" 
     hx-on::after-request="hideSpinner()">
    Loading...
</div>

<!-- After content is swapped into DOM -->
<div hx-get="/Content" 
     hx-on::after-swap="initializeComponents(this)">
    Loading...
</div>

<!-- After settling (CSS transitions complete) -->
<div hx-get="/Content" 
     hx-on::after-settle="this.classList.add('loaded')">
    Loading...
</div>
```

### Key htmx Events

| Event | When it Fires |
|-------|---------------|
| `htmx:configRequest` | Before request, can modify headers/parameters |
| `htmx:beforeRequest` | Just before request is sent |
| `htmx:afterRequest` | After request completes (success or failure) |
| `htmx:beforeSwap` | Before content is swapped, can modify swap behavior |
| `htmx:afterSwap` | After content is swapped into DOM |
| `htmx:afterSettle` | After CSS transitions complete |
| `htmx:responseError` | When server returns error status |
| `htmx:sendError` | When request fails to send (network error) |

### Form Validation with `hx-on`

Validate before sending the request:

```html
<form hx-post="/Contact?handler=Submit" 
      hx-target="#result"
      hx-on::before-request="return validateForm(event)">
    @Html.AntiForgeryToken()
    <input type="email" id="email" name="email" required />
    <textarea id="message" name="message" required></textarea>
    <button type="submit">Send</button>
</form>
<div id="result"></div>

<script>
function validateForm(event) {
    var email = document.getElementById('email').value;
    var message = document.getElementById('message').value;
    
    if (!email.includes('@')) {
        alert('Please enter a valid email');
        event.preventDefault();
        return false;
    }
    
    if (message.length < 10) {
        alert('Message must be at least 10 characters');
        event.preventDefault();
        return false;
    }
    
    return true;
}
</script>
```

### Animations with `hx-on`

Trigger animations when content loads:

```html
<style>
.fade-in {
    animation: fadeIn 0.3s ease-in;
}
@keyframes fadeIn {
    from { opacity: 0; }
    to { opacity: 1; }
}
</style>

<div hx-get="/Content?handler=Load" 
     hx-trigger="click from:#load-btn"
     hx-on::after-swap="this.classList.add('fade-in')">
    Content will fade in when loaded
</div>

<button id="load-btn">Load Content</button>
```

## Server-Triggered Events with HX-Trigger Header

The `HX-Trigger` response header enables server-initiated events. When your server includes this header, htmx dispatches custom events on the client that other elements can listen for.

### Basic Server Event

```csharp
public IActionResult OnPostCreate(string name)
{
    _itemService.Create(name);
    
    // Tell htmx to trigger an event on the client
    Response.Headers.Append("HX-Trigger", "itemCreated");
    
    return Content("<div class=\"success\">Item created!</div>", "text/html");
}
```

Other elements can listen for this event:

```html
<!-- This refreshes when itemCreated event fires -->
<div hx-get="/Items?handler=List" 
     hx-trigger="load, itemCreated from:body"
     hx-target="this">
    Loading items...
</div>

<!-- Form that triggers the event -->
<form hx-post="/Items?handler=Create" hx-target="#create-result">
    @Html.AntiForgeryToken()
    <input type="text" name="name" required />
    <button type="submit">Create Item</button>
</form>
<div id="create-result"></div>
```

When the form submits and the server returns the `HX-Trigger: itemCreated` header, the items list automatically refreshes.

### Multiple Events

Trigger multiple events:

```csharp
Response.Headers.Append("HX-Trigger", "itemCreated, statsUpdated, notificationAdded");
```

### Events with Data

Pass data with events using JSON:

```csharp
var triggerData = JsonSerializer.Serialize(new 
{
    itemCreated = new { id = item.Id, name = item.Name },
    statsUpdated = new { total = _itemService.Count() }
});

Response.Headers.Append("HX-Trigger", triggerData);
```

Access the data in JavaScript:

```html
<div hx-on:itemCreated="console.log('Created:', event.detail.id)">
    <!-- content -->
</div>
```

### Timing Variations

The `HX-Trigger` header has timing variants:

```csharp
// Fire immediately (default)
Response.Headers.Append("HX-Trigger", "myEvent");

// Fire after swap
Response.Headers.Append("HX-Trigger-After-Swap", "myEvent");

// Fire after settle (CSS transitions complete)
Response.Headers.Append("HX-Trigger-After-Settle", "myEvent");
```

## Practical Examples

### Live Search with Debouncing

```html
<div class="search-container">
    <input type="text" 
           name="query"
           placeholder="Search products..."
           hx-get="/Search?handler=Results"
           hx-trigger="keyup changed delay:300ms, search"
           hx-target="#search-results"
           hx-indicator="#search-spinner" />
    <span id="search-spinner" class="htmx-indicator">Searching...</span>
</div>
<div id="search-results"></div>
```

```csharp
public IActionResult OnGetResults(string query)
{
    if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
    {
        return Content("", "text/html");
    }

    var results = _productService.Search(query);
    return Partial("_SearchResults", results);
}
```

### Infinite Scroll

```html
<div id="feed">
    @foreach (var post in Model.Posts)
    {
        <partial name="_PostCard" model="post" />
    }
</div>

<div hx-get="/Feed?handler=More&amp;page=@(Model.CurrentPage + 1)"
     hx-trigger="revealed"
     hx-target="#feed"
     hx-swap="beforeend"
     hx-indicator="#loading">
    <div id="loading" class="htmx-indicator">Loading more...</div>
</div>
```

```csharp
public IActionResult OnGetMore(int page)
{
    var posts = _postService.GetPage(page, pageSize: 10);
    
    if (!posts.Any())
    {
        return Content("", "text/html");
    }

    return Partial("_PostBatch", new PostBatchViewModel 
    { 
        Posts = posts, 
        NextPage = page + 1,
        HasMore = _postService.HasMorePages(page)
    });
}
```

**_PostBatch.cshtml:**

```html
@model PostBatchViewModel

@foreach (var post in Model.Posts)
{
    <partial name="_PostCard" model="post" />
}

@if (Model.HasMore)
{
    <div hx-get="/Feed?handler=More&amp;page=@Model.NextPage"
         hx-trigger="revealed"
         hx-target="#feed"
         hx-swap="beforeend">
        <span class="htmx-indicator">Loading more...</span>
    </div>
}
```

### Auto-Refreshing Dashboard

```html
<div class="dashboard">
    <!-- Stats refresh every 30 seconds -->
    <div id="stats-panel"
         hx-get="/Dashboard?handler=Stats"
         hx-trigger="load, every 30s"
         hx-target="this">
        Loading stats...
    </div>

    <!-- Notifications refresh every 10 seconds -->
    <div id="notifications"
         hx-get="/Dashboard?handler=Notifications"
         hx-trigger="load, every 10s, notificationAdded from:body"
         hx-target="this">
        Loading notifications...
    </div>

    <!-- Activity feed refreshes on custom event -->
    <div id="activity"
         hx-get="/Dashboard?handler=Activity"
         hx-trigger="load, activityUpdated from:body"
         hx-target="this">
        Loading activity...
    </div>
</div>
```

### Keyboard Shortcuts

```html
<!-- Global keyboard shortcuts -->
<body hx-on:keyup="handleGlobalShortcuts(event)">
    
    <!-- Ctrl+S to save -->
    <form hx-post="/Document?handler=Save"
          hx-trigger="submit, keydown[ctrlKey && key=='s'] from:body"
          hx-target="#save-status"
          hx-on:keydown="if(event.ctrlKey && event.key=='s') event.preventDefault()">
        @Html.AntiForgeryToken()
        <textarea name="content"></textarea>
        <div id="save-status"></div>
    </form>

</body>

<script>
function handleGlobalShortcuts(event) {
    // Escape closes modals
    if (event.key === 'Escape') {
        var modal = document.querySelector('.modal.open');
        if (modal) htmx.trigger(modal, 'close');
    }
    
    // / focuses search
    if (event.key === '/' && event.target.tagName !== 'INPUT') {
        event.preventDefault();
        document.getElementById('search-input').focus();
    }
}
</script>
```

### Cascading Dropdowns

```html
<div class="form-group">
    <label>Country</label>
    <select name="country" 
            hx-get="/Location?handler=States"
            hx-trigger="change"
            hx-target="#states-container">
        <option value="">Select country...</option>
        <option value="US">United States</option>
        <option value="CA">Canada</option>
    </select>
</div>

<div id="states-container">
    <div class="form-group">
        <label>State/Province</label>
        <select name="state" disabled>
            <option>Select country first...</option>
        </select>
    </div>
</div>
```

```csharp
public IActionResult OnGetStates(string country)
{
    if (string.IsNullOrEmpty(country))
    {
        return Content(@"
            <div class=""form-group"">
                <label>State/Province</label>
                <select name=""state"" disabled>
                    <option>Select country first...</option>
                </select>
            </div>", "text/html");
    }

    var states = _locationService.GetStates(country);
    return Partial("_StatesDropdown", states);
}
```

## Debugging Triggers and Events

### Enable htmx Logging

```html
<script>
htmx.logAll();
</script>
```

This logs every event htmx processes, showing trigger evaluation and request lifecycle.

### Monitor Specific Events

```html
<script>
document.body.addEventListener('htmx:beforeRequest', function(event) {
    console.log('Request starting:', event.detail.pathInfo.requestPath);
});

document.body.addEventListener('htmx:afterRequest', function(event) {
    console.log('Request complete:', event.detail.xhr.status);
});

document.body.addEventListener('htmx:triggerError', function(event) {
    console.error('Trigger error:', event.detail);
});
</script>
```

### Common Issues

**Trigger not firing**

- Check event name spelling
- Verify the element exists when htmx initializes
- For `from:` triggers, ensure the source element exists

**Request fires too often**

- Add `changed` modifier for input events
- Add `delay:` for debouncing
- Add `throttle:` for rate limiting

**Event filter not working**

- Check JavaScript syntax in brackets
- Verify property names (e.g., `key` not `keyCode`)
- Test the condition in browser console first

**HX-Trigger header not working**

- Header name is case-sensitive
- JSON must be valid for events with data
- Check for events firing on `body` element

## Summary

This chapter covered `hx-trigger` and `hx-on` for precise control over htmx interactions:

- **Default triggers** work for common elements (click, submit, change)
- **Modifiers** refine behavior: `changed`, `delay:`, `throttle:`, `once`, `from:`, `target:`, `consume`, `queue:`
- **Special triggers**: `load`, `revealed`, `intersect`, `every`
- **Event filters** use bracket syntax for conditions: `keyup[key=='Enter']`, `click[ctrlKey]`
- **`hx-on:`** executes JavaScript on DOM events (single colon)
- **`hx-on::`** executes JavaScript on htmx events (double colon)
- **HX-Trigger header** dispatches events from server responses

These tools let you build responsive, event-driven interfaces without custom JavaScript frameworks.

## Preview of Next Chapter

Chapter 9 introduces Hyperscript, a companion language to htmx that adds client-side logic with a clean, readable syntax. You will learn how to handle complex interactions, manage state, and add behaviors that would otherwise require JavaScript, all using simple English-like commands embedded in your HTML.
