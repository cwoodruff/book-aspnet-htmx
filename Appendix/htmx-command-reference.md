---
order: 4
icon: stack
label: Appendix A - htmx Command Reference
meta:
title: "htmx Command Reference"
---
# Appendix A: htmx Command Reference

This appendix provides a complete reference for htmx attributes, headers, events, and JavaScript API. Use it as a quick lookup while building htmx applications with ASP.NET Core.

---

## A.1 Core Attributes

These attributes initiate HTTP requests when triggered.

### hx-get

Issues an HTTP GET request to the specified URL.

```html
<button hx-get="/Artists?handler=List" 
        hx-target="#artist-list">
    Load Artists
</button>
```

```csharp
// Razor Pages handler
public IActionResult OnGetList()
{
    var artists = _artistService.GetAll();
    return Partial("_ArtistList", artists);
}
```

### hx-post

Issues an HTTP POST request. Form values are sent in the request body.

```html
<form hx-post="/Artists?handler=Create" 
      hx-target="#artist-list" 
      hx-swap="afterbegin">
    @Html.AntiForgeryToken()
    <input type="text" name="name" />
    <button type="submit">Create</button>
</form>
```

```csharp
public IActionResult OnPostCreate(string name)
{
    var artist = _artistService.Create(name);
    return Partial("_ArtistRow", artist);
}
```

### hx-put

Issues an HTTP PUT request for full resource updates.

```html
<form hx-put="/Artists/5?handler=Update" 
      hx-target="#artist-row-5">
    @Html.AntiForgeryToken()
    <input type="text" name="name" value="@Model.Name" />
    <button type="submit">Update</button>
</form>
```

### hx-patch

Issues an HTTP PATCH request for partial resource updates.

```html
<button hx-patch="/Artists/5?handler=ToggleActive" 
        hx-target="#artist-row-5">
    Toggle Status
</button>
```

### hx-delete

Issues an HTTP DELETE request.

```html
<button hx-delete="/Artists/5" 
        hx-target="#artist-row-5" 
        hx-swap="delete"
        hx-confirm="Delete this artist?">
    Delete
</button>
```

```csharp
public IActionResult OnDelete(int id)
{
    _artistService.Delete(id);
    return Content(""); // Empty response for delete swap
}
```

### URL Patterns

| Pattern | Example | Description |
|---------|---------|-------------|
| Relative | `/Artists` | Relative to current origin |
| With handler | `/Artists?handler=List` | Razor Pages handler |
| With parameters | `/Artists?handler=Edit&id=5` | Handler with parameters |
| Absolute | `https://api.example.com/data` | Full URL (CORS required) |

---

## A.2 Targeting and Swapping

### hx-target

Specifies the element to update with the response. Accepts CSS selectors.

| Value | Description | Example |
|-------|-------------|---------|
| CSS selector | Target by selector | `hx-target="#results"` |
| `this` | The triggering element | `hx-target="this"` |
| `closest <selector>` | Closest ancestor | `hx-target="closest tr"` |
| `find <selector>` | First descendant | `hx-target="find .content"` |
| `next` | Next sibling | `hx-target="next"` |
| `next <selector>` | Next matching sibling | `hx-target="next .item"` |
| `previous` | Previous sibling | `hx-target="previous"` |
| `previous <selector>` | Previous matching sibling | `hx-target="previous .item"` |

```html
<!-- Target by ID -->
<button hx-get="/data" hx-target="#content">Load</button>

<!-- Target closest table row -->
<button hx-get="/edit" hx-target="closest tr">Edit</button>

<!-- Target the button itself -->
<button hx-get="/status" hx-target="this">Refresh</button>
```

### hx-swap

Specifies how the response content is swapped into the target.

| Value | Description |
|-------|-------------|
| `innerHTML` | Replace inner HTML (default) |
| `outerHTML` | Replace entire element |
| `beforebegin` | Insert before target |
| `afterbegin` | Insert at start of target |
| `beforeend` | Insert at end of target |
| `afterend` | Insert after target |
| `delete` | Delete target element |
| `none` | No swap (use for side effects) |

```html
<!-- Replace content inside #list -->
<button hx-get="/items" hx-target="#list" hx-swap="innerHTML">
    Load
</button>

<!-- Replace entire row -->
<button hx-get="/edit/5" hx-target="#row-5" hx-swap="outerHTML">
    Edit
</button>

<!-- Append to list -->
<button hx-get="/more" hx-target="#list" hx-swap="beforeend">
    Load More
</button>

<!-- Delete row after successful delete -->
<button hx-delete="/items/5" hx-target="closest tr" hx-swap="delete">
    Delete
</button>
```

### Swap Modifiers

