---
order: 10
icon: stack
label: Chap 21 - Debugging htmx Applications
meta:
title: "Debugging htmx Applications"
---
# Debugging htmx Applications

When an htmx application works, it feels like magic. Partial updates flow smoothly, the page responds instantly, and users never see a loading screen. When something breaks, that same magic becomes a mystery. Where did the request go? Why didn't the content update? What happened to my data?

Debugging htmx applications requires different techniques than traditional web development. This chapter provides the tools and methods to diagnose and fix htmx issues quickly and systematically.

## 21.1 Introduction

### Why Debugging htmx Differs from Traditional Web Apps

Traditional web applications follow a predictable pattern: user clicks a link, browser sends a request, server returns a complete page, browser renders it. When something goes wrong, you see an error page or a blank screen. The failure is visible.

htmx operates differently. Requests happen in the background. Responses replace portions of the page rather than the whole thing. State exists simultaneously on the server (in session, database, or handler properties) and in the browser's DOM. A failure might mean nothing visible happens at all, which is harder to diagnose than an obvious error.

Several characteristics make htmx debugging unique:

**Invisible Updates**: When you click a button and nothing happens, was the request sent? Did the server respond? Did the response reach the right element? Traditional page loads answer these questions automatically. With htmx, you need to investigate each step.

**Partial Responses**: The server returns HTML fragments, not complete pages. If the fragment is malformed, targets the wrong element, or includes unexpected content, the visible result may be confusing or absent.

**Dual State**: Your application state lives in two places. The server knows what data exists. The browser's DOM reflects what the user sees. These can fall out of sync if updates fail partway through, if OOB swaps miss their targets, or if the user navigates with the back button.

**Event Timing**: htmx operations are asynchronous. A fast typist might trigger multiple search requests. A slow network might deliver responses out of order. Debouncing and request cancellation help, but timing issues can still cause unexpected behavior.

**Attribute-Driven Behavior**: htmx behavior comes from HTML attributes, not JavaScript code. A typo in an attribute name silently fails. An incorrect selector in `hx-target` sends content to the wrong place without throwing an error.

### Common Categories of htmx Issues

Most htmx problems fall into a few categories:

**Configuration Errors**: Typos in attribute names (`hx-triger` instead of `hx-trigger`), incorrect handler names in URLs, missing htmx.js include, wrong attribute values.

**Server Response Problems**: Handler returns full page instead of partial, wrong content type, partial view not found, server error (500), authentication redirect (302).

**Targeting Failures**: Target element doesn't exist, selector matches wrong element, element ID changed after previous swap, OOB target missing.

**Event and Timing Issues**: Trigger never fires, events fire too often, request cancellation causes unexpected behavior, debounce timing wrong.

**Anti-Forgery Token Problems**: Token not included in request, token expired, token mismatch between form and handler.

**History and URL State**: Back button shows stale content, URL doesn't reflect current state, history cache serves outdated responses.

### What This Chapter Covers

This chapter provides systematic approaches to debugging each category of issue. You'll learn to use browser developer tools effectively for htmx requests, use htmx's built-in debugging features, implement server-side logging for htmx requests in ASP.NET Core, diagnose common problems with proven solutions, and build a systematic debugging workflow.

The examples use the Chinook Dashboard from Chapter 21. If you haven't built that project, the techniques still apply to any htmx application.

---

## 21.2 Browser Developer Tools for htmx

Browser developer tools are your primary debugging interface. Chrome, Firefox, and Edge all provide similar capabilities. This section focuses on Chrome DevTools, but the concepts transfer to other browsers.

### 21.2.1 Network Tab Essentials

The Network tab shows every request the browser makes. For htmx debugging, you need to filter and interpret these requests effectively.

#### Opening the Network Tab

Press F12 to open DevTools, then click the Network tab. Check "Preserve log" to keep requests visible across navigations. For htmx work, filter to "Fetch/XHR" to hide CSS, images, and other resource requests.

#### Identifying htmx Requests

htmx adds an `HX-Request: true` header to every request it makes. This header distinguishes htmx requests from regular browser navigation or other JavaScript fetch calls.

In the Network tab, click on any request to see its details. Under the Headers section, look for Request Headers. htmx requests include:

```
HX-Request: true
HX-Current-URL: https://localhost:5001/Artists
HX-Target: artist-list
HX-Trigger: search-input
```