Add modifiers to `hx-swap` to control timing and scrolling.

| Modifier | Description | Example |
|----------|-------------|---------|
| `swap:<time>` | Delay before swap | `hx-swap="innerHTML swap:500ms"` |
| `settle:<time>` | Delay before settle | `hx-swap="innerHTML settle:100ms"` |
| `scroll:<target>` | Scroll after swap | `hx-swap="innerHTML scroll:top"` |
| `show:<target>` | Show element | `hx-swap="innerHTML show:top"` |
| `focus-scroll:<bool>` | Scroll focused element | `hx-swap="innerHTML focus-scroll:true"` |
| `transition:<bool>` | Enable view transitions | `hx-swap="innerHTML transition:true"` |

```html
<!-- Swap with animation delay -->
<div hx-get="/content" hx-swap="innerHTML swap:300ms settle:100ms">

<!-- Scroll to top after swap -->
<div hx-get="/page/2" hx-swap="innerHTML scroll:top">

<!-- Show bottom of target -->
<div hx-get="/messages" hx-swap="beforeend show:bottom">
```

### hx-swap-oob

Enables out-of-band swaps. The response can update multiple elements.

```html
<!-- In response HTML -->
<div id="main-content">Main content here</div>

<div id="stats" hx-swap-oob="true">
    Updated stats: 42 items
</div>

<div id="notification" hx-swap-oob="innerHTML">
    Operation successful!
</div>
```

OOB swap strategies:

| Value | Description |
|-------|-------------|
| `true` | Use outerHTML swap |
| `innerHTML` | Replace inner content |
| `outerHTML` | Replace entire element |
| `beforebegin` | Insert before |
| `afterbegin` | Insert at start |
| `beforeend` | Insert at end |
| `afterend` | Insert after |
| `delete` | Remove element |
| `none` | No action |

### hx-select

Selects a portion of the response to swap.

```html
<!-- Only swap the #results portion of the response -->
<button hx-get="/search" 
        hx-target="#output" 
        hx-select="#results">
    Search
</button>
```

### hx-select-oob

Selects elements from response for out-of-band swaps.

```html
<button hx-get="/data" 
        hx-target="#main"
        hx-select-oob="#sidebar,#footer">
    Load
</button>
```

---

## A.3 Triggering Requests

### hx-trigger

Specifies the event that triggers the request.

**Standard DOM Events:**

```html
<!-- Click (default for buttons) -->
<button hx-get="/data" hx-trigger="click">Click Me</button>

<!-- Change (default for inputs/selects) -->
<select hx-get="/filter" hx-trigger="change">...</select>

<!-- Submit (default for forms) -->
<form hx-post="/save" hx-trigger="submit">...</form>

<!-- Keyboard events -->
<input hx-get="/search" hx-trigger="keyup">

<!-- Mouse events -->
<div hx-get="/preview" hx-trigger="mouseenter">Hover me</div>
```

**Special Triggers:**

| Trigger | Description | Example |
|---------|-------------|---------|
| `load` | On element load | `hx-trigger="load"` |
| `revealed` | When scrolled into view | `hx-trigger="revealed"` |
| `intersect` | Intersection observer | `hx-trigger="intersect"` |
| `every <time>` | Polling interval | `hx-trigger="every 5s"` |

```html
<!-- Load on page render -->
<div hx-get="/stats" hx-trigger="load">Loading...</div>

<!-- Infinite scroll -->
<div hx-get="/more" hx-trigger="revealed" hx-swap="afterend">
    Loading more...
</div>

<!-- Poll every 30 seconds -->
<div hx-get="/notifications" hx-trigger="every 30s">
    Checking...
</div>

<!-- Intersection with threshold -->
<div hx-get="/lazy" hx-trigger="intersect threshold:0.5">
    Lazy content
</div>
```

### Trigger Modifiers

| Modifier | Description | Example |
|----------|-------------|---------|
| `once` | Trigger only once | `hx-trigger="click once"` |
| `changed` | Only if value changed | `hx-trigger="keyup changed"` |
| `delay:<time>` | Debounce | `hx-trigger="keyup delay:300ms"` |
| `throttle:<time>` | Throttle | `hx-trigger="scroll throttle:500ms"` |
| `from:<selector>` | Listen on another element | `hx-trigger="click from:body"` |
| `target:<selector>` | Filter by event target | `hx-trigger="click target:.btn"` |
| `consume` | Stop event propagation | `hx-trigger="click consume"` |
| `queue:<strategy>` | Queue behavior | `hx-trigger="click queue:last"` |

```html
<!-- Debounced search (wait 300ms after typing stops) -->
<input type="text" 
       hx-get="/search" 
       hx-trigger="keyup changed delay:300ms"
       hx-target="#results">

<!-- Fire only once -->
<button hx-get="/init" hx-trigger="click once">Initialize</button>

<!-- Listen to events from document body -->
<div hx-get="/refresh" hx-trigger="refreshData from:body">
    Data content
</div>

<!-- Queue only the last request -->
<input hx-get="/autocomplete" 
       hx-trigger="keyup delay:100ms queue:last">
```

**Queue Strategies:**

| Strategy | Description |
|----------|-------------|
| `first` | Queue first, ignore rest |
| `last` | Queue last, ignore previous |
| `all` | Queue all requests |
| `none` | Drop if request in flight |

### Multiple Triggers

Separate multiple triggers with commas:

```html
<input hx-get="/validate" 
       hx-trigger="change, keyup delay:500ms changed">
```

### hx-confirm

Shows a confirmation dialog before the request.

```html
<button hx-delete="/items/5" 
        hx-confirm="Are you sure you want to delete this item?">
    Delete
</button>
```

### hx-prompt

Shows a prompt dialog and sends the input value.

```html
<button hx-post="/rename" 
        hx-prompt="Enter new name:"
        hx-vals='{"id": 5}'>
    Rename
</button>
```

The prompt value is sent in the `HX-Prompt` header.

---

## A.4 Request Modifiers

### hx-include

Includes additional element values in the request.

| Value | Description |
|-------|-------------|
| CSS selector | Include matching elements |
| `this` | Include triggering element |
| `closest <selector>` | Include closest ancestor |
| `find <selector>` | Include descendants |
| `next <selector>` | Include next sibling |
| `previous <selector>` | Include previous sibling |

```html
<!-- Include search input value -->
<button hx-get="/search" 
        hx-include="[name='query']">
    Search
</button>

<!-- Include all form inputs -->
<button hx-get="/filter" 
        hx-include="closest form">
    Apply Filters
</button>

<!-- Include multiple elements -->
<button hx-get="/report" 
        hx-include="#startDate, #endDate, #category">
    Generate
</button>
```

### hx-params

Filters which parameters are submitted.

| Value | Description |
|-------|-------------|
| `*` | All parameters (default) |
| `none` | No parameters |
| `not <list>` | Exclude listed parameters |
| `<list>` | Only listed parameters |

```html
<!-- Send only specific parameters -->
<form hx-post="/save" hx-params="name, email">
    <input name="name" />
    <input name="email" />
    <input name="internal_id" /> <!-- Not sent -->
</form>

<!-- Exclude parameters -->
<form hx-post="/save" hx-params="not csrf_token">
    ...
</form>

<!-- Send no parameters -->
<button hx-post="/ping" hx-params="none">Ping</button>
```

### hx-vals

Adds additional values to the request as JSON.

```html
<!-- Static values -->
<button hx-post="/action" 
        hx-vals='{"type": "archive", "notify": true}'>
    Archive
</button>

<!-- Dynamic values with JavaScript -->
<button hx-post="/save" 
        hx-vals='js:{timestamp: Date.now()}'>
    Save
</button>
```

### hx-headers

Adds custom HTTP headers to the request.

```html
<button hx-get="/api/data" 
        hx-headers='{"X-Custom-Header": "value"}'>
    Load
</button>
```

For anti-forgery tokens in ASP.NET Core:

```javascript
// In site.js - add to all requests
document.body.addEventListener('htmx:configRequest', function(event) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) {
        event.detail.headers['RequestVerificationToken'] = token;
    }
});
```

### hx-sync

Synchronizes requests to prevent race conditions.

| Strategy | Description |
|----------|-------------|
| `drop` | Drop new request if one in flight |
| `abort` | Abort current request for new one |
| `replace` | Abort current, queue new |
| `queue` | Queue all requests |
| `queue first` | Queue first, drop rest |
| `queue last` | Queue last, drop previous |
| `queue all` | Queue all |

```html
<!-- Only allow one search at a time -->
<input hx-get="/search" 
       hx-sync="closest form:abort">

<!-- Queue form submissions -->
<form hx-post="/save" hx-sync="this:queue">
    ...
</form>

<!-- Drop duplicate clicks -->
<button hx-post="/action" hx-sync="this:drop">
    Submit
</button>
```

### hx-encoding

Sets the encoding type for the request.

```html
<!-- For file uploads -->
<form hx-post="/upload" hx-encoding="multipart/form-data">
    <input type="file" name="file" />
    <button type="submit">Upload</button>
</form>
```

---

## A.5 History and Navigation

### hx-push-url

Pushes a URL to the browser history.