These headers tell you:
- `HX-Request`: Confirms this is an htmx request
- `HX-Current-URL`: The page URL when the request was made
- `HX-Target`: The ID of the element that will receive the response (without the # prefix)
- `HX-Trigger`: The ID of the element that triggered the request

If you don't see the `HX-Request` header, the request wasn't made by htmx. Check that htmx.js is loaded and the element has proper htmx attributes.

#### Reading Response Headers

The server can send instructions back to htmx through response headers:

```
HX-Redirect: /Login
HX-Refresh: true
HX-Trigger: showToast
HX-Trigger-After-Swap: refreshStats
HX-Push-Url: /Artists?search=acdc
```

These headers control behavior after the response arrives:
- `HX-Redirect`: Forces a full page redirect
- `HX-Refresh`: Triggers a full page refresh
- `HX-Trigger`: Fires a custom event on the target element
- `HX-Trigger-After-Swap`: Fires after the swap completes
- `HX-Push-Url`: Updates the browser URL

Check response headers when htmx behaves unexpectedly after receiving a response. An `HX-Redirect` from an authentication handler, for example, might explain why your partial update instead triggers a full page load.

#### Inspecting Response Content

Click the Response or Preview tab to see what the server returned. For htmx partials, you should see an HTML fragment, not a complete page with `<!DOCTYPE html>` and `<html>` tags.

If you see a full HTML page, your handler is returning `Page()` instead of `Partial()`, or your partial view is somehow pulling in the layout. This is a common issue covered in detail in Section 21.4.2.

Check the response status code as well:
- **200**: Success, response should be swapped
- **204**: Success, no content (htmx does nothing)
- **400**: Bad request (often anti-forgery token issues)
- **404**: Handler or page not found
- **500**: Server error

#### Demo: Debugging a 404 Handler Not Found

Let's walk through a real debugging scenario. You have an Edit button on an artist row:

```html
<button hx-get="/Artists?handler=Editt&id=1"
        hx-target="#artist-row-1"
        hx-swap="outerHTML">
    Edit
</button>
```

You click the button. Nothing happens. Here's how to diagnose it:

**Step 1: Open Network Tab**

Press F12, select Network, filter to Fetch/XHR.

**Step 2: Click the Button Again**

Watch for a new request to appear.

**Step 3: Examine the Request**

You see a request to `/Artists?handler=Editt&id=1`. The status code is 404.

**Step 4: Identify the Problem**

The handler name is `Editt` with two t's. The actual handler in your PageModel is `OnGetEditAsync`, which maps to `handler=Edit`.

**Step 5: Fix and Verify**

Change the button:

```html
<button hx-get="/Artists?handler=Edit&id=1"
        hx-target="#artist-row-1"
        hx-swap="outerHTML">
    Edit
</button>
```

Refresh and test. The request now returns 200 and the edit form appears.

This pattern applies to any "nothing happens" scenario: open Network, trigger the action, check the status code, examine the URL and response.

### 21.2.2 Console Tab for Errors and Events

The Console tab displays JavaScript errors and log output. htmx and Hyperscript both report errors here.

#### JavaScript Console Errors

When htmx encounters a problem it can detect, it logs to the console. Common errors include:

```
htmx: could not find target with selector "#nonexistent"
```

This means `hx-target` specified an element that doesn't exist in the DOM.

```
htmx: response error : 404 from /Artists?handler=Wrong
```

The server returned an error status code.

Hyperscript also reports parsing errors:

```
_hyperscript: syntax error at line 1: unexpected token 'documnet'
```

This indicates a typo in your Hyperscript code.

Always check the Console tab when something doesn't work. Many issues produce explicit error messages.

#### Using htmx.logAll() for Verbose Logging

htmx can log every event it processes. This verbose output helps understand the sequence of operations:

```javascript
htmx.logAll();
```

Run this in the Console tab, then trigger an htmx action. You'll see output like:

```
htmx:configRequest {verb: 'get', path: '/Artists?handler=List', elt: button, ...}
htmx:beforeRequest {verb: 'get', path: '/Artists?handler=List', ...}
htmx:afterRequest {verb: 'get', path: '/Artists?handler=List', successful: true, ...}
htmx:beforeSwap {target: div#artist-list, serverResponse: '<table>...', ...}
htmx:afterSwap {target: div#artist-list, ...}
htmx:afterSettle {target: div#artist-list, ...}
```

This shows the complete lifecycle: configuring the request, sending it, receiving the response, swapping content, and settling (running scripts in the new content).

If something fails, the chain breaks at a specific point. No `htmx:beforeRequest`? The trigger didn't fire. No `htmx:afterRequest`? The request failed. No `htmx:afterSwap`? The swap failed, possibly due to a missing target.

#### Custom Console Logging

For ongoing debugging, add event listeners that log specific information:

```javascript
// Log all htmx requests with their details
document.body.addEventListener('htmx:configRequest', function(event) {
    console.log('htmx request:', {
        method: event.detail.verb.toUpperCase(),
        path: event.detail.path,
        target: event.detail.target?.id || '(no target)',
        trigger: event.detail.triggeringEvent?.target?.id || '(unknown trigger)'
    });
});

// Log successful swaps
document.body.addEventListener('htmx:afterSwap', function(event) {
    console.log('htmx swapped:', {
        target: event.detail.target?.id || event.detail.target,
        path: event.detail.pathInfo?.requestPath,
        responseSize: event.detail.xhr?.responseText?.length || 0
    });
});

// Log errors prominently
document.body.addEventListener('htmx:responseError', function(event) {
    console.error('htmx ERROR:', {
        status: event.detail.xhr?.status,
        statusText: event.detail.xhr?.statusText,
        path: event.detail.pathInfo?.requestPath
    });
});
```

Add these to your `site.js` file during development. They provide a running commentary of htmx activity without the noise of `htmx.logAll()`.

### 21.2.3 Elements Tab for DOM Inspection

The Elements tab shows the current state of the DOM. Use it to verify htmx attributes and watch for changes.

#### Inspecting htmx Attributes

Right-click any element and select "Inspect" to jump to it in the Elements tab. Expand the element to see its attributes:

```html
<button class="btn btn-primary"
        hx-get="/Artists?handler=Edit&amp;id=1"
        hx-target="#artist-row-1"
        hx-swap="outerHTML"
        hx-indicator="#loading-spinner">
    Edit
</button>
```

Verify each attribute:
- Is `hx-get` pointing to the correct URL?
- Does the `hx-target` selector match an existing element?
- Is the `hx-swap` value what you expect?

You can edit attributes directly in the Elements tab to test fixes before changing your code. Double-click an attribute value to modify it, then trigger the action again.

#### Using "Break on Subtree Modifications"

When content changes unexpectedly, you can set a breakpoint that triggers whenever an element's children change:

1. Right-click the element in the Elements tab
2. Select "Break on" → "subtree modifications"
3. Trigger the htmx action

The debugger pauses when the DOM changes, showing you the call stack that caused the modification. This helps identify which swap or JavaScript code is modifying the element.

#### Testing CSS Selectors in Console

If an `hx-target` selector isn't working, test it in the Console:

```javascript
// Test if the selector finds anything
document.querySelector('#artist-row-1')

// Returns the element or null if not found

// Test more complex selectors
document.querySelector('closest tr')  // This won't work - closest is htmx syntax

// For closest, you need:
document.querySelector('button').closest('tr')  // Find button's parent row
```

Note that htmx supports special selectors like `closest`, `find`, `next`, and `previous` that aren't standard CSS. You can't test these directly with `querySelector`, but you can test the underlying CSS selector.

#### Demo: Element Not Found After Swap

Consider this scenario: clicking Edit works the first time, but the second click does nothing.

**The Setup:**

```html
<tr id="artist-row-1">
    <td>AC/DC</td>
    <td>
        <button hx-get="/Artists?handler=Edit&id=1"
                hx-target="#artist-row-1"
                hx-swap="outerHTML">
            Edit
        </button>
    </td>
</tr>
```

The handler returns an edit form that replaces the entire row:

```html
<tr id="artist-edit-1">
    <td colspan="2">
        <form hx-post="/Artists?handler=Update&id=1"
              hx-target="#artist-edit-1"
              hx-swap="outerHTML">
            <input name="name" value="AC/DC" />
            <button type="submit">Save</button>
            <button type="button" 
                    hx-get="/Artists?handler=Cancel&id=1"
                    hx-target="#artist-row-1">
                Cancel
            </button>
        </form>
    </td>
</tr>
```

**The Problem:**

The Cancel button targets `#artist-row-1`, but that element no longer exists. It was replaced by `#artist-edit-1`.

**Debugging Steps:**

1. Open Elements tab after clicking Edit
2. Search for `artist-row-1` - not found
3. The swap with `outerHTML` replaced the element, changing its ID

**The Fix:**

Use a consistent ID, or target a stable container:

```html
<!-- Option 1: Keep the same ID -->
<tr id="artist-row-1">
    <td colspan="2">
        <form hx-post="/Artists?handler=Update&id=1"
              hx-target="#artist-row-1"
              hx-swap="outerHTML">
            <!-- form content -->
        </form>
    </td>
</tr>

<!-- Option 2: Target a stable container -->
<button type="button"
        hx-get="/Artists?handler=Cancel&id=1"
        hx-target="closest tr"
        hx-swap="outerHTML">
    Cancel
</button>
```

This scenario illustrates why the Elements tab matters: it shows the actual DOM state, which might differ from what your code expects.

---

## 21.3 htmx Built-in Debugging Features

htmx includes several features specifically designed for debugging. These work alongside browser DevTools to provide deeper insight into htmx behavior.

### 21.3.1 The htmx.logAll() Method

The `htmx.logAll()` method enables verbose logging of all htmx events. This creates detailed output that traces every step of htmx processing.

#### Enabling in Development Only

You don't want verbose logging in production. Use ASP.NET Core's environment detection to enable it conditionally:

**In _Layout.cshtml:**

```html
@inject IWebHostEnvironment Environment

<!-- htmx and Hyperscript -->
<script src="~/lib/htmx/htmx.min.js"></script>
<script src="~/lib/hyperscript/_hyperscript.min.js"></script>

@if (Environment.IsDevelopment())
{
    <script>
        // Enable verbose htmx logging in development
        htmx.logAll();
        console.log('%chtmx debug mode enabled', 'color: #3b82f6; font-weight: bold;');
    </script>
}
```

Now logging activates automatically during development but stays off in production.

#### Understanding Event Lifecycle Output

With `logAll()` enabled, triggering an htmx action produces output like this:

```
htmx:configRequest
  {elt: button, verb: "get", path: "/Artists?handler=Edit&id=1", headers: {...}, ...}

htmx:beforeRequest  
  {elt: button, xhr: XMLHttpRequest, target: tr#artist-row-1, ...}

htmx:afterRequest
  {elt: button, xhr: XMLHttpRequest, target: tr#artist-row-1, successful: true, ...}

htmx:beforeSwap
  {elt: button, xhr: XMLHttpRequest, target: tr#artist-row-1, serverResponse: "...", ...}

htmx:afterSwap
  {elt: button, target: tr#artist-row-1, ...}

htmx:afterSettle
  {elt: button, target: tr#artist-row-1, ...}
```

Each event represents a stage in the htmx lifecycle:

1. **configRequest**: htmx is configuring the request (you can modify it here)
2. **beforeRequest**: Request is about to be sent
3. **afterRequest**: Response received (check `successful` property)
4. **beforeSwap**: About to insert content into DOM
5. **afterSwap**: Content inserted
6. **afterSettle**: Any scripts in new content have run

When debugging, identify where the chain breaks. If you see `configRequest` but not `beforeRequest`, something prevented the request. If you see `afterRequest` with `successful: false`, the server returned an error.

#### Filtering Specific Events

Full logging is verbose. For targeted debugging, listen to specific events instead:

```javascript
// Only log errors and swaps
const eventsToLog = ['htmx:responseError', 'htmx:afterSwap', 'htmx:swapError'];

eventsToLog.forEach(eventName => {
    document.body.addEventListener(eventName, function(event) {
        console.log(eventName, event.detail);
    });
});
```

This approach focuses on what matters while reducing console noise.

### 21.3.2 The htmx.config Object

htmx's behavior is controlled by a configuration object. Inspecting and modifying this object helps diagnose certain issues.

#### Viewing Current Configuration

In the browser console, type:

```javascript
console.log(htmx.config);
```

You'll see all current settings:

```javascript
{
    historyEnabled: true,
    historyCacheSize: 10,
    refreshOnHistoryMiss: false,
    defaultSwapStyle: "innerHTML",
    defaultSwapDelay: 0,
    defaultSettleDelay: 20,
    includeIndicatorStyles: true,
    indicatorClass: "htmx-indicator",
    requestClass: "htmx-request",
    addedClass: "htmx-added",
    settlingClass: "htmx-settling",
    swappingClass: "htmx-swapping",
    timeout: 0,
    // ... more settings
}
```

#### Useful Settings for Debugging

**Disable History Cache:**

```javascript
htmx.config.historyCacheSize = 0;
```

When debugging back-button issues, disable caching to ensure fresh content loads on every navigation.

**Increase Timeout:**

```javascript
htmx.config.timeout = 60000; // 60 seconds
```

When stepping through server-side code with a debugger, increase the timeout to prevent premature request failures.

**Force History Refresh:**

```javascript
htmx.config.refreshOnHistoryMiss = true;
```

If history restoration fails (cache miss), this forces a full page refresh instead of showing stale content.

**Check Default Swap Style:**

```javascript
console.log(htmx.config.defaultSwapStyle); // "innerHTML" by default
```

If content is replacing elements when you expect it to replace children (or vice versa), verify the default swap style matches your expectations.

### 21.3.3 The htmx:responseError Event

The `htmx:responseError` event fires when the server returns a 4xx or 5xx status code. Setting up a global handler for this event provides consistent error handling.

#### Global Error Handling Setup

Add this to your `site.js` or a script block in your layout:

```javascript
document.body.addEventListener('htmx:responseError', function(event) {
    const xhr = event.detail.xhr;
    const status = xhr.status;
    const path = event.detail.pathInfo?.requestPath || 'unknown';
    
    // Log for debugging
    console.error('htmx request failed:', {
        status: status,
        statusText: xhr.statusText,
        path: path,
        response: xhr.responseText?.substring(0, 500) // First 500 chars
    });
    
    // User-facing error handling
    handleHtmxError(status, path);
});

function handleHtmxError(status, path) {
    const messages = {
        400: 'The request was invalid. Please check your input and try again.',
        401: 'Your session has expired. Please refresh the page to log in again.',
        403: 'You do not have permission to perform this action.',
        404: 'The requested resource was not found.',
        409: 'A conflict occurred. The data may have been modified by another user.',
        421: 'The submitted data was invalid.',
        429: 'Too many requests. Please wait a moment and try again.',
        500: 'A server error occurred. Please try again later.',
        502: 'The server is temporarily unavailable. Please try again later.',
        503: 'The service is currently unavailable. Please try again later.'
    };
    
    const message = messages[status] || `An error occurred (${status}).`;
    
    // Show toast notification if available
    if (typeof showToast === 'function') {
        showToast(message, 'error');
    } else {
        alert(message);
    }
}
```

This handler provides both developer information (console log) and user feedback (toast or alert).

#### Integration with Toast Notifications

If you built the toast system from Chapter 21, integrate it:

```javascript
document.body.addEventListener('htmx:responseError', function(event) {
    const status = event.detail.xhr.status;
    
    // Skip if server sent its own error HTML
    const contentType = event.detail.xhr.getResponseHeader('Content-Type');
    if (contentType?.includes('text/html') && event.detail.xhr.responseText?.includes('error-message')) {
        return; // Let the server's error content display
    }
    
    const messages = {
        400: 'Invalid request. Please check your input.',
        401: 'Session expired. Please log in again.',
        403: 'Permission denied.',
        404: 'Not found.',
        500: 'Server error. Please try again.'
    };
    
    createToast(messages[status] || `Error: ${status}`, 'error');
});
```

### 21.3.4 The htmx:beforeRequest and htmx:configRequest Events

These events let you inspect and modify requests before they're sent.

#### Inspecting Outgoing Requests

The `htmx:configRequest` event fires while htmx is configuring the request. The event detail contains:

```javascript
document.body.addEventListener('htmx:configRequest', function(event) {
    console.log('Request configuration:', {
        verb: event.detail.verb,           // 'get', 'post', etc.
        path: event.detail.path,           // The URL
        headers: event.detail.headers,     // Request headers
        parameters: event.detail.parameters, // Form/query parameters
        target: event.detail.target,       // Target element
        triggeringEvent: event.detail.triggeringEvent // Original DOM event
    });
});
```

This shows exactly what htmx will send, helping verify that parameters and headers are correct.

#### Modifying Requests for Debugging

You can modify the request during `htmx:configRequest`:

```javascript
// Add a debug parameter to all requests during development
document.body.addEventListener('htmx:configRequest', function(event) {
    event.detail.parameters['_debug'] = 'true';
    event.detail.parameters['_timestamp'] = Date.now();
});
```

On the server, check for these parameters:

```csharp
public async Task<IActionResult> OnGetListAsync()
{
    if (Request.Query.ContainsKey("_debug"))
    {
        _logger.LogDebug("Debug request at {Timestamp}", Request.Query["_timestamp"]);
    }
    // ... rest of handler
}
```

You can also add custom headers:

```javascript
document.body.addEventListener('htmx:configRequest', function(event) {
    event.detail.headers['X-Debug-Session'] = sessionStorage.getItem('debugId') || 'none';
});
```

---

## 21.4 ASP.NET Core Server-Side Debugging

Client-side tools show what the browser sends and receives. Server-side debugging reveals what happens between those two points.

### 21.4.1 Logging htmx Requests

#### Identifying htmx Requests in Handlers

Check for the `HX-Request` header to determine if a request came from htmx:

```csharp
public async Task<IActionResult> OnGetAsync()
{
    var isHtmxRequest = Request.Headers.ContainsKey("HX-Request");
    
    _logger.LogInformation(
        "Artists page requested | htmx: {IsHtmx} | Target: {Target} | Trigger: {Trigger}",
        isHtmxRequest,
        Request.Headers["HX-Target"].FirstOrDefault() ?? "(none)",
        Request.Headers["HX-Trigger"].FirstOrDefault() ?? "(none)"
    );
    
    await LoadArtistsAsync();
    
    if (isHtmxRequest)
    {
        return Partial("Artists/_ArtistList", this);
    }
    
    return Page();
}
```

This logs htmx-specific details that help trace request handling.

#### Complete HtmxLoggingMiddleware Implementation

For complete logging across all htmx requests, create middleware:

**Middleware/HtmxLoggingMiddleware.cs**

```csharp
using System.Diagnostics;

namespace ChinookDashboard.Middleware;

public class HtmxLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HtmxLoggingMiddleware> _logger;

    public HtmxLoggingMiddleware(RequestDelegate next, ILogger<HtmxLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only log htmx requests
        if (!context.Request.Headers.ContainsKey("HX-Request"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        // Extract htmx headers
        var htmxTarget = context.Request.Headers["HX-Target"].FirstOrDefault();
        var htmxTrigger = context.Request.Headers["HX-Trigger"].FirstOrDefault();
        var htmxTriggerName = context.Request.Headers["HX-Trigger-Name"].FirstOrDefault();
        var htmxCurrentUrl = context.Request.Headers["HX-Current-URL"].FirstOrDefault();

        _logger.LogDebug(
            "[htmx:{RequestId}] {Method} {Path}{Query} | Target: {Target} | Trigger: {Trigger}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            htmxTarget ?? "(default)",
            htmxTrigger ?? htmxTriggerName ?? "(unknown)"
        );

        try
        {
            await _next(context);

            stopwatch.Stop();

            _logger.LogDebug(
                "[htmx:{RequestId}] Completed {StatusCode} in {ElapsedMs}ms | Content-Length: {Length}",
                requestId,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                context.Response.ContentLength ?? 0
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "[htmx:{RequestId}] Failed after {ElapsedMs}ms | {Method} {Path}",
                requestId,
                stopwatch.ElapsedMilliseconds,
                context.Request.Method,
                context.Request.Path
            );

            throw;
        }
    }
}

// Extension method for clean registration
public static class HtmxLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseHtmxLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HtmxLoggingMiddleware>();
    }
}
```

#### Registration in Program.cs

Add the middleware in development only:

```csharp
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseHtmxLogging(); // Add htmx logging
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
// ... rest of pipeline
```

Now your development logs show htmx request flow:

```
[htmx:a1b2c3d4] GET /Artists?handler=List&search=ac | Target: artist-list | Trigger: search-input
[htmx:a1b2c3d4] Completed 200 in 45ms | Content-Length: 2847
```

### 21.4.2 Debugging Partial View Returns

One of the most common htmx issues is returning the wrong content from the server.

#### Common Issues

**Returning Page() Instead of Partial()**

```csharp
// Wrong - returns full page with layout
public async Task<IActionResult> OnGetListAsync()
{
    await LoadArtistsAsync();
    return Page(); // Includes <html>, <head>, <body>, etc.
}

// Correct - returns just the partial content
public async Task<IActionResult> OnGetListAsync()
{
    await LoadArtistsAsync();
    return Partial("Artists/_ArtistList", this);
}
```

**Wrong Partial View Path**

```csharp
// Wrong - looks for Pages/Artists/_List.cshtml
return Partial("_List", model);

// Correct - full path from Shared folder
return Partial("Artists/_ArtistList", model);

// Also correct - explicit path
return Partial("~/Pages/Shared/Artists/_ArtistList.cshtml", model);
```

**Model Type Mismatch**

```csharp
// Partial expects ArtistListModel but receives List<Artist>
return Partial("Artists/_ArtistList", artists); // Wrong type

// Correct - pass the expected model type
return Partial("Artists/_ArtistList", new ArtistListModel { Artists = artists });
```

#### Diagnostic Extension Method

Create an extension that logs partial view returns:

**Extensions/PartialResultExtensions.cs**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChinookDashboard.Extensions;

public static class PartialResultExtensions
{
    public static PartialViewResult PartialWithDiagnostics(
        this PageModel page,
        string viewName,
        object? model,
        ILogger logger)
    {
        var modelType = model?.GetType().Name ?? "null";
        var modelCount = GetItemCount(model);
        
        logger.LogDebug(
            "Returning partial '{ViewName}' | Model: {ModelType} | Items: {Count}",
            viewName,
            modelType,
            modelCount
        );

        return page.Partial(viewName, model);
    }

    private static string GetItemCount(object? model)
    {
        if (model == null) return "n/a";
        
        // Check for common collection types
        if (model is System.Collections.ICollection collection)
            return collection.Count.ToString();
            
        // Check for paginated list
        var itemsProperty = model.GetType().GetProperty("Items");
        if (itemsProperty?.GetValue(model) is System.Collections.ICollection items)
            return items.Count.ToString();
            
        return "n/a";
    }
}
```

Use it in your handlers:

```csharp
public async Task<IActionResult> OnGetListAsync()
{
    await LoadArtistsAsync();
    return this.PartialWithDiagnostics("Artists/_ArtistList", Artists, _logger);
}
```

#### Adding Debug Headers to Responses

In development, add headers that help identify what was returned:

```csharp
public async Task<IActionResult> OnGetListAsync()
{
    await LoadArtistsAsync();
    
    if (Request.Headers.ContainsKey("HX-Request"))
    {
        // Add debug headers in development
        Response.Headers.Append("X-Debug-Partial", "Artists/_ArtistList");
        Response.Headers.Append("X-Debug-Model", Artists?.GetType().Name ?? "null");
        Response.Headers.Append("X-Debug-Count", Artists?.Count.ToString() ?? "0");
    }
    
    return Partial("Artists/_ArtistList", this);
}
```

These headers appear in the Network tab's response headers, confirming what the server intended to return.

### 21.4.3 Anti-Forgery Token Debugging

Anti-forgery token failures are among the most frustrating htmx issues because they silently fail with a 400 status code.

#### Common Issues and Symptoms

**Symptoms:**
- POST/PUT/DELETE requests return 400 Bad Request
- Server logs show "The required antiforgery cookie is not present" or "The antiforgery token could not be decrypted"
- Request works in a regular form but fails with htmx

**Common Causes:**
1. Token not included in htmx request headers
2. No `<form>` element with token on the page
3. `[ValidateAntiForgeryToken]` attribute missing from handler
4. Token expired
5. Cookie and token mismatch after deployment

#### Debugging Token Flow

**Step 1: Verify Token Exists on Page**

In browser console:

```javascript
// Check for token input
document.querySelector('input[name="__RequestVerificationToken"]');
// Should return an <input> element with a long value

// Get the token value
document.querySelector('input[name="__RequestVerificationToken"]')?.value;
// Should return a long string like "CfDJ8N..."
```

If this returns `null`, add a form with an anti-forgery token to your layout:

```html
<!-- In _Layout.cshtml, typically in a hidden location -->
<form id="__AjaxAntiForgeryForm" asp-antiforgery="true" style="display:none;"></form>
```

**Step 2: Verify htmx Sends the Token**

Check that your JavaScript includes the token in requests:

```javascript
document.body.addEventListener('htmx:configRequest', function(event) {
    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    if (token) {
        event.detail.headers['RequestVerificationToken'] = token.value;
        console.log('Anti-forgery token attached:', token.value.substring(0, 20) + '...');
    } else {
        console.warn('No anti-forgery token found on page!');
    }
});
```

**Step 3: Verify Server Receives the Token**

Check the Network tab. Under Request Headers, you should see:

```
RequestVerificationToken: CfDJ8N...
```

If the header is missing, the client-side code isn't attaching it.

**Step 4: Verify Server Configuration**

In `Program.cs`:

```csharp
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken"; // Must match client header name
});
```

And verify your handler has the attribute:

```csharp
[ValidateAntiForgeryToken]
public async Task<IActionResult> OnPostUpdateAsync(int id, string name)
{
    // This will fail without the attribute when tokens are enabled
}
```

#### Complete Verification Code

Add this diagnostic code temporarily to identify token issues:

```csharp
// In your PageModel, add a diagnostic handler
public IActionResult OnPostDiagnoseTokenAsync()
{
    var headerToken = Request.Headers["RequestVerificationToken"].FirstOrDefault();
    var formToken = Request.Form["__RequestVerificationToken"].FirstOrDefault();
    var cookieToken = Request.Cookies
        .FirstOrDefault(c => c.Key.StartsWith(".AspNetCore.Antiforgery"))
        .Value;

    _logger.LogInformation("Token Diagnosis: Header={Header}, Form={Form}, Cookie={Cookie}",
        headerToken?.Substring(0, Math.Min(20, headerToken?.Length ?? 0)) ?? "(missing)",
        formToken?.Substring(0, Math.Min(20, formToken?.Length ?? 0)) ?? "(missing)",
        !string.IsNullOrEmpty(cookieToken) ? "(present)" : "(missing)"
    );

    return Content("Check server logs for token diagnosis");
}
```

### 21.4.4 Debugging Model Binding

Model binding transforms request data into handler parameters. When binding fails, parameters arrive as null or with unexpected values.

#### Logging Bound Values

Add explicit logging to see what values arrive:

```csharp
public async Task<IActionResult> OnPostUpdateAsync(
    int id,
    [FromForm] string? name,
    [FromForm] int? genreId)
{
    _logger.LogDebug(
        "Model binding received: id={Id}, name={Name}, genreId={GenreId}",
        id,
        name ?? "(null)",
        genreId?.ToString() ?? "(null)"
    );

    // Rest of handler...
}
```

If values are null when you expect them to have data, check the parameter names match the form field names.

#### Diagnosing Binding Failures via ModelState

When validation fails, check `ModelState` for binding errors:

```csharp
public async Task<IActionResult> OnPostUpdateAsync(ArtistEditModel model)
{
    if (!ModelState.IsValid)
    {
        foreach (var entry in ModelState)
        {
            if (entry.Value.Errors.Any())
            {
                _logger.LogWarning(
                    "Binding/validation error for '{Key}': {Errors}",
                    entry.Key,
                    string.Join("; ", entry.Value.Errors.Select(e => 
                        e.ErrorMessage ?? e.Exception?.Message ?? "Unknown error"))
                );
            }
        }
        
        return Partial("Artists/_ArtistEditRow", model);
    }

    // Continue with valid model...
}
```

#### Common Binding Issues

**Parameter Name Mismatch:**

```html
<!-- HTML uses 'artistName' -->
<input name="artistName" value="AC/DC" />
```

```csharp
// Handler expects 'name' - won't bind!
public async Task<IActionResult> OnPostUpdateAsync(string name)

// Fix: Match the name
public async Task<IActionResult> OnPostUpdateAsync(string artistName)

// Or use [FromForm] with explicit name
public async Task<IActionResult> OnPostUpdateAsync([FromForm(Name = "artistName")] string name)
```

**Checkbox Binding:**

Checkboxes only submit when checked. For unchecked state, you need a hidden field:

```html
<!-- Without hidden field, unchecked = no value sent = null -->
<input type="checkbox" name="isActive" value="true" />

<!-- With hidden field, unchecked = "false", checked = "true" -->
<input type="hidden" name="isActive" value="false" />
<input type="checkbox" name="isActive" value="true" />
```

**Array Binding:**

Multiple values need matching names with proper indexing:

```html
<!-- For List<int> selectedIds -->
<input type="checkbox" name="selectedIds" value="1" />
<input type="checkbox" name="selectedIds" value="2" />
<input type="checkbox" name="selectedIds" value="3" />
```

```csharp
// Handler receives all checked values
public async Task<IActionResult> OnPostBulkDeleteAsync(List<int> selectedIds)
{
    _logger.LogDebug("Selected IDs: {Ids}", string.Join(", ", selectedIds));
    // ...
}
```

**Debugging Request Body:**

For complex binding issues, log the raw request:

```csharp
public async Task<IActionResult> OnPostUpdateAsync()
{
    // Log all form values
    foreach (var key in Request.Form.Keys)
    {
        _logger.LogDebug("Form[{Key}] = {Value}", key, Request.Form[key]);
    }
    
    // Log query parameters too
    foreach (var key in Request.Query.Keys)
    {
        _logger.LogDebug("Query[{Key}] = {Value}", key, Request.Query[key]);
    }
    
    // Then bind manually or use TryUpdateModelAsync
    var id = int.Parse(Request.Form["id"]!);
    var name = Request.Form["name"].ToString();
    
    // Continue with values...
}
```

This diagnostic approach reveals exactly what data the server received, independent of model binding.

## 21.5 Common htmx Problems and Solutions

This section catalogs the most frequent htmx issues and provides systematic approaches to diagnose and fix each one. Each problem includes symptoms, debugging steps, root cause analysis, and complete solutions.

### 21.5.1 "Nothing Happens" When Clicking

The most common htmx complaint: you click a button or link and nothing visible happens. No error, no feedback, no change.

#### Diagnostic Checklist

Work through this checklist in order. Most "nothing happens" issues resolve within the first few items.

**1. Is htmx loaded?**

Open browser console and type:
```javascript
typeof htmx
```
If this returns `"undefined"`, htmx isn't loaded. Check your script include.

**2. Are there JavaScript errors?**

Check the Console tab for red error messages. Any JavaScript error might prevent htmx from functioning.

**3. Does the element have htmx attributes?**

Right-click the element, select Inspect, and verify htmx attributes are present:
```html
<button hx-get="/Artists?handler=Edit" ...>
```
If attributes are missing, check your Razor view source.

**4. Is a network request being made?**

Open Network tab, filter to Fetch/XHR, then click the element. If no request appears, the trigger isn't firing.

**5. What's the response status?**

If a request appears, check its status code:
- **200**: Server succeeded, problem is with swap
- **204**: No content returned, htmx does nothing (by design)
- **400**: Bad request (often anti-forgery token)
- **404**: Handler or page not found
- **500**: Server error

**6. Does the response contain expected content?**

Click the request in Network tab, check Response or Preview. Verify it's the HTML you expect.

**7. Does the target element exist?**

In Console, test the selector:
```javascript
document.querySelector('#my-target')
```
If this returns `null`, the target doesn't exist.

**8. Is hx-swap correct for the situation?**

If `hx-swap="outerHTML"` but you're returning content that should go inside the element, or vice versa, the result may look like nothing happened.

#### Common Causes with Code Examples

**htmx Not Processing Dynamic Content**

When content is loaded via htmx, new elements in that content need htmx processing. By default, htmx processes new content automatically after swaps. But if you're adding content via other JavaScript, htmx won't know about it.

```html
<!-- This button loads new content -->
<button hx-get="/load-more" hx-target="#container">Load More</button>

<div id="container">
    <!-- htmx loads this content -->
    <div class="item">
        <!-- This button works - htmx processes it after swap -->
        <button hx-delete="/items/1">Delete</button>
    </div>
</div>
```

But if you add content with vanilla JavaScript:

```javascript
// This bypasses htmx processing
container.innerHTML += '<button hx-delete="/items/2">Delete</button>';
// The new button won't work!

// Fix: Tell htmx to process the new content
container.innerHTML += '<button hx-delete="/items/2">Delete</button>';
htmx.process(container);
```

**Missing htmx.js Include**

The script tag might be missing, misplaced, or failing to load:

```html
<!-- Check that this exists in _Layout.cshtml -->
<script src="~/lib/htmx/htmx.min.js"></script>

<!-- Or from CDN -->
<script src="https://unpkg.com/htmx.org@2.0.4"></script>
```

Verify in Network tab that the script loads with status 200. A 404 means the file path is wrong.

**Element Inside a Form with Conflicting Action**

```html
<!-- Problem: Button inside form triggers form submit, not htmx -->
<form action="/traditional-submit" method="post">
    <button hx-get="/htmx-action">This won't work as expected</button>
</form>

<!-- Solution 1: Use type="button" -->
<form action="/traditional-submit" method="post">
    <button type="button" hx-get="/htmx-action">Now htmx handles it</button>