```html
<!-- Push the request URL -->
<a hx-get="/page/2" hx-push-url="true">Page 2</a>

<!-- Push a custom URL -->
<button hx-get="/search?q=test" hx-push-url="/search/test">
    Search
</button>

<!-- Disable URL push -->
<a hx-get="/modal" hx-push-url="false">Open Modal</a>
```

### hx-replace-url

Replaces the current URL in history (no new history entry).

```html
<button hx-get="/tab/details" hx-replace-url="true">
    Details Tab
</button>
```

### hx-history

Controls whether the element's innerHTML is saved to history cache.

```html
<!-- Don't save this element to history -->
<div hx-history="false">
    Sensitive content not cached
</div>
```

### hx-history-elt

Specifies which element's content to snapshot for history.

```html
<body hx-history-elt>
    <!-- Only body content is cached, not full page -->
</body>
```

---

## A.6 Indicators and Feedback

### hx-indicator

Specifies an element to show during the request.

```html
<button hx-get="/data" hx-indicator="#spinner">
    Load Data
</button>
<span id="spinner" class="htmx-indicator">Loading...</span>
```

```css
/* htmx automatically adds this, but you can customize */
.htmx-indicator {
    display: none;
}
.htmx-request .htmx-indicator,
.htmx-request.htmx-indicator {
    display: inline;
}
```

Multiple indicators:

```html
<button hx-get="/data" hx-indicator="#spinner, #overlay">
    Load
</button>
```

### hx-disabled-elt

Disables elements during the request.

```html
<!-- Disable the button itself -->
<button hx-post="/save" hx-disabled-elt="this">
    Save
</button>

<!-- Disable multiple elements -->
<form hx-post="/submit" hx-disabled-elt="find input, find button">
    <input name="data" />
    <button type="submit">Submit</button>
</form>

<!-- Disable closest form -->
<button hx-post="/save" hx-disabled-elt="closest form">
    Save
</button>
```

### CSS Classes Applied by htmx

| Class | Applied When |
|-------|--------------|
| `.htmx-request` | Request in progress |
| `.htmx-settling` | During settle phase |
| `.htmx-swapping` | During swap phase |
| `.htmx-added` | Element just added to DOM |
| `.htmx-indicator` | Indicator elements |

```css
/* Show loading state */
.htmx-request {
    opacity: 0.5;
    pointer-events: none;
}

/* Fade in new content */
.htmx-added {
    animation: fadeIn 0.3s ease-out;
}

/* Indicator styling */
.htmx-indicator {
    display: none;
}
.htmx-request .htmx-indicator {
    display: inline-flex;
}
```

---

## A.7 Inheritance and Scope

### hx-boost

Converts standard anchors and forms to use AJAX.

```html
<!-- Boost all links and forms in this container -->
<div hx-boost="true">
    <a href="/page/2">Page 2</a> <!-- Now uses AJAX -->
    <form action="/search" method="get">...</form> <!-- Now uses AJAX -->
</div>

<!-- Disable boost for specific elements -->
<div hx-boost="true">
    <a href="/page/2">AJAX Link</a>
    <a href="/download.pdf" hx-boost="false">Normal Link</a>
</div>
```

### hx-inherit

Controls which attributes are inherited from ancestors.

```html
<!-- Only inherit hx-target -->
<div hx-target="#output" hx-swap="innerHTML">
    <button hx-get="/a" hx-inherit="hx-target">A</button>
    <button hx-get="/b" hx-inherit="hx-target">B</button>
</div>
```

### hx-disinherit

Prevents attribute inheritance.

```html
<div hx-target="#main" hx-confirm="Are you sure?">
    <!-- This button won't inherit hx-confirm -->
    <button hx-get="/safe" hx-disinherit="hx-confirm">
        Safe Action
    </button>
    
    <!-- This button inherits everything -->
    <button hx-delete="/dangerous">
        Dangerous Action
    </button>
</div>

<!-- Disinherit all attributes -->
<button hx-get="/standalone" hx-disinherit="*">
    Standalone
</button>
```

### hx-preserve

Preserves an element across swaps (useful for video, audio, iframes).

```html
<div id="content">
    <video id="player" hx-preserve="true" src="video.mp4"></video>
    <div id="info">Info that gets swapped</div>
</div>
```

---

## A.8 Extensions

### hx-ext

Enables htmx extensions for an element and its descendants.

```html
<!-- Enable single extension -->
<body hx-ext="json-enc">
    ...
</body>

<!-- Enable multiple extensions -->
<body hx-ext="json-enc, loading-states">
    ...
</body>

<!-- Disable inherited extension -->
<div hx-ext="ignore:json-enc">
    ...
</div>
```

### Built-in Extensions