</form>

<!-- Solution 2: Move button outside form -->
<form action="/traditional-submit" method="post">
    <!-- form content -->
</form>
<button hx-get="/htmx-action">Separate from form</button>
```

#### Demo: The Silent Button

**Symptom**: Clicking "Load Artists" does nothing. No console errors, no network activity.

**The Code**:
```html
@page
@model ArtistsModel

<h1>Artists</h1>

<button hx-get="/Artists?handler=List" 
        hx-target="#artist-container">
    Load Artists
</button>

<div id="artist-container">
    <!-- Artists load here -->
</div>

@section Scripts {
    <!-- Scripts section exists but... -->
}
```

**Debug Steps**:

1. Console: `typeof htmx` returns `"undefined"` - htmx isn't loaded!

2. Check _Layout.cshtml - the htmx script is there

3. Check if the Scripts section renders: look at page source, htmx script is missing from this page

4. Found it: _Layout.cshtml has `@RenderSection("Scripts", required: false)` but the htmx include is inside that section in a _Scripts partial, and this page has its own Scripts section that overrides it

**Root Cause**: The page's `@section Scripts` overrides the layout's script section, excluding the htmx include.

**Solution**:

Move htmx to the layout body, outside any section:

```html
<!-- _Layout.cshtml - place before </body>, outside sections -->
<script src="~/lib/htmx/htmx.min.js"></script>
<script src="~/lib/hyperscript/_hyperscript.min.js"></script>

@await RenderSectionAsync("Scripts", required: false)
</body>
```

**Prevention**: Always include htmx in the main layout body, not in a replaceable section.

### 21.5.2 Content Replaces Wrong Element

You click a button, content updates, but it appears in the wrong place or replaces the wrong element.

#### Duplicate ID Problems

HTML IDs must be unique. When multiple elements share an ID, `querySelector` returns only the first one.

**Symptom**: Editing artist row 5 updates artist row 1 instead.

**The Problem**:
```html
<!-- Generated in a loop without unique IDs -->
<tr id="artist-row">  <!-- First occurrence -->
    <td>AC/DC</td>
    <td><button hx-get="/edit/1" hx-target="#artist-row">Edit</button></td>
</tr>
<tr id="artist-row">  <!-- Duplicate! -->
    <td>Accept</td>
    <td><button hx-get="/edit/2" hx-target="#artist-row">Edit</button></td>
</tr>
```

**The Fix**:
```html
@foreach (var artist in Model.Artists)
{
    <tr id="artist-row-@artist.Id">
        <td>@artist.Name</td>
        <td>
            <button hx-get="/Artists?handler=Edit&id=@artist.Id" 
                    hx-target="#artist-row-@artist.Id">
                Edit
            </button>
        </td>
    </tr>
}
```

**Debug Technique**:
```javascript
// Find all elements with a given ID (should be only one!)
document.querySelectorAll('[id="artist-row"]').length
// If > 1, you have duplicate IDs
```

#### Target Doesn't Exist When Response Arrives

The target element might be removed by a previous operation or never existed.

**Symptom**: First click works, subsequent clicks do nothing.

**The Problem**:
```html
<!-- Initial state -->
<div id="modal-container"></div>

<button hx-get="/Artists?handler=CreateForm" 
        hx-target="#modal-container">
    Add Artist
</button>
```

```html
<!-- Response replaces container with modal content -->
<div class="modal" id="create-modal">
    <form hx-post="/Artists?handler=Create"
          hx-target="#modal-container">  <!-- This ID no longer exists! -->
        ...
    </form>
</div>
```

The form's `hx-target` points to `#modal-container`, which was replaced by `#create-modal`.

**The Fix**:

Option 1: Use `hx-swap="innerHTML"` to preserve the container:
```html
<button hx-get="/Artists?handler=CreateForm" 
        hx-target="#modal-container"
        hx-swap="innerHTML">
    Add Artist
</button>
```

Option 2: Update the form's target to match the new structure:
```html
<div class="modal" id="create-modal">
    <form hx-post="/Artists?handler=Create"
          hx-target="#create-modal"
          hx-swap="outerHTML">
        ...
    </form>
</div>
```

#### Debugging Target Resolution

Add an event listener that warns when targets don't exist:

```javascript
document.body.addEventListener('htmx:configRequest', function(event) {
    const elt = event.detail.elt;
    const targetSelector = elt.getAttribute('hx-target');
    
    if (targetSelector && !targetSelector.startsWith('this') && 
        !targetSelector.startsWith('closest') && !targetSelector.startsWith('find')) {
        const target = document.querySelector(targetSelector);
        if (!target) {
            console.error('htmx target not found:', {
                selector: targetSelector,
                trigger: elt,
                suggestion: 'Check if target was removed by previous swap'
            });
        }
    }
});
```

This logs a warning before the request is made if the target element doesn't exist.

#### Demo: Second Click Fails

**Symptom**: Click "Edit" on a row, the edit form appears. Click "Cancel" and nothing happens.

**The Code**:
```html
<!-- Artist row partial -->
<tr id="artist-row-1">
    <td>AC/DC</td>
    <td>
        <button hx-get="/Artists?handler=Edit&id=1"
                hx-target="#artist-row-1"
                hx-swap="outerHTML">
            Edit
        </button>
    </td>
</tr>
```

```html
<!-- Edit form partial (returned from server) -->
<tr id="artist-edit-1">  <!-- Different ID! -->
    <td colspan="2">
        <form hx-post="/Artists?handler=Update&id=1">
            <input name="name" value="AC/DC" />
            <button type="submit">Save</button>
            <button type="button"
                    hx-get="/Artists?handler=Cancel&id=1"
                    hx-target="#artist-row-1">  <!-- Original ID no longer exists -->
                Cancel
            </button>
        </form>
    </td>
</tr>
```

**Debug Steps**:

1. First click: Network shows 200, row becomes edit form - working
2. Click Cancel: Network shows 200, nothing changes
3. Check Console: "htmx target not found: #artist-row-1" (if using our debug listener)
4. Elements tab: search for "artist-row-1" - not found, only "artist-edit-1" exists

**Root Cause**: The `outerHTML` swap replaced `#artist-row-1` with `#artist-edit-1`. The Cancel button still targets the old ID.

**Solution**: Use consistent IDs or relative targeting:
```html
<!-- Option 1: Keep same ID -->
<tr id="artist-row-1">
    <td colspan="2">
        <form hx-post="/Artists?handler=Update&id=1"
              hx-target="#artist-row-1"
              hx-swap="outerHTML">
            ...
        </form>
    </td>
</tr>

<!-- Option 2: Use relative selector -->
<button type="button"
        hx-get="/Artists?handler=Cancel&id=1"
        hx-target="closest tr"
        hx-swap="outerHTML">
    Cancel
</button>
```

### 21.5.3 OOB Updates Not Working

Out-of-band (OOB) updates let a single response update multiple page areas. When they fail, only the primary target updates.

#### Troubleshooting Steps

**1. Verify OOB element has correct attribute**

The element in the response must have `hx-swap-oob`:
```html
<!-- In server response -->
<span id="artist-count" hx-swap-oob="true">275</span>
```

**2. Verify target element exists in DOM**

The OOB element's ID must match an existing element:
```javascript
document.querySelector('#artist-count')  // Must not be null
```

**3. Check OOB element is at root level of response**

OOB elements must be direct children of the response, not nested inside other elements:
```html
<!-- Correct: OOB at root level -->
<div>Primary content</div>
<span id="artist-count" hx-swap-oob="true">275</span>

<!-- Wrong: OOB nested inside container -->
<div>
    Primary content
    <span id="artist-count" hx-swap-oob="true">275</span>
</div>
```

**4. Verify OOB value syntax**

Valid values include:
- `true` - replace element (same as `outerHTML`)
- `innerHTML` - replace element's content
- `outerHTML` - replace entire element
- `beforebegin`, `afterbegin`, `beforeend`, `afterend` - insert adjacent
- `delete` - remove the target element
- `none` - do nothing (useful for conditional updates)

#### Demo: OOB Element ID Mismatch

**Symptom**: Creating a new artist adds the row but doesn't update the count badge.

**The Response**:
```html
<!-- New row (primary swap target) -->
<tr id="artist-row-276" hx-swap-oob="afterbegin:#artist-table-body">
    <td>New Artist</td>
    <td>0 albums</td>
</tr>

<!-- Count update (OOB) -->
<span id="artistCount" hx-swap-oob="true">276</span>
```

**The Page**:
```html
<span id="artist-count">275</span>  <!-- Note: kebab-case -->
```

**Debug Steps**:

1. Network tab shows 200 response with both elements
2. Table row appears (primary swap worked)
3. Count still shows 275 (OOB failed)
4. Response Preview shows `id="artistCount"` (camelCase)
5. Elements tab shows page has `id="artist-count"` (kebab-case)

**Root Cause**: ID mismatch due to inconsistent naming convention.

**Solution**: Use consistent ID naming:
```html
<!-- Server response - match page ID exactly -->
<span id="artist-count" hx-swap-oob="true">276</span>
```

#### Debugging OOB with Events

htmx fires events for OOB processing:

```javascript
// Log OOB swap attempts
document.body.addEventListener('htmx:oobBeforeSwap', function(event) {
    console.log('OOB before swap:', {
        fragment: event.detail.fragment?.outerHTML?.substring(0, 100),
        target: event.detail.target,
        targetId: event.detail.target?.id
    });
});

// Log successful OOB swaps
document.body.addEventListener('htmx:oobAfterSwap', function(event) {
    console.log('OOB after swap:', {
        target: event.detail.target?.id || event.detail.target
    });
});

// Log OOB errors
document.body.addEventListener('htmx:oobErrorNoTarget', function(event) {
    console.error('OOB target not found:', {
        content: event.detail.content?.outerHTML?.substring(0, 100)
    });
});
```

### 21.5.4 Forms Submit But Data Is Lost

The form submits, the server receives the request, but form field values are missing or empty.

#### Button Outside Form Problem

Buttons with htmx attributes outside a form don't automatically include form data.

**Symptom**: Server receives POST request but all form values are null.

**The Problem**:
```html
<form id="artist-form">
    <input name="name" value="AC/DC" />
    <input name="genre" value="Rock" />
</form>

<!-- Button outside form - doesn't include form data! -->
<button hx-post="/Artists?handler=Create"
        hx-target="#result">
    Save
</button>
```

**The Fix**:

Option 1: Move button inside form:
```html
<form hx-post="/Artists?handler=Create" hx-target="#result">
    <input name="name" value="AC/DC" />
    <input name="genre" value="Rock" />
    <button type="submit">Save</button>
</form>
```

Option 2: Use `hx-include` to specify form:
```html
<form id="artist-form">
    <input name="name" value="AC/DC" />
    <input name="genre" value="Rock" />
</form>

<button hx-post="/Artists?handler=Create"
        hx-include="#artist-form"
        hx-target="#result">
    Save
</button>
```

Option 3: Use `form` attribute (HTML5):
```html
<form id="artist-form" hx-post="/Artists?handler=Create" hx-target="#result">
    <input name="name" value="AC/DC" />
    <input name="genre" value="Rock" />
</form>

<!-- Button references form by ID -->
<button type="submit" form="artist-form">Save</button>
```

#### hx-include vs Form Containment

`hx-include` specifies additional elements to include in the request. Understanding its behavior prevents data loss:

```html
<!-- hx-include adds to, doesn't replace, automatic inclusion -->
<form hx-post="/save">
    <input name="included-by-form" value="yes" />
    
    <button type="submit" hx-include="#extra-data">Save</button>
</form>

<div id="extra-data">
    <input name="extra" value="also included" />
</div>
```

Both `included-by-form` and `extra` are sent because the button is inside the form AND specifies `hx-include`.

**Common Mistake**:
```html
<!-- hx-include on the form itself doesn't do what you might expect -->
<form hx-post="/save" hx-include="this">  <!-- 'this' is redundant -->
    <input name="field1" value="yes" />
</form>
```

#### Debugging Form Data

Add a listener that logs exactly what htmx will send:

```javascript
document.body.addEventListener('htmx:configRequest', function(event) {
    if (event.detail.verb !== 'get') {
        console.log('Form submission data:', {
            method: event.detail.verb.toUpperCase(),
            path: event.detail.path,
            parameters: event.detail.parameters,
            // Show which element triggered submission
            trigger: event.detail.elt.tagName + 
                    (event.detail.elt.id ? '#' + event.detail.elt.id : '') +
                    (event.detail.elt.name ? '[name=' + event.detail.elt.name + ']' : '')
        });
    }
});
```

This logs parameters before the request is sent, showing exactly what data htmx collected.

### 21.5.5 Anti-Forgery Token Failures

Anti-forgery token issues cause cryptic 400 errors that give little indication of the actual problem.

#### Symptoms

**Network Tab Shows**:
- Status: 400 Bad Request
- Response may contain: "The required antiforgery cookie is not present" or "The antiforgery token could not be decrypted"

**Console Shows**:
- `htmx:responseError` with status 400
- No JavaScript errors (this is a server-side rejection)

**Server Logs Show**:
- "Antiforgery token validation failed"
- "The required antiforgery form field '__RequestVerificationToken' is not present"

#### 4-Step Diagnostic Process

**Step 1: Verify Token Exists in Page**

```javascript
// Run in browser console
const token = document.querySelector('input[name="__RequestVerificationToken"]');
console.log('Token element:', token);
console.log('Token value:', token?.value?.substring(0, 30) + '...');
```

If `null`, add a form with anti-forgery to your layout:
```html
<!-- In _Layout.cshtml -->
@using Microsoft.AspNetCore.Antiforgery
@inject IAntiforgery Antiforgery

<!-- Hidden form to provide token -->
<form id="__AntiForgeryForm" method="post" style="display:none">
    @Html.AntiForgeryToken()
</form>
```

**Step 2: Verify JavaScript Sends Token**

Check that your htmx configuration includes the token:
```javascript
// This should be in your layout or site.js
document.body.addEventListener('htmx:configRequest', function(event) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    if (token) {
        event.detail.headers['RequestVerificationToken'] = token.value;
    }
});
```

Test by adding logging:
```javascript
document.body.addEventListener('htmx:configRequest', function(event) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    if (token) {
        event.detail.headers['RequestVerificationToken'] = token.value;
        console.log('Token attached to request');
    } else {
        console.warn('NO TOKEN FOUND - request will fail validation');
    }
});
```

**Step 3: Verify Server Receives Token**

Check Network tab, click the failed request, look at Request Headers:
```
RequestVerificationToken: CfDJ8...
```

If missing, the JavaScript isn't attaching it. If present, continue to step 4.

**Step 4: Verify Server Configuration**

Check `Program.cs`:
```csharp
builder.Services.AddAntiforgery(options =>
{
    // This name MUST match what JavaScript sends
    options.HeaderName = "RequestVerificationToken";
});
```

Check handler has attribute:
```csharp
[ValidateAntiForgeryToken]
public async Task<IActionResult> OnPostCreateAsync(...)
```

#### Complete Solution Implementation

**_Layout.cshtml**:
```html
@using Microsoft.AspNetCore.Antiforgery
@inject IAntiforgery Antiforgery

<!DOCTYPE html>
<html>
<head>
    <!-- head content -->
</head>
<body>
    <!-- Provide anti-forgery token for htmx requests -->
    <form id="__AntiForgeryForm" method="post" style="display:none">
        @Html.AntiForgeryToken()
    </form>
    
    <!-- page content -->
    @RenderBody()
    
    <script src="~/lib/htmx/htmx.min.js"></script>
    <script>
        // Attach anti-forgery token to all htmx requests
        document.body.addEventListener('htmx:configRequest', function(event) {
            // Only add for non-GET requests
            if (event.detail.verb === 'get') return;
            
            const token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (token) {
                event.detail.headers['RequestVerificationToken'] = token.value;
            } else {
                console.error('Anti-forgery token not found. POST/PUT/DELETE will fail.');
            }
        });
    </script>
    
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

**Program.cs**:
```csharp
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});
```

**Handler**:
```csharp
[ValidateAntiForgeryToken]
public async Task<IActionResult> OnPostCreateAsync(string name)
{
    // Token validated automatically by attribute
    // Handler only executes if validation passes
}
```

### 21.5.6 History/Back Button Issues

htmx can integrate with browser history, but misconfiguration leads to confusing back-button behavior.

#### Common Problems

**Stale Content on Back Button**

After navigating forward with htmx, pressing back shows outdated content.

**URL Doesn't Update**

User applies filters but the URL stays the same, making the view non-bookmarkable.

**Scroll Position Lost**

After navigating back, page scrolls to top instead of previous position.

#### Debugging History Events

htmx fires events during history operations:

```javascript
// Log history push
document.body.addEventListener('htmx:pushedIntoHistory', function(event) {
    console.log('History push:', event.detail.path);
});

// Log history restore attempts
document.body.addEventListener('htmx:historyRestore', function(event) {
    console.log('History restore:', {
        path: event.detail.path,
        item: event.detail.item
    });
});

// Log history cache misses
document.body.addEventListener('htmx:historyCacheMiss', function(event) {
    console.log('History cache miss:', event.detail.path);
});

// Log history cache errors
document.body.addEventListener('htmx:historyCacheMissLoad', function(event) {
    console.log('Loading page for cache miss:', event.detail.path);
});
```

#### Demo: Filters Not Preserved on Back Button

**Symptom**: Search for "AC/DC", results appear. Click an artist to view details. Press back. Search is gone, showing all artists.

**The Code**:
```html
<!-- Search input without hx-push-url -->
<input type="text" 
       name="search"
       hx-get="/Artists?handler=List"
       hx-trigger="keyup changed delay:300ms"
       hx-target="#artist-list">

<!-- Artist link without history support -->
<a hx-get="/Artists?handler=Detail&id=1"
   hx-target="#content">
    AC/DC
</a>
```

**Debug Steps**:

1. Type "AC/DC" in search - URL stays `/Artists`
2. Click artist - URL stays `/Artists`
3. Press back - URL still `/Artists`, content jumps to all artists
4. No `htmx:pushedIntoHistory` events in console

**Root Cause**: Neither the search nor the detail link uses `hx-push-url`, so the browser history never records the filtered state.

**Solution**:

```html
<!-- Search with URL push -->
<input type="text" 
       name="search"
       hx-get="/Artists?handler=List"
       hx-trigger="keyup changed delay:300ms"
       hx-target="#artist-list"
       hx-push-url="true">

<!-- Detail link with URL push -->
<a hx-get="/Artists?handler=Detail&id=1"
   hx-target="#content"
   hx-push-url="true">
    AC/DC
</a>

<!-- Mark the content area for history restoration -->
<div id="content" hx-history-elt>
    <!-- Content that should be restored on back button -->
</div>
```

Now the URL updates with each action, and back button restores previous content.

**Additional Fix - Server-Side Support**:

The server must handle direct navigation to filtered URLs:

```csharp
public async Task<IActionResult> OnGetAsync(string? search)
{
    // Load data with filter
    Artists = await _artistService.SearchAsync(search);
    
    // For htmx requests, return partial
    if (Request.Headers.ContainsKey("HX-Request"))
    {
        return Partial("Artists/_ArtistList", this);
    }
    
    // For direct navigation (including back button cache miss), return full page
    return Page();
}
```

---

## 21.6 Debugging Hyperscript

Hyperscript provides client-side interactivity through a human-readable syntax. When Hyperscript doesn't work as expected, debugging requires understanding its unique error reporting and syntax rules.

### 21.6.1 Hyperscript Syntax Errors

Hyperscript has its own syntax distinct from JavaScript. Common mistakes come from mixing the two.

#### JavaScript Syntax in Hyperscript

**Wrong**: Using JavaScript method calls
```html
<button _="on click document.getElementById('modal').classList.add('open')">
    Open Modal
</button>
```

Hyperscript doesn't understand JavaScript syntax. This silently fails or produces parsing errors.

**Correct**: Using Hyperscript syntax
```html
<button _="on click add .open to #modal">
    Open Modal
</button>
```

**Wrong**: JavaScript conditionals
```html
<div _="on load if (count > 0) { show #results }">
```

**Correct**: Hyperscript conditionals
```html
<div _="on load if count > 0 show #results">
```

**Wrong**: JavaScript string concatenation
```html
<button _="on click set #output.innerHTML to 'Count: ' + count">
```

**Correct**: Hyperscript string interpolation
```html
<button _="on click set #output's innerHTML to `Count: ${count}`">
```

#### Missing 'then' Between Actions

Hyperscript commands must be chained with `then`:

**Wrong**: Missing connectors
```html
<div _="on load add .visible to me wait 2s remove .visible from me">
```

This might partially work or fail unpredictably.

**Correct**: Chain with 'then'
```html
<div _="on load add .visible to me then wait 2s then remove .visible from me">
```

#### Wrong Element References

Hyperscript has specific syntax for referencing elements:

**Wrong**: JavaScript-style element access
```html
<button _="on click set document.querySelector('#count').textContent to '5'">
```

**Correct**: Hyperscript element references
```html
<!-- By ID -->
<button _="on click set #count's textContent to '5'">

<!-- By class (first match) -->
<button _="on click set .counter's textContent to '5'">

<!-- Relative to current element -->
<button _="on click set the closest <div/>'s textContent to '5'">

<!-- Using 'me' for self-reference -->
<button _="on click add .clicked to me">
```

#### Enabling Hyperscript Debug Mode

Hyperscript can output debug information:

```html
<script src="~/lib/hyperscript/_hyperscript.min.js"></script>
<script>
    // Enable debug mode
    _hyperscript.config.debug = true;
</script>
```

With debug mode enabled, Hyperscript logs parsing information and execution details to the console.

You can also add inline debugging:

```html
<!-- Use 'log' to output values during execution -->
<button _="on click 
             log 'Button clicked'
             set x to #count's textContent
             log `Current count: ${x}`
             set #count's textContent to (x as Int) + 1
             log 'Count incremented'">
    Increment
</button>
```

### 21.6.2 Hyperscript Event Issues

Events in Hyperscript follow specific patterns that differ from JavaScript event handling.

#### Event Bubbling Considerations

By default, Hyperscript's `on click` triggers when the element or any descendant is clicked:

```html
<!-- Clicking the span also triggers the div's handler -->
<div _="on click add .selected to me">
    <span>Click me</span>
</div>
```

To handle only direct clicks:

```html
<!-- Only trigger if the click target is specifically this div -->
<div _="on click if event.target is me add .selected to me">
    <span>This click won't trigger the handler</span>
</div>
```

#### Being Specific About Event Targets

Filter events to specific child elements:

```html
<!-- Only respond to clicks on buttons within this container -->
<div _="on click from <button/> in me
          log 'Button clicked'
          set the target's textContent to 'Clicked!'">
    <button>Click me</button>
    <button>Or me</button>
    <span>Clicking here does nothing</span>
</div>
```

The `from <selector/> in me` pattern limits which descendant elements trigger the handler.

#### Adding Logging to Hyperscript

The `log` command outputs to the console:

```html
<!-- Simple logging -->
<button _="on click log 'Clicked!'">Test</button>

<!-- Log variables -->
<button _="on click
             set count to #counter's textContent as Int
             log `Count is ${count}`
             if count > 10
                 log 'Count exceeds threshold'
             end">
    Check Count
</button>

<!-- Log event details -->
<div _="on click
          log event
          log event.target
          log event.clientX
          log event.clientY">
    Click anywhere
</div>
```

For complex debugging, create a verbose handler:

```html
<form _="on submit
           log '=== Form Submit Debug ==='
           log `Target: ${event.target.id}`
           log `Form data: ${new FormData(me)}`
           -- Let the form continue
       ">
```

### 21.6.3 htmx Event Integration

Hyperscript can listen to htmx events, enabling coordination between htmx requests and client-side behavior.

#### Correct Event Name Casing

htmx event names use camelCase after the colon. Getting the case wrong causes silent failures:

**Wrong**: All lowercase
```html
<div _="on htmx:afterswap log 'swapped'">
```

**Wrong**: All caps
```html
<div _="on htmx:AFTERSWAP log 'swapped'">
```

**Correct**: Proper camelCase
```html
<div _="on htmx:afterSwap log 'swapped'">
```

Common htmx events to listen for:
- `htmx:configRequest` - Before request is configured
- `htmx:beforeRequest` - Just before request is sent
- `htmx:afterRequest` - After response received
- `htmx:beforeSwap` - Before content is swapped
- `htmx:afterSwap` - After content is swapped
- `htmx:afterSettle` - After settling (scripts run, etc.)
- `htmx:responseError` - Server returned error status

#### Complete Example: Form with Multiple htmx Event Handlers

This example shows a form with full htmx lifecycle handling in Hyperscript:

```html
<form id="artist-form"
      hx-post="/Artists?handler=Create"
      hx-target="#result"
      _="on htmx:beforeRequest
             -- Disable form during submission
             add @disabled to <input/> in me
             add @disabled to <button/> in me
             add .submitting to me
             set #submit-btn's textContent to 'Saving...'
             log 'Form submission started'
         
         on htmx:afterRequest
             -- Re-enable form
             remove @disabled from <input/> in me
             remove @disabled from <button/> in me
             remove .submitting from me
             set #submit-btn's textContent to 'Save'
             log `Request completed with status: ${event.detail.successful}`
         
         on htmx:responseError
             -- Show error state
             add .error to me
             set #error-message's textContent to 'Failed to save. Please try again.'
             show #error-message
             log `Error: ${event.detail.xhr.status}`
         
         on htmx:afterSwap
             -- Success! Close form or show confirmation
             if event.detail.successful
                 add .success to me
                 wait 1s
                 send closeModal to #modal
             end
             log 'Content swapped'">
    
    <div id="error-message" style="display: none;" class="error-text"></div>
    
    <label>
        Artist Name
        <input type="text" name="name" required />
    </label>
    
    <button type="submit" id="submit-btn">Save</button>