| Extension | Purpose |
|-----------|---------|
| `json-enc` | Send request body as JSON |
| `morphdom-swap` | Use morphdom for DOM diffing |
| `alpine-morph` | Use Alpine.js morph |
| `client-side-templates` | Mustache/Handlebars templates |
| `path-deps` | Refresh on path dependencies |
| `class-tools` | Advanced class manipulation |
| `loading-states` | Enhanced loading indicators |
| `preload` | Preload on hover/focus |
| `remove-me` | Auto-remove elements |
| `response-targets` | Multiple response targets |
| `head-support` | Process head elements |
| `multi-swap` | Multiple swap targets |
| `debug` | Debug logging |
| `event-header` | Include triggering event |

### hx-ws (WebSocket)

Establishes a WebSocket connection.

```html
<div hx-ws="connect:/chat">
    <div id="messages"></div>
    <form hx-ws="send">
        <input name="message" />
        <button>Send</button>
    </form>
</div>
```

| Value | Description |
|-------|-------------|
| `connect:<url>` | Connect to WebSocket URL |
| `send` | Send form data over WebSocket |

### hx-sse (Server-Sent Events)

Connects to a Server-Sent Events endpoint.

```html
<div hx-sse="connect:/events">
    <div hx-sse="swap:message">
        <!-- Updated when 'message' event received -->
    </div>
    <div hx-sse="swap:notification">
        <!-- Updated when 'notification' event received -->
    </div>
</div>
```

| Value | Description |
|-------|-------------|
| `connect:<url>` | Connect to SSE endpoint |
| `swap:<event>` | Swap content on event |

---

## A.9 Request Headers

Headers automatically sent by htmx with each request:

| Header | Value | Description |
|--------|-------|-------------|
| `HX-Request` | `true` | Identifies request as htmx |
| `HX-Target` | Element ID | Target element ID |
| `HX-Trigger` | Element ID | Triggering element ID |
| `HX-Trigger-Name` | Name attribute | Name of triggering element |
| `HX-Current-URL` | URL | Current page URL |
| `HX-Prompt` | String | User input from hx-prompt |
| `HX-Boosted` | `true` | If request is boosted |
| `HX-History-Restore-Request` | `true` | If restoring from history |

### Detecting htmx Requests in ASP.NET Core

```csharp
// In a Razor Page or Controller
public IActionResult OnGet()
{
    if (Request.Headers.ContainsKey("HX-Request"))
    {
        // htmx request - return partial
        return Partial("_Content", model);
    }
    
    // Regular request - return full page
    return Page();
}

// Extension method for cleaner code
public static class HttpRequestExtensions
{
    public static bool IsHtmx(this HttpRequest request)
    {
        return request.Headers.ContainsKey("HX-Request");
    }
    
    public static string? HxTarget(this HttpRequest request)
    {
        return request.Headers["HX-Target"].FirstOrDefault();
    }
    
    public static string? HxTrigger(this HttpRequest request)
    {
        return request.Headers["HX-Trigger"].FirstOrDefault();
    }
}

// Usage
if (Request.IsHtmx())
{
    var target = Request.HxTarget();
}
```

---

## A.10 Response Headers

Headers the server can send to control htmx behavior:

### HX-Location

Triggers client-side redirect without full page reload.

```csharp
// Simple redirect
Response.Headers.Append("HX-Location", "/new-page");

// With options
Response.Headers.Append("HX-Location", 
    JsonSerializer.Serialize(new {
        path = "/new-page",
        target = "#content",
        swap = "innerHTML"
    }));
```

### HX-Push-Url

Pushes URL to browser history.

```csharp
Response.Headers.Append("HX-Push-Url", "/artists/5");

// Prevent URL push
Response.Headers.Append("HX-Push-Url", "false");
```

### HX-Replace-Url

Replaces current URL in history.

```csharp
Response.Headers.Append("HX-Replace-Url", "/current-state");
```

### HX-Redirect

Triggers full page redirect.

```csharp
Response.Headers.Append("HX-Redirect", "/login");
```

### HX-Refresh

Triggers full page refresh.

```csharp
Response.Headers.Append("HX-Refresh", "true");
```

### HX-Retarget

Changes the target element.

```csharp
// Retarget to error container on validation failure
if (!ModelState.IsValid)
{
    Response.Headers.Append("HX-Retarget", "#error-container");
    Response.Headers.Append("HX-Reswap", "innerHTML");
    return Partial("_ValidationErrors", ModelState);
}
```

### HX-Reswap

Changes the swap method.

```csharp
Response.Headers.Append("HX-Reswap", "outerHTML");
```

### HX-Reselect

Changes which part of response to select.

```csharp
Response.Headers.Append("HX-Reselect", "#main-content");
```

### HX-Trigger

Triggers client-side events after response is received.