</form>
```

#### Debugging Integration Issues

When Hyperscript doesn't respond to htmx events:

**1. Verify the event fires**

Add a JavaScript listener to confirm htmx fires the event:
```javascript
document.body.addEventListener('htmx:afterSwap', function(e) {
    console.log('htmx:afterSwap fired', e.detail);
});
```

If this logs but Hyperscript doesn't respond, the issue is in Hyperscript.

**2. Check event name spelling and case**

```html
<!-- Add logging to verify Hyperscript receives the event -->
<div _="on htmx:afterSwap log 'Hyperscript received afterSwap'">
```

**3. Verify element receives the event**

htmx events bubble up from the target element. If your Hyperscript is on an unrelated element, it won't receive the event:

```html
<!-- This won't work - #other-div isn't in the event path -->
<div id="other-div" _="on htmx:afterSwap log 'Received'"></div>

<div id="content" hx-get="/load" hx-trigger="click">
    Load Content
</div>
```

Fix by listening on body or a common ancestor:

```html
<body _="on htmx:afterSwap log 'Swap occurred somewhere'">
```

Or target the specific element:

```html
<div id="content" 
     hx-get="/load" 
     hx-trigger="click"
     _="on htmx:afterSwap log 'My content was swapped'">
    Load Content
</div>
```

**4. Check event.detail properties**

Different events provide different details:

```html
<div _="on htmx:afterSwap
          log event.detail.target
          log event.detail.xhr
          log event.detail.successful">
```

Use `log event.detail` to see all available properties:

```html
<div _="on htmx:afterSwap log event.detail">
```

This outputs the entire detail object to the console, showing what data is available.

## 21.7 Debugging Workflow: A Systematic Approach

Random debugging wastes time. Clicking around and changing things hoping something works is inefficient and often introduces new problems. A systematic approach finds root causes faster and produces reliable fixes.

### 21.7.1 The Five-Step Debug Process

This process applies to any htmx issue, from simple attribute typos to complex timing problems.

#### Step 1: Reproduce Consistently

Before debugging, you need a reliable way to trigger the bug.

**Document the exact steps:**
1. Starting state (fresh page load? after specific actions?)
2. User action (what exactly do you click/type?)
3. Expected result (what should happen?)
4. Actual result (what happens instead?)

**Note the environment:**
- Browser and version (Chrome 120, Firefox 121, etc.)
- Development or production?
- Authenticated or anonymous user?
- Any browser extensions that might interfere?

**Check for intermittent behavior:**
- Does it happen every time?
- Only the first time? Only after the first time?
- Only with certain data?

A bug you can't reproduce is a bug you can't fix with confidence. Spend time here until you have reliable reproduction steps.

#### Step 2: Isolate the Layer

htmx applications have multiple layers where problems can occur. Identify which layer contains the bug:

```
┌─────────────────────────────────────────────────────────────┐
│  CLIENT SIDE                                                │
│  ┌─────────────────┐   ┌─────────────────┐                 │
│  │  User Interface │ → │  htmx/Browser   │                 │
│  │  (HTML/CSS)     │   │  (Attributes,   │                 │
│  │                 │   │   Events, DOM)  │                 │
│  └─────────────────┘   └────────┬────────┘                 │
└──────────────────────────────────┼──────────────────────────┘
                                   │ HTTP Request
                                   ▼
┌─────────────────────────────────────────────────────────────┐
│  NETWORK                                                    │
│  (Request headers, Response headers, Status codes)          │
└──────────────────────────────────┬──────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────┐
│  SERVER SIDE                                                │
│  ┌─────────────────┐   ┌─────────────────┐                 │
│  │  Razor Pages    │ → │  Services/      │                 │
│  │  (Handlers,     │   │  Database       │                 │
│  │   Partials)     │   │                 │                 │
│  └─────────────────┘   └─────────────────┘                 │
└─────────────────────────────────────────────────────────────┘
```

**Decision tree for isolation:**

1. **Is a network request made?** (Check Network tab)
   - No → Problem is client-side (htmx attributes, triggers, JavaScript)
   - Yes → Continue to step 2

2. **What's the response status?**
   - 4xx/5xx → Problem is server-side (handler, validation, database)
   - 200 → Continue to step 3

3. **Is the response content correct?**
   - No → Problem is server-side (wrong partial, model issue)
   - Yes → Continue to step 4

4. **Does the content appear in the right place?**
   - No → Problem is client-side (hx-target, hx-swap)
   - Yes → Problem might be CSS or content issue

#### Step 3: Gather Evidence

Once you know which layer to focus on, collect relevant data:

**Client-side evidence:**
- Console errors and warnings
- htmx event log (`htmx.logAll()` output)
- DOM state before and after (Elements tab screenshots)
- Attribute values on triggering element

**Network evidence:**
- Full URL including query parameters
- Request headers (especially HX-* headers)
- Response status code
- Response headers
- Response body content

**Server-side evidence:**
- Application logs
- Debugger output (breakpoint values)
- Database query results
- Exception details if any

Keep notes or screenshots. You'll need to compare before and after states.

#### Step 4: Form and Test Hypothesis

Based on the evidence, form a specific hypothesis about what's wrong.

**Good hypothesis:** "The Cancel button's hx-target points to #artist-row-1, but the Edit swap changed that element's ID to #artist-edit-1"

**Bad hypothesis:** "Something's wrong with htmx"

Test your hypothesis with a minimal change:

- If you think the ID is wrong, change just the ID
- If you think the handler name is wrong, fix just the handler name
- Don't change multiple things at once

If the fix doesn't work, your hypothesis was wrong. Return to Step 3 and gather more evidence.

#### Step 5: Fix and Verify

When you find the fix:

1. **Implement the fix properly** - Don't just patch the symptom
2. **Test the original reproduction steps** - Does the bug still occur?
3. **Test related functionality** - Did you break anything else?
4. **Test edge cases** - Empty data, special characters, rapid clicks

Document what you learned. If this bug could happen elsewhere in the codebase, check those places too.

### 21.7.2 Debug Scenario Walkthrough

Let's walk through a complete debugging session using the Chinook Dashboard.

#### The Bug Report

**Reported behavior:** "When I edit an artist name and click Save, the old name still shows."

#### Step 1: Reproduce Consistently

**Reproduction steps:**
1. Navigate to /Artists
2. Click Edit on any artist (e.g., "AC/DC")
3. Change the name to "AC/DC Updated"
4. Click Save
5. **Expected:** Row shows "AC/DC Updated"
6. **Actual:** Row shows "AC/DC" (original name)

**Environment:** Chrome 120, development mode, localhost:5001

**Consistency:** Happens every time, with any artist.

#### Step 2: Isolate the Layer

**Check 1: Is a request made?**

Open Network tab, filter to Fetch/XHR, reproduce the bug.

```
POST /Artists?handler=Update&id=1    200    text/html    245 B    23ms
```

Yes, request is made. Status 200 means server processed it successfully.

**Check 2: Is the response correct?**

Click the request, view Response tab:

```html
<tr id="artist-row-1">
    <td>AC/DC Updated</td>
    <td>10 albums</td>
    <td>
        <button hx-get="/Artists?handler=Edit&amp;id=1" ...>Edit</button>
    </td>