```csharp
// Simple event
Response.Headers.Append("HX-Trigger", "showMessage");

// Event with data
Response.Headers.Append("HX-Trigger", 
    JsonSerializer.Serialize(new {
        showToast = new { message = "Saved!", type = "success" }
    }));

// Multiple events
Response.Headers.Append("HX-Trigger",
    JsonSerializer.Serialize(new {
        showToast = new { message = "Created!" },
        refreshList = true,
        closeModal = true
    }));
```

### HX-Trigger-After-Settle

Triggers events after the settle phase.

```csharp
Response.Headers.Append("HX-Trigger-After-Settle", "initComponents");
```

### HX-Trigger-After-Swap

Triggers events after the swap phase.

```csharp
Response.Headers.Append("HX-Trigger-After-Swap", "highlightNew");
```

### Response Header Helper Class

```csharp
public static class HtmxResponseExtensions
{
    public static void HxTrigger(this HttpResponse response, string eventName)
    {
        response.Headers.Append("HX-Trigger", eventName);
    }
    
    public static void HxTrigger(this HttpResponse response, object events)
    {
        response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(events));
    }
    
    public static void HxRedirect(this HttpResponse response, string url)
    {
        response.Headers.Append("HX-Redirect", url);
    }
    
    public static void HxRefresh(this HttpResponse response)
    {
        response.Headers.Append("HX-Refresh", "true");
    }
    
    public static void HxRetarget(this HttpResponse response, string selector)
    {
        response.Headers.Append("HX-Retarget", selector);
    }
    
    public static void HxReswap(this HttpResponse response, string swapStyle)
    {
        response.Headers.Append("HX-Reswap", swapStyle);
    }
    
    public static void HxPushUrl(this HttpResponse response, string url)
    {
        response.Headers.Append("HX-Push-Url", url);
    }
    
    public static void HxReplaceUrl(this HttpResponse response, string url)
    {
        response.Headers.Append("HX-Replace-Url", url);
    }
}

// Usage
Response.HxTrigger(new { 
    showToast = new { message = "Success!", type = "success" },
    closeModal = true 
});
```

---

## A.11 Events

### Request Lifecycle Events

| Event | When Fired | Detail Properties |
|-------|------------|-------------------|
| `htmx:configRequest` | Before request config | `parameters`, `headers`, `target`, `verb` |
| `htmx:beforeRequest` | Before request sent | `xhr`, `target`, `requestConfig` |
| `htmx:beforeSend` | Just before XHR send | `xhr`, `target`, `requestConfig` |
| `htmx:xhr:loadstart` | XHR load started | `xhr` |
| `htmx:xhr:progress` | XHR progress | `xhr`, `loaded`, `total` |
| `htmx:xhr:loadend` | XHR load ended | `xhr` |
| `htmx:afterRequest` | After response received | `xhr`, `target`, `requestConfig`, `successful`, `failed` |
| `htmx:responseError` | HTTP error (4xx, 5xx) | `xhr`, `target`, `requestConfig` |
| `htmx:sendError` | Network error | `xhr`, `target`, `requestConfig` |
| `htmx:timeout` | Request timeout | `xhr`, `target`, `requestConfig` |

### Swap and Settle Events

| Event | When Fired | Detail Properties |
|-------|------------|-------------------|
| `htmx:beforeSwap` | Before content swap | `xhr`, `target`, `requestConfig`, `shouldSwap`, `serverResponse` |
| `htmx:afterSwap` | After content swap | `xhr`, `target`, `requestConfig` |
| `htmx:beforeSettle` | Before settle phase | `xhr`, `target`, `requestConfig` |
| `htmx:afterSettle` | After settle phase | `xhr`, `target`, `requestConfig` |
| `htmx:oobBeforeSwap` | Before OOB swap | `fragment`, `target` |
| `htmx:oobAfterSwap` | After OOB swap | `fragment`, `target` |

### Other Events

| Event | When Fired | Detail Properties |
|-------|------------|-------------------|
| `htmx:load` | Element added to DOM | `elt` |
| `htmx:confirm` | Before confirmation | `question`, `triggeringEvent` |
| `htmx:prompt` | Before prompt | `question`, `triggeringEvent` |
| `htmx:beforeHistoryUpdate` | Before history update | `history`, `navigator` |
| `htmx:pushedIntoHistory` | After history push | `path` |
| `htmx:historyCacheMiss` | History cache miss | `path`, `xhr` |
| `htmx:historyCacheMissLoad` | After cache miss load | `path`, `xhr` |
| `htmx:historyRestore` | History restored | `path` |
| `htmx:validation:validate` | Form validation | `elt` |
| `htmx:validation:failed` | Validation failed | `elt`, `message`, `validity` |

### Event Handling Examples

```javascript
// Add anti-forgery token to all requests
document.body.addEventListener('htmx:configRequest', function(event) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) {
        event.detail.headers['RequestVerificationToken'] = token;
    }
});

// Handle custom events from HX-Trigger header
document.body.addEventListener('showToast', function(event) {
    const { message, type } = event.detail;
    showToast(message, type);
});

document.body.addEventListener('closeModal', function() {
    document.querySelector('.modal')?.remove();
});

// Cancel request conditionally
document.body.addEventListener('htmx:confirm', function(event) {
    if (!confirm(event.detail.question)) {
        event.preventDefault();
    }
});

// Modify swap behavior
document.body.addEventListener('htmx:beforeSwap', function(event) {
    // Handle 404 errors
    if (event.detail.xhr.status === 404) {
        event.detail.shouldSwap = true;
        event.detail.target = document.getElementById('error-container');
    }
});

// Handle errors globally
document.body.addEventListener('htmx:responseError', function(event) {
    console.error('Request failed:', event.detail.xhr.status);
    showToast('An error occurred', 'error');
});

document.body.addEventListener('htmx:sendError', function(event) {
    showToast('Network error. Please check your connection.', 'error');
});

// Initialize components after swap
document.body.addEventListener('htmx:afterSettle', function(event) {
    // Re-initialize any JavaScript components in swapped content
    initializeComponents(event.detail.target);
});

// Log all htmx activity (debugging)
document.body.addEventListener('htmx:afterRequest', function(event) {
    console.log('Request completed:', {
        url: event.detail.requestConfig.path,
        successful: event.detail.successful,
        status: event.detail.xhr.status
    });
});
```

---

## A.12 JavaScript API

### htmx.ajax()

Programmatically issue an htmx request.

```javascript
// Simple GET
htmx.ajax('GET', '/api/data', '#target');

// With options
htmx.ajax('POST', '/api/save', {
    target: '#result',
    swap: 'innerHTML',
    values: { name: 'John', age: 30 },
    headers: { 'X-Custom': 'value' }
});

// Returns a Promise
htmx.ajax('GET', '/data', '#output')
    .then(() => console.log('Request complete'));
```

### DOM Manipulation

```javascript
// Find elements
const el = htmx.find('#myId');
const els = htmx.findAll('.myClass');
const parent = htmx.closest(el, '.container');

// Class manipulation with transitions
htmx.addClass(el, 'active');           // Add class
htmx.removeClass(el, 'active');        // Remove class
htmx.toggleClass(el, 'active');        // Toggle class
htmx.takeClass(el, 'active');          // Take from siblings

// Remove element with transition
htmx.remove(el);

// Swap content manually
htmx.swap('#target', '<p>New content</p>', { swapStyle: 'innerHTML' });
```

### Events

```javascript
// Add event listener
htmx.on('htmx:afterSwap', function(event) {
    console.log('Swapped:', event.detail.target);
});

// Remove event listener
htmx.off('htmx:afterSwap', handler);

// Trigger event
htmx.trigger(el, 'customEvent', { data: 'value' });

// Trigger htmx request on element
htmx.trigger(el, 'click');
```

### Processing

```javascript
// Process htmx attributes on dynamically added content
const newContent = document.createElement('div');
newContent.innerHTML = '<button hx-get="/data">Load</button>';
document.body.appendChild(newContent);
htmx.process(newContent);
```

### Utilities

```javascript
// Parse time strings
htmx.parseInterval('500ms');  // 500
htmx.parseInterval('2s');     // 2000
htmx.parseInterval('1m');     // 60000

// Get htmx version
console.log(htmx.version);    // "1.9.10"

// Set logger for debugging
htmx.logger = function(elt, event, data) {
    console.log(event, elt, data);
};
```

### Extensions

```javascript
// Define custom extension
htmx.defineExtension('my-extension', {
    onEvent: function(name, event) {
        if (name === 'htmx:configRequest') {
            event.detail.headers['X-My-Header'] = 'value';
        }
    },
    transformResponse: function(text, xhr, elt) {
        return text.toUpperCase();
    },
    isInlineSwap: function(swapStyle) {
        return swapStyle === 'my-swap';
    },
    handleSwap: function(swapStyle, target, fragment, settleInfo) {
        // Custom swap logic
        return true; // Return true if handled
    }
});

// Remove extension
htmx.removeExtension('my-extension');
```

---

## A.13 Configuration

### Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `historyEnabled` | `true` | Enable history support |
| `historyCacheSize` | `10` | Number of pages to cache |
| `refreshOnHistoryMiss` | `false` | Full refresh on cache miss |
| `defaultSwapStyle` | `innerHTML` | Default swap method |
| `defaultSwapDelay` | `0` | Default swap delay (ms) |
| `defaultSettleDelay` | `20` | Default settle delay (ms) |
| `includeIndicatorStyles` | `true` | Include default indicator CSS |
| `indicatorClass` | `htmx-indicator` | Class for indicators |
| `requestClass` | `htmx-request` | Class during request |
| `addedClass` | `htmx-added` | Class for new elements |
| `settlingClass` | `htmx-settling` | Class during settle |
| `swappingClass` | `htmx-swapping` | Class during swap |
| `allowEval` | `true` | Allow eval (for hx-on) |
| `allowScriptTags` | `true` | Process script tags |
| `inlineScriptNonce` | `''` | Nonce for inline scripts |
| `useTemplateFragments` | `false` | Use template for parsing |
| `wsReconnectDelay` | `full-jitter` | WebSocket reconnect strategy |
| `wsBinaryType` | `blob` | WebSocket binary type |
| `disableSelector` | `[hx-disable], [data-hx-disable]` | Selector for disabled elements |
| `withCredentials` | `false` | Send credentials cross-origin |
| `timeout` | `0` | Request timeout (ms, 0=none) |
| `scrollBehavior` | `smooth` | Scroll behavior |
| `defaultFocusScroll` | `false` | Scroll to focused element |
| `getCacheBusterParam` | `false` | Add cache buster param |
| `globalViewTransitions` | `false` | Enable view transitions |
| `methodsThatUseUrlParams` | `["get"]` | Methods using URL params |
| `selfRequestsOnly` | `false` | Only allow same-origin |
| `ignoreTitle` | `false` | Ignore title tag updates |
| `scrollIntoViewOnBoost` | `true` | Scroll on boosted navigation |

### Setting Configuration

**Via JavaScript:**

```javascript
htmx.config.historyCacheSize = 20;
htmx.config.defaultSwapDelay = 100;
htmx.config.timeout = 30000;
```

**Via meta tag:**

```html
<meta name="htmx-config" content='{
    "historyCacheSize": 20,
    "defaultSwapDelay": 100,
    "timeout": 30000,
    "selfRequestsOnly": true
}'>
```

### Common Configuration Patterns

```html
<!-- Production security settings -->
<meta name="htmx-config" content='{
    "selfRequestsOnly": true,
    "allowEval": false,
    "timeout": 30000
}'>

<!-- SPA-like history handling -->
<meta name="htmx-config" content='{
    "historyCacheSize": 50,
    "refreshOnHistoryMiss": false,
    "scrollIntoViewOnBoost": true
}'>

<!-- Performance tuning -->
<meta name="htmx-config" content='{
    "defaultSwapDelay": 0,
    "defaultSettleDelay": 10,
    "getCacheBusterParam": true
}'>
```

---

## Quick Reference Card

### Most-Used Attributes

| Attribute | Purpose | Example |
|-----------|---------|---------|
| `hx-get` | GET request | `hx-get="/data"` |
| `hx-post` | POST request | `hx-post="/save"` |
| `hx-target` | Target element | `hx-target="#output"` |
| `hx-swap` | Swap method | `hx-swap="outerHTML"` |
| `hx-trigger` | Trigger event | `hx-trigger="click"` |

### Common Patterns

**Debounced Search:**
```html
<input hx-get="/search" 
       hx-target="#results" 
       hx-trigger="keyup changed delay:300ms">
```

**Inline Edit:**
```html
<button hx-get="/edit/5" 
        hx-target="closest tr" 
        hx-swap="outerHTML">
    Edit
</button>
```

**Modal Form:**
```html
<button hx-get="/form" 
        hx-target="#modal-container">
    Open Form
</button>
```

**Delete with Confirmation:**
```html
<button hx-delete="/items/5" 
        hx-target="closest tr" 
        hx-swap="delete"
        hx-confirm="Delete this item?">
    Delete
</button>
```

### Key Response Headers

| Header | Purpose | Example |
|--------|---------|---------|
| `HX-Trigger` | Trigger events | `{"showToast": {"message": "Saved"}}` |
| `HX-Retarget` | Change target | `#error-container` |
| `HX-Reswap` | Change swap | `innerHTML` |
| `HX-Redirect` | Full redirect | `/login` |

### Essential CSS

```css
.htmx-indicator { display: none; }
.htmx-request .htmx-indicator { display: inline; }
.htmx-request { opacity: 0.5; }
```

### Key Events

| Event | Use Case |
|-------|----------|
| `htmx:configRequest` | Add headers |
| `htmx:afterSwap` | Post-swap init |
| `htmx:responseError` | Error handling |
| Custom events | HX-Trigger response |

---

*For detailed examples and explanations, refer to the relevant chapters in the main text.*