</tr>
```

The response contains "AC/DC Updated" - the correct, updated name. Server is working correctly.

**Conclusion:** Problem is client-side. The server returns correct data, but it's not displaying.

#### Step 3: Gather Evidence

**htmx event log:**

Enable logging and reproduce:

```javascript
htmx.logAll();
```

```
htmx:configRequest {verb: "post", path: "/Artists?handler=Update&id=1", ...}
htmx:beforeRequest {...}
htmx:afterRequest {successful: true, ...}
htmx:beforeSwap {target: tr#artist-edit-1, ...}
htmx:afterSwap {target: tr#artist-edit-1, ...}
```

Notice: `beforeSwap` shows `target: tr#artist-edit-1`

**DOM inspection before Save:**

Check Elements tab for the edit form:

```html
<tr id="artist-edit-1">
    <form hx-post="/Artists?handler=Update&id=1"
          hx-target="#artist-row-1"
          hx-swap="outerHTML">
        <input name="name" value="AC/DC Updated" />
        <button type="submit">Save</button>
    </form>
</tr>
```

The form's `hx-target` is `#artist-row-1`.

**DOM inspection - search for artist-row-1:**

Search in Elements tab: "artist-row-1" - **Not found**

The edit form replaced `#artist-row-1` with `#artist-edit-1`. The target no longer exists.

#### Step 4: Form and Test Hypothesis

**Hypothesis:** The form targets `#artist-row-1`, but that element doesn't exist (it was replaced by `#artist-edit-1`). htmx can't find the target, so it uses a fallback or fails silently.

**Test:** Check htmx behavior for missing targets.

Looking at htmx documentation: when a target isn't found, htmx falls back to the element that triggered the request (the form itself). But the form is inside `#artist-edit-1`, so the swap replaces content inside the edit row, not the edit row itself.

Let's verify by checking what actually happened:

```html
<!-- After Save, the DOM shows: -->
<tr id="artist-edit-1">
    <tr id="artist-row-1">  <!-- Nested row! Invalid HTML -->
        <td>AC/DC Updated</td>
        ...
    </tr>
</tr>
```

The response was inserted inside the edit row, creating invalid nested table rows. The browser may render this unexpectedly or the original edit form content may still be visible.

**Hypothesis confirmed:** Target mismatch causes incorrect swap location.

#### Step 5: Fix and Verify

**The fix:** Keep the ID consistent, or use a stable target.

**Option A: Same ID throughout**

Edit row partial keeps the original ID:

```html
<!-- _ArtistEditRow.cshtml -->
<tr id="artist-row-@Model.Id">
    <form hx-post="/Artists?handler=Update&id=@Model.Id"
          hx-target="#artist-row-@Model.Id"
          hx-swap="outerHTML">
        ...
    </form>
</tr>
```

**Option B: Use relative target**

```html
<form hx-post="/Artists?handler=Update&id=@Model.Id"
      hx-target="closest tr"
      hx-swap="outerHTML">
```

**Verification:**

1. Edit artist, change name, click Save - ✅ Shows updated name
2. Edit same artist again - ✅ Edit form appears
3. Edit different artist - ✅ Works correctly
4. Cancel edit - ✅ Returns to view mode
5. Rapid edit/save/edit - ✅ No issues

**Root cause documented:** When entering edit mode, the row ID changed from `artist-row-X` to `artist-edit-X`. The save form targeted the original ID, which no longer existed. Fix: maintain consistent IDs or use relative selectors.

### 21.7.3 Creating a Debug Page

A dedicated debug page helps test htmx behavior in isolation. Build one into your development environment.

#### Debug Page Model

**Pages/Debug/Index.cshtml.cs**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ChinookDashboard.Pages.Debug;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IWebHostEnvironment _environment;

    public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public Dictionary<string, string> HtmxHeaders { get; set; } = new();
    public Dictionary<string, string> AllHeaders { get; set; } = new();
    public string RequestInfo { get; set; } = "";

    public IActionResult OnGet()
    {
        // Only allow in development
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        CaptureRequestInfo();
        return Page();
    }

    public IActionResult OnGetEcho(string? message, int delay = 0)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        // Simulate slow response for testing
        if (delay > 0)
        {
            Thread.Sleep(Math.Min(delay, 5000)); // Max 5 seconds
        }

        var response = $@"
            <div class='echo-response'>
                <strong>Echo Response</strong>
                <p>Message: {System.Web.HttpUtility.HtmlEncode(message ?? "(none)")}</p>
                <p>Time: {DateTime.Now:HH:mm:ss.fff}</p>
                <p>Delay: {delay}ms</p>
            </div>";

        return Content(response, "text/html");
    }

    public IActionResult OnGetHeaders()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        CaptureRequestInfo();

        var html = "<div class='headers-response'>";
        html += "<h4>htmx Headers</h4><dl>";
        foreach (var header in HtmxHeaders)
        {
            html += $"<dt>{header.Key}</dt><dd>{System.Web.HttpUtility.HtmlEncode(header.Value)}</dd>";
        }
        html += "</dl>";

        html += "<h4>All Request Headers</h4><dl>";
        foreach (var header in AllHeaders.Take(20))
        {
            html += $"<dt>{header.Key}</dt><dd>{System.Web.HttpUtility.HtmlEncode(header.Value)}</dd>";
        }
        html += "</dl></div>";

        return Content(html, "text/html");
    }

    public IActionResult OnPostFormTest(string? field1, string? field2, int? numericField)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        _logger.LogDebug("Form test received: field1={F1}, field2={F2}, numeric={Num}",
            field1, field2, numericField);

        var html = $@"
            <div class='form-response'>
                <strong>Form Data Received</strong>
                <dl>
                    <dt>field1</dt><dd>{System.Web.HttpUtility.HtmlEncode(field1 ?? "(null)")}</dd>
                    <dt>field2</dt><dd>{System.Web.HttpUtility.HtmlEncode(field2 ?? "(null)")}</dd>
                    <dt>numericField</dt><dd>{numericField?.ToString() ?? "(null)"}</dd>
                </dl>
                <p>Received at: {DateTime.Now:HH:mm:ss.fff}</p>
            </div>";

        return Content(html, "text/html");
    }

    public IActionResult OnGetError(int statusCode = 500)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        return StatusCode(statusCode, $"Simulated {statusCode} error for testing");
    }

    public IActionResult OnGetOobTest()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var html = @"
            <div class='primary-response'>
                <strong>Primary Response</strong>
                <p>This goes to the main target.</p>
            </div>
            <div id='oob-target-1' hx-swap-oob='true'>
                OOB Update 1: Updated at " + DateTime.Now.ToString("HH:mm:ss") + @"
            </div>
            <div id='oob-target-2' hx-swap-oob='innerHTML'>
                OOB Update 2: Content replaced at " + DateTime.Now.ToString("HH:mm:ss") + @"
            </div>";

        return Content(html, "text/html");
    }

    private void CaptureRequestInfo()
    {
        // Capture htmx-specific headers
        var htmxHeaderNames = new[] { "HX-Request", "HX-Target", "HX-Trigger", 
            "HX-Trigger-Name", "HX-Current-URL", "HX-Prompt", "HX-Boosted" };

        foreach (var name in htmxHeaderNames)
        {
            if (Request.Headers.TryGetValue(name, out var value))
            {
                HtmxHeaders[name] = value.ToString();
            }
        }

        // Capture all headers
        foreach (var header in Request.Headers)
        {
            AllHeaders[header.Key] = header.Value.ToString();
        }

        // Request info summary
        RequestInfo = JsonSerializer.Serialize(new
        {
            Method = Request.Method,
            Path = Request.Path.ToString(),
            QueryString = Request.QueryString.ToString(),
            IsHttps = Request.IsHttps,
            ContentType = Request.ContentType,
            ContentLength = Request.ContentLength
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
```

#### Debug Page View

**Pages/Debug/Index.cshtml**

```html
@page
@model ChinookDashboard.Pages.Debug.IndexModel
@{
    ViewData["Title"] = "htmx Debug Console";
}

<style>
    .debug-page {
        font-family: system-ui, -apple-system, sans-serif;
        max-width: 1200px;
        margin: 0 auto;
        padding: 20px;
    }
    .debug-section {
        background: #f8f9fa;
        border: 1px solid #dee2e6;
        border-radius: 8px;
        padding: 20px;
        margin-bottom: 20px;
    }
    .debug-section h2 {
        margin-top: 0;
        padding-bottom: 10px;
        border-bottom: 1px solid #dee2e6;
    }
    .debug-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
        gap: 20px;
    }
    .result-box {
        background: white;
        border: 1px solid #dee2e6;
        border-radius: 4px;
        padding: 15px;
        min-height: 100px;
    }
    .result-box:empty::before {
        content: 'Results will appear here...';
        color: #6c757d;
        font-style: italic;
    }
    .debug-input {
        padding: 8px 12px;
        border: 1px solid #ced4da;
        border-radius: 4px;
        font-size: 14px;
    }
    .debug-btn {
        padding: 8px 16px;
        background: #0d6efd;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 14px;
    }
    .debug-btn:hover {
        background: #0b5ed7;
    }
    .debug-btn.secondary {
        background: #6c757d;
    }
    .debug-btn.danger {
        background: #dc3545;
    }
    .event-log {
        font-family: monospace;
        font-size: 12px;
        background: #1e1e1e;
        color: #d4d4d4;
        padding: 15px;
        border-radius: 4px;
        max-height: 300px;
        overflow-y: auto;
    }
    .event-log .event {
        margin: 2px 0;
        padding: 2px 5px;
    }
    .event-log .event-request { color: #569cd6; }
    .event-log .event-response { color: #4ec9b0; }
    .event-log .event-swap { color: #dcdcaa; }
    .event-log .event-error { color: #f14c4c; }
    .config-display {
        font-family: monospace;
        font-size: 12px;
        background: #f8f9fa;
        padding: 10px;
        border-radius: 4px;
        max-height: 200px;
        overflow-y: auto;
    }
    .form-row {
        display: flex;
        gap: 10px;
        margin-bottom: 10px;
        align-items: center;
    }
    .form-row label {
        min-width: 100px;
    }
    dl {
        display: grid;
        grid-template-columns: auto 1fr;
        gap: 5px 15px;
    }
    dt {
        font-weight: bold;
        color: #495057;
    }
    dd {
        margin: 0;
        word-break: break-all;
    }
    .echo-response, .form-response, .headers-response, .primary-response {
        padding: 10px;
        background: #e7f5ff;
        border-radius: 4px;
    }
    .htmx-request {
        opacity: 0.5;
    }
</style>

<div class="debug-page">
    <h1>🔧 htmx Debug Console</h1>
    <p>Test htmx features and diagnose issues. <strong>Development only.</strong></p>

    <div class="debug-grid">
        <!-- Echo Test -->
        <div class="debug-section">
            <h2>Echo Test</h2>
            <p>Test basic htmx GET requests</p>
            
            <div class="form-row">
                <label for="echo-message">Message:</label>
                <input type="text" 
                       id="echo-message" 
                       name="message"
                       class="debug-input" 
                       placeholder="Type something..."
                       value="Hello htmx!" />
            </div>
            <div class="form-row">
                <label for="echo-delay">Delay (ms):</label>
                <input type="number" 
                       id="echo-delay" 
                       name="delay"
                       class="debug-input" 
                       value="0"
                       min="0"
                       max="5000"
                       style="width: 100px;" />
            </div>
            <div class="form-row">
                <button class="debug-btn"
                        hx-get="/Debug?handler=Echo"
                        hx-include="#echo-message, #echo-delay"
                        hx-target="#echo-result">
                    Send Echo
                </button>
            </div>
            <div id="echo-result" class="result-box"></div>
        </div>

        <!-- Form Test -->
        <div class="debug-section">
            <h2>Form Test</h2>
            <p>Test POST requests with form data</p>
            
            <form hx-post="/Debug?handler=FormTest" hx-target="#form-result">
                <div class="form-row">
                    <label for="field1">Field 1:</label>
                    <input type="text" id="field1" name="field1" class="debug-input" value="Test Value" />
                </div>
                <div class="form-row">
                    <label for="field2">Field 2:</label>
                    <input type="text" id="field2" name="field2" class="debug-input" />
                </div>
                <div class="form-row">
                    <label for="numericField">Number:</label>
                    <input type="number" id="numericField" name="numericField" class="debug-input" value="42" />
                </div>
                <div class="form-row">
                    <button type="submit" class="debug-btn">Submit Form</button>
                </div>
            </form>
            <div id="form-result" class="result-box"></div>
        </div>

        <!-- Headers Inspection -->
        <div class="debug-section">
            <h2>Headers Inspection</h2>
            <p>View htmx request headers sent to server</p>
            
            <div class="form-row">
                <button class="debug-btn"
                        hx-get="/Debug?handler=Headers"
                        hx-target="#headers-result">
                    Inspect Headers
                </button>
            </div>
            <div id="headers-result" class="result-box"></div>
        </div>

        <!-- OOB Test -->
        <div class="debug-section">
            <h2>Out-of-Band Updates</h2>
            <p>Test OOB swap functionality</p>
            
            <div class="form-row">
                <button class="debug-btn"
                        hx-get="/Debug?handler=OobTest"
                        hx-target="#oob-primary">
                    Trigger OOB Update
                </button>
            </div>
            <div id="oob-primary" class="result-box">Primary target</div>
            <div class="form-row" style="margin-top: 10px;">
                <div id="oob-target-1" class="result-box" style="flex: 1;">OOB Target 1</div>
                <div id="oob-target-2" class="result-box" style="flex: 1;">OOB Target 2</div>
            </div>
        </div>

        <!-- Error Simulation -->
        <div class="debug-section">
            <h2>Error Simulation</h2>
            <p>Test error handling</p>
            
            <div class="form-row">
                <button class="debug-btn danger"
                        hx-get="/Debug?handler=Error&statusCode=400"
                        hx-target="#error-result">
                    Trigger 400
                </button>
                <button class="debug-btn danger"
                        hx-get="/Debug?handler=Error&statusCode=404"
                        hx-target="#error-result">
                    Trigger 404
                </button>
                <button class="debug-btn danger"
                        hx-get="/Debug?handler=Error&statusCode=500"
                        hx-target="#error-result">
                    Trigger 500
                </button>
            </div>
            <div id="error-result" class="result-box"></div>
        </div>

        <!-- htmx Config Display -->
        <div class="debug-section">
            <h2>htmx Configuration</h2>
            <p>Current htmx settings</p>
            
            <div class="config-display" id="htmx-config"></div>
            <div class="form-row" style="margin-top: 10px;">
                <button class="debug-btn secondary" onclick="refreshConfig()">
                    Refresh Config
                </button>
                <button class="debug-btn secondary" onclick="htmx.logAll(); alert('Logging enabled. Check console.')">
                    Enable logAll()
                </button>
            </div>
        </div>
    </div>

    <!-- Event Log -->
    <div class="debug-section">
        <h2>Event Log</h2>
        <p>Real-time htmx event monitoring</p>
        
        <div class="form-row">
            <button class="debug-btn secondary" onclick="clearEventLog()">Clear Log</button>
            <label>
                <input type="checkbox" id="log-all-events" checked onchange="toggleEventLogging(this.checked)" />
                Log all events
            </label>
        </div>
        <div id="event-log" class="event-log">
            <div class="event">Event log started...</div>
        </div>
    </div>
</div>

@section Scripts {
    <script>
        // Display htmx config
        function refreshConfig() {
            const configDisplay = document.getElementById('htmx-config');
            configDisplay.textContent = JSON.stringify(htmx.config, null, 2);
        }
        refreshConfig();

        // Event logging
        const eventLog = document.getElementById('event-log');
        let loggingEnabled = true;

        function logEvent(name, detail, className) {
            if (!loggingEnabled) return;
            
            const entry = document.createElement('div');
            entry.className = 'event ' + className;
            
            const time = new Date().toLocaleTimeString('en-US', { 
                hour12: false, 
                hour: '2-digit', 
                minute: '2-digit', 
                second: '2-digit',
                fractionalSecondDigits: 3 
            });
            
            let info = '';
            if (detail.path) info += ` path=${detail.path}`;
            if (detail.target?.id) info += ` target=#${detail.target.id}`;
            if (detail.xhr?.status) info += ` status=${detail.xhr.status}`;
            if (detail.successful !== undefined) info += ` success=${detail.successful}`;
            
            entry.textContent = `[${time}] ${name}${info}`;
            eventLog.appendChild(entry);
            eventLog.scrollTop = eventLog.scrollHeight;
        }

        function clearEventLog() {
            eventLog.innerHTML = '<div class="event">Log cleared...</div>';
        }

        function toggleEventLogging(enabled) {
            loggingEnabled = enabled;
            logEvent(enabled ? 'Logging enabled' : 'Logging disabled', {}, '');
        }

        // htmx event listeners
        const events = [
            { name: 'htmx:configRequest', class: 'event-request' },
            { name: 'htmx:beforeRequest', class: 'event-request' },
            { name: 'htmx:afterRequest', class: 'event-response' },
            { name: 'htmx:beforeSwap', class: 'event-swap' },
            { name: 'htmx:afterSwap', class: 'event-swap' },
            { name: 'htmx:responseError', class: 'event-error' },
            { name: 'htmx:sendError', class: 'event-error' },
            { name: 'htmx:timeout', class: 'event-error' },
        ];

        events.forEach(({ name, class: className }) => {
            document.body.addEventListener(name, (e) => {
                logEvent(name, e.detail, className);
            });
        });

        // Log OOB events
        document.body.addEventListener('htmx:oobAfterSwap', (e) => {
            logEvent('htmx:oobAfterSwap', { target: e.detail.target }, 'event-swap');
        });
    </script>
}
```

---

## 21.8 Production Debugging Considerations

Development debugging with browser DevTools and `console.log` doesn't work in production. You need logging, error reporting, and diagnostic tools that work without direct browser access.

### 21.8.1 Error Logging for htmx Requests

Production errors need structured logging that captures context without exposing sensitive data.

**Middleware/HtmxErrorLoggingMiddleware.cs**

```csharp
using System.Diagnostics;

namespace ChinookDashboard.Middleware;

public class HtmxErrorLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HtmxErrorLoggingMiddleware> _logger;

    public HtmxErrorLoggingMiddleware(RequestDelegate next, ILogger<HtmxErrorLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process htmx requests
        if (!context.Request.Headers.ContainsKey("HX-Request"))
        {
            await _next(context);
            return;
        }

        var requestId = Activity.Current?.Id ?? context.TraceIdentifier;
        var stopwatch = Stopwatch.StartNew();

        // Capture request details before processing
        var requestDetails = new HtmxRequestDetails
        {
            RequestId = requestId,
            Method = context.Request.Method,
            Path = context.Request.Path.ToString(),
            QueryString = SanitizeQueryString(context.Request.QueryString.ToString()),
            Target = context.Request.Headers["HX-Target"].FirstOrDefault(),
            Trigger = context.Request.Headers["HX-Trigger"].FirstOrDefault(),
            CurrentUrl = context.Request.Headers["HX-Current-URL"].FirstOrDefault(),
            UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault()?[..Math.Min(100, context.Request.Headers["User-Agent"].FirstOrDefault()?.Length ?? 0)]
        };

        try
        {
            await _next(context);

            stopwatch.Stop();

            // Log 4xx and 5xx responses
            if (context.Response.StatusCode >= 400)
            {
                _logger.LogWarning(
                    "htmx request failed | RequestId: {RequestId} | Status: {StatusCode} | " +
                    "Method: {Method} | Path: {Path} | Target: {Target} | Duration: {Duration}ms",
                    requestDetails.RequestId,
                    context.Response.StatusCode,
                    requestDetails.Method,
                    requestDetails.Path,
                    requestDetails.Target ?? "(none)",
                    stopwatch.ElapsedMilliseconds
                );
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log exceptions with full context
            _logger.LogError(ex,
                "htmx request exception | RequestId: {RequestId} | Method: {Method} | " +
                "Path: {Path} | Target: {Target} | Trigger: {Trigger} | Duration: {Duration}ms",
                requestDetails.RequestId,
                requestDetails.Method,
                requestDetails.Path,
                requestDetails.Target ?? "(none)",
                requestDetails.Trigger ?? "(none)",
                stopwatch.ElapsedMilliseconds
            );

            throw;
        }
    }

    private static string SanitizeQueryString(string queryString)
    {
        // Remove potentially sensitive parameters
        var sensitiveParams = new[] { "password", "token", "key", "secret", "auth" };
        
        if (string.IsNullOrEmpty(queryString)) return queryString;

        foreach (var param in sensitiveParams)
        {
            // Simple replacement - in production, use proper query string parsing
            queryString = System.Text.RegularExpressions.Regex.Replace(
                queryString,
                $@"({param}=)[^&]*",
                "$1[REDACTED]",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }

        return queryString;
    }

    private class HtmxRequestDetails
    {
        public string RequestId { get; set; } = "";
        public string Method { get; set; } = "";
        public string Path { get; set; } = "";
        public string QueryString { get; set; } = "";
        public string? Target { get; set; }
        public string? Trigger { get; set; }
        public string? CurrentUrl { get; set; }
        public string? UserAgent { get; set; }
    }
}

public static class HtmxErrorLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseHtmxErrorLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HtmxErrorLoggingMiddleware>();
    }
}
```

#### Integration with Serilog

For structured logging with Serilog, the above middleware works as-is. Configure Serilog in `Program.cs`:

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ChinookDashboard")
    .WriteTo.Console()
    .WriteTo.File("logs/htmx-.log", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();
```

### 21.8.2 Client-Side Error Reporting

Capture client-side errors and send them to the server for logging.

**wwwroot/js/error-reporting.js**

```javascript
(function() {
    'use strict';

    // Configuration
    const config = {
        endpoint: '/api/client-errors',
        maxErrorsPerMinute: 10,
        includeStack: false, // Don't send stack traces in production
        sessionId: generateSessionId()
    };

    // Rate limiting
    let errorCount = 0;
    setInterval(() => { errorCount = 0; }, 60000);

    function generateSessionId() {
        return 'sess_' + Math.random().toString(36).substring(2, 15);
    }

    function shouldReport() {
        errorCount++;
        return errorCount <= config.maxErrorsPerMinute;
    }

    function sanitizeUrl(url) {
        if (!url) return url;
        try {
            const parsed = new URL(url, window.location.origin);
            // Remove sensitive query parameters
            ['token', 'key', 'password', 'auth', 'secret'].forEach(param => {
                if (parsed.searchParams.has(param)) {
                    parsed.searchParams.set(param, '[REDACTED]');
                }
            });
            return parsed.pathname + parsed.search;
        } catch {
            return url;
        }
    }

    function reportError(errorData) {
        if (!shouldReport()) {
            console.warn('Error reporting rate limited');
            return;
        }

        // Sanitize data before sending
        const sanitizedData = {
            type: errorData.type,
            message: errorData.message?.substring(0, 500), // Limit message length
            path: sanitizeUrl(errorData.path),
            target: errorData.target,
            status: errorData.status,
            timestamp: new Date().toISOString(),
            sessionId: config.sessionId,
            pageUrl: sanitizeUrl(window.location.href),
            userAgent: navigator.userAgent.substring(0, 200)
        };

        // Send via beacon (doesn't block page unload)
        if (navigator.sendBeacon) {
            navigator.sendBeacon(config.endpoint, JSON.stringify(sanitizedData));
        } else {
            // Fallback to fetch
            fetch(config.endpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(sanitizedData),
                keepalive: true
            }).catch(() => {}); // Ignore send failures
        }
    }

    // htmx error handling
    document.body.addEventListener('htmx:responseError', function(event) {
        reportError({
            type: 'htmx:responseError',
            message: `HTTP ${event.detail.xhr?.status}: ${event.detail.xhr?.statusText}`,
            path: event.detail.pathInfo?.requestPath,
            target: event.detail.target?.id,
            status: event.detail.xhr?.status
        });
    });

    document.body.addEventListener('htmx:sendError', function(event) {
        reportError({
            type: 'htmx:sendError',
            message: 'Network request failed',
            path: event.detail.pathInfo?.requestPath,
            target: event.detail.target?.id
        });
    });

    document.body.addEventListener('htmx:timeout', function(event) {
        reportError({
            type: 'htmx:timeout',
            message: 'Request timed out',
            path: event.detail.pathInfo?.requestPath,
            target: event.detail.target?.id
        });
    });

    // Expose for manual error reporting
    window.reportClientError = reportError;
})();
```

**API Endpoint for Receiving Errors**

```csharp
// In a minimal API or controller
app.MapPost("/api/client-errors", async (
    HttpContext context,
    ILogger<Program> logger) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        
        // Parse and validate
        var error = JsonSerializer.Deserialize<ClientErrorReport>(body);
        if (error == null) return Results.BadRequest();

        // Log with structured data
        logger.LogWarning(
            "Client-side error | Type: {Type} | Message: {Message} | Path: {Path} | " +
            "Status: {Status} | SessionId: {SessionId}",
            error.Type,
            error.Message,
            error.Path,
            error.Status,
            error.SessionId
        );

        return Results.Ok();
    }
    catch
    {
        return Results.BadRequest();
    }
});

public record ClientErrorReport(
    string Type,
    string? Message,
    string? Path,
    string? Target,
    int? Status,
    string? Timestamp,
    string? SessionId,
    string? PageUrl,
    string? UserAgent
);
```

### 21.8.3 Feature Flags for Debug Mode

Control debug features based on environment and configuration.

**Program.cs**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();

// Add feature flags for debugging
builder.Services.Configure<DebugOptions>(
    builder.Configuration.GetSection("Debug"));

var app = builder.Build();

// Environment-specific middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseHtmxLogging(); // Verbose logging in development
    
    // Map debug endpoints only in development
    app.MapRazorPages();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHtmxErrorLogging(); // Error-only logging in production
    
    // Exclude debug pages in production
    app.MapRazorPages().RequireAuthorization();
}

// Configuration class
public class DebugOptions
{
    public bool EnableClientLogging { get; set; }
    public bool EnableVerboseErrors { get; set; }
    public bool EnablePerformanceMetrics { get; set; }
}
```

**Conditional htmx.logAll() in Layout**

```html
@inject IWebHostEnvironment Environment
@inject IOptions<DebugOptions> DebugOptions

<!-- htmx -->
<script src="~/lib/htmx/htmx.min.js"></script>

@if (Environment.IsDevelopment())
{
    <script>
        // Full debug mode in development
        htmx.logAll();
        console.log('%c[htmx] Debug mode enabled', 'color: #3b82f6; font-weight: bold;');
    </script>
}
else if (DebugOptions.Value.EnableClientLogging)
{
    <script>
        // Limited logging in production when enabled via config
        document.body.addEventListener('htmx:responseError', function(e) {
            console.error('htmx error:', e.detail.xhr?.status, e.detail.pathInfo?.requestPath);
        });
    </script>
}

@if (Environment.IsDevelopment() || DebugOptions.Value.EnablePerformanceMetrics)
{
    <script>
        // Performance timing
        document.body.addEventListener('htmx:afterRequest', function(e) {
            const duration = e.detail.xhr?.getResponseHeader('X-Request-Duration');
            if (duration) {
                console.log(`[htmx] ${e.detail.pathInfo?.requestPath}: ${duration}ms`);
            }
        });
    </script>
}
```

**appsettings.Production.json**

```json
{
  "Debug": {
    "EnableClientLogging": false,
    "EnableVerboseErrors": false,
    "EnablePerformanceMetrics": false
  }
}
```

**appsettings.Development.json**

```json
{
  "Debug": {
    "EnableClientLogging": true,
    "EnableVerboseErrors": true,
    "EnablePerformanceMetrics": true
  }
}
```

---

## 21.9 Summary

Debugging htmx applications requires understanding both client-side and server-side behavior. The techniques in this chapter provide a systematic approach to finding and fixing issues.

### Key Debugging Tools

| Tool | Purpose | When to Use |
|------|---------|-------------|
| **Browser Network Tab** | Inspect requests and responses | Always start here for htmx issues |
| **Browser Console** | View errors, run htmx.logAll() | JavaScript errors, event tracing |
| **Browser Elements Tab** | Inspect DOM and htmx attributes | Target issues, attribute verification |
| **Server Logs** | Track server-side processing | Handler execution, database issues |

### Essential htmx Debug Methods

| Method/Event | Purpose | Usage |
|--------------|---------|-------|
| `htmx.logAll()` | Log all htmx events | Development debugging |
| `htmx.config` | View/modify htmx settings | Configuration issues |
| `htmx:responseError` | Handle server errors | Global error handling |
| `htmx:configRequest` | Inspect/modify requests | Request debugging, token injection |
| `htmx:afterSwap` | Post-swap processing | Verify swaps completed |
| `htmx:oobAfterSwap` | OOB swap completion | Debug OOB updates |

### The Systematic Debugging Approach

1. **Reproduce** - Document exact steps to trigger the bug
2. **Isolate** - Determine if issue is client, network, or server
3. **Gather** - Collect evidence from appropriate tools
4. **Hypothesize** - Form specific theory about root cause
5. **Verify** - Test fix and check for regressions

### Common Issues Quick Reference

| Symptom | Likely Cause | Debug Focus |
|---------|--------------|-------------|
| Nothing happens on click | htmx not loaded, JS error, no trigger | Console errors, Network tab |
| 400 Bad Request | Anti-forgery token missing | Request headers, token config |
| 404 Not Found | Wrong handler name, typo | URL in Network tab |
| Content in wrong place | Wrong hx-target selector | Target element existence |
| Duplicate ID issues | Non-unique IDs in loop | Elements tab search |
| OOB not updating | Target ID mismatch | Response HTML, DOM IDs |
| Form data missing | Button outside form | hx-include, form structure |
| Old data after save | Target changed by edit | ID consistency |
| Back button stale | Missing hx-push-url | History events |
| Hyperscript silent fail | Syntax error | _hyperscript.config.debug |

### Companion Code Files

```
chap21/
├── ChinookDashboard/
│   ├── Middleware/
│   │   ├── HtmxLoggingMiddleware.cs
│   │   └── HtmxErrorLoggingMiddleware.cs
│   ├── Extensions/
│   │   └── PartialResultExtensions.cs
│   ├── Pages/
│   │   └── Debug/
│   │       ├── Index.cshtml
│   │       └── Index.cshtml.cs
│   └── wwwroot/
│       └── js/
│           ├── htmx-debug.js
│           └── error-reporting.js
└── README.md
```

### What's Next

Chapter 23 covers testing strategies for htmx applications. You'll learn to write integration tests that verify htmx interactions, test partial responses, and automate the verification of complex multi-step workflows. The debugging skills from this chapter form the foundation for understanding what to test and how to verify correct behavior.
