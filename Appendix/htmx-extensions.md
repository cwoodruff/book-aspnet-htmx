---
order: 3
icon: stack
label: Appendix B - htmx Command Extensions
meta:
title: "htmx Command Extensions"
---

# Appendix B: htmx Extensions Reference

This appendix provides a complete reference for htmx extensions, including official extensions maintained by the htmx team and guidance on creating custom extensions. Use this as a reference when extending htmx functionality in your ASP.NET Core applications.

---

## B.1 Introduction to Extensions

### What Are htmx Extensions?

Extensions add functionality to htmx by hooking into its request/response lifecycle. They can modify requests, transform responses, add new attributes, and change swap behavior.

### Loading Extensions

Extensions can be loaded from CDN or installed via npm:

```html
<!-- From unpkg CDN -->
<script src="https://unpkg.com/htmx.org@1.9.12"></script>
<script src="https://unpkg.com/htmx.org@1.9.12/dist/ext/json-enc.js"></script>

<!-- From cdnjs -->
<script src="https://cdnjs.cloudflare.com/ajax/libs/htmx/1.9.12/htmx.min.js"></script>
<script src="https://cdnjs.cloudflare.com/ajax/libs/htmx/1.9.12/ext/json-enc.min.js"></script>
```

```bash
# npm installation
npm install htmx.org
```

### Enabling Extensions

Use the `hx-ext` attribute to enable extensions:

```html
<!-- Enable for entire body -->
<body hx-ext="json-enc">
    ...
</body>

<!-- Enable multiple extensions -->
<body hx-ext="json-enc, loading-states, response-targets">
    ...
</body>

<!-- Enable for specific section -->
<div hx-ext="preload">
    <a href="/page1" preload>Page 1</a>
    <a href="/page2" preload>Page 2</a>
</div>

<!-- Disable inherited extension -->
<div hx-ext="ignore:json-enc">
    <!-- json-enc disabled here -->
</div>
```

### Extension Inheritance

Extensions enabled on a parent element apply to all descendants:

```html
<body hx-ext="loading-states">
    <!-- All elements inherit loading-states -->
    <div hx-ext="json-enc">
        <!-- This div has both loading-states AND json-enc -->
        <div hx-ext="ignore:loading-states">
            <!-- This div has only json-enc -->
        </div>
    </div>
</body>
```

---

## B.2 Official Extensions

### B.2.1 json-enc

Encodes request bodies as JSON instead of form-urlencoded.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/json-enc.js"></script>
```

**Activation:**
```html
<body hx-ext="json-enc">
```

**Usage:**

```html
<form hx-post="/api/artists" hx-ext="json-enc">
    <input type="text" name="name" value="New Artist" />
    <input type="number" name="rating" value="5" />
    <button type="submit">Create</button>
</form>
```

Request body becomes:
```json
{"name": "New Artist", "rating": "5"}
```

**ASP.NET Core Integration:**

```csharp
public class CreateArtistRequest
{
    public string Name { get; set; } = string.Empty;
    public int Rating { get; set; }
}

public IActionResult OnPost([FromBody] CreateArtistRequest request)
{
    // request.Name and request.Rating are populated from JSON
    var artist = _artistService.Create(request.Name, request.Rating);
    return Partial("_ArtistRow", artist);
}
```

Configure JSON options in `Program.cs`:

```csharp
builder.Services.AddRazorPages()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
```

**Notes:**
- Sets `Content-Type: application/json` automatically
- Nested objects require proper naming: `address.city`, `address.zip`
- Arrays use bracket notation: `tags[0]`, `tags[1]`

---

### B.2.2 client-side-templates

Renders JSON responses using client-side template engines (Mustache, Handlebars, Nunjucks).

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/client-side-templates.js"></script>
<!-- Plus your template engine -->
<script src="https://unpkg.com/mustache@4.2.0"></script>
```

**Activation:**
```html
<body hx-ext="client-side-templates">
```

**Supported Engines:**

| Engine | Attribute | Library |
|--------|-----------|---------|
| Mustache | `mustache-template` | mustache.js |
| Handlebars | `handlebars-template` | handlebars.js |
| Nunjucks | `nunjucks-template` | nunjucks.js |

**Usage with Mustache:**

```html
<div hx-get="/api/artists" 
     hx-trigger="load"
     mustache-template="artist-template">
</div>

<template id="artist-template">
    <ul>
        {{#artists}}
        <li>{{name}} - {{albumCount}} albums</li>
        {{/artists}}
    </ul>
</template>
```

**ASP.NET Core Endpoint:**

```csharp
public IActionResult OnGetApi()
{
    var artists = _artistService.GetAll()
        .Select(a => new { name = a.Name, albumCount = a.Albums.Count });
    
    return new JsonResult(new { artists });
}
```

**Usage with Handlebars:**

```html
<div hx-get="/api/stats" 
     handlebars-template="stats-template">
</div>

<template id="stats-template">
    <div class="stats">
        <p>Total: {{total}}</p>
        {{#if hasMore}}
        <button hx-get="/api/more">Load More</button>
        {{/if}}
    </div>
</template>
```

**Array Rendering:**

```html
<template id="list-template">
    {{#each items}}
    <div class="item">
        <h3>{{this.title}}</h3>
        <p>{{this.description}}</p>
    </div>
    {{/each}}
</template>
```

**Notes:**
- Templates must be in `<template>` tags
- Template ID is referenced without `#`
- Server must return JSON (set `Content-Type: application/json`)

---

### B.2.3 path-deps

Automatically refreshes elements when other requests modify their dependencies.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/path-deps.js"></script>
```

**Activation:**
```html
<body hx-ext="path-deps">
```

**Attributes:**

| Attribute | Description | Example |
|-----------|-------------|---------|
| `path-deps` | Paths this element depends on | `path-deps="/artists"` |

**Usage:**

```html
<!-- This element depends on /artists path -->
<div hx-get="/artists/list" 
     hx-trigger="load"
     path-deps="/artists">
    Artist list loads here
</div>

<!-- When this form posts, it triggers refresh of dependent elements -->
<form hx-post="/artists">
    <input name="name" />
    <button>Add Artist</button>
</form>

<!-- Multiple dependencies -->
<div hx-get="/dashboard/stats" 
     hx-trigger="load"
     path-deps="/artists, /albums, /tracks">
    Dashboard stats
</div>
```

**How It Works:**

1. Element declares dependencies via `path-deps`
2. When any htmx request modifies a dependency path (POST, PUT, PATCH, DELETE)
3. All elements depending on that path automatically refresh

**Server-Side Trigger:**

You can also trigger refreshes via response header:

```csharp
public IActionResult OnPostCreate(string name)
{
    var artist = _artistService.Create(name);
    
    // Trigger path-deps refresh
    Response.Headers.Append("HX-Trigger", "path-deps");
    
    return Partial("_ArtistRow", artist);
}
```

---

### B.2.4 class-tools

Provides advanced class manipulation with timing support.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/class-tools.js"></script>
```

**Activation:**
```html
<body hx-ext="class-tools">
```

**Attributes:**

| Attribute | Description |
|-----------|-------------|
| `classes` | Add/remove classes with timing |
| `toggle` | Toggle classes on events |

**classes Attribute Syntax:**

```
classes="operation:className:timing, ..."
```

Operations:
- `add` - Add class
- `remove` - Remove class
- `toggle` - Toggle class

Timing:
- `100ms` - Milliseconds
- `1s` - Seconds
- `&` - After previous completes

**Examples:**

```html
<!-- Add class after 1 second -->
<div classes="add:highlight:1s">
    Highlights after 1 second
</div>

<!-- Remove class after 500ms -->
<div class="visible" classes="remove:visible:500ms">
    Fades out after 500ms
</div>

<!-- Sequential operations -->
<div classes="add:fade-in:0s, remove:fade-in:1s &, add:complete:0s &">
    Sequential class changes
</div>

<!-- Toggle on interval -->
<div classes="toggle:blink:500ms">
    Blinks every 500ms
</div>
```

**Animation Pattern:**

```html
<style>
.fade-in { animation: fadeIn 0.5s ease-out; }
.fade-out { animation: fadeOut 0.5s ease-out; }
@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
@keyframes fadeOut { from { opacity: 1; } to { opacity: 0; } }
</style>

<div hx-get="/content" 
     hx-target="this"
     hx-swap="innerHTML"
     classes="add:fade-out:0s, remove:fade-out:500ms &, add:fade-in:0s &">
    Content with fade transition
</div>
```

---

### B.2.5 loading-states

Provides enhanced loading state management with fine-grained control.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/loading-states.js"></script>
```

**Activation:**
```html
<body hx-ext="loading-states">
```

**Attributes:**

| Attribute | Description | Example |
|-----------|-------------|---------|
| `data-loading` | Show element during request | `data-loading` |
| `data-loading-class` | Add class during request | `data-loading-class="opacity-50"` |
| `data-loading-class-remove` | Remove class during request | `data-loading-class-remove="hidden"` |
| `data-loading-disable` | Disable element during request | `data-loading-disable` |
| `data-loading-aria-busy` | Set aria-busy during request | `data-loading-aria-busy` |
| `data-loading-delay` | Delay before showing state | `data-loading-delay="200ms"` |
| `data-loading-target` | Scope loading state | `data-loading-target="#form"` |
| `data-loading-path` | Only for specific paths | `data-loading-path="/api/save"` |

**Basic Usage:**

```html
<form hx-post="/save">
    <input name="data" />
    
    <!-- Button shows loading state -->
    <button type="submit">
        <span data-loading-class="hidden">Save</span>
        <span data-loading class="hidden">Saving...</span>
    </button>
</form>
```

**Scoped Loading States:**

```html
<div id="search-section">
    <input hx-get="/search" 
           hx-target="#results"
           hx-trigger="keyup changed delay:300ms" />
    
    <!-- Only shows when search-section has active request -->
    <div data-loading data-loading-target="#search-section" class="hidden">
        Searching...
    </div>
    
    <div id="results"></div>
</div>
```

**Delay to Prevent Flicker:**

```html
<!-- Only show spinner if request takes > 200ms -->
<div data-loading data-loading-delay="200ms" class="hidden">
    <div class="spinner"></div>
</div>
```

**Path-Specific Loading:**

```html
<!-- Only show for /api/heavy endpoint -->
<div data-loading data-loading-path="/api/heavy" class="hidden">
    Processing heavy request...
</div>
```

**Complete Form Example:**

```html
<form hx-post="/api/submit" hx-ext="loading-states">
    <fieldset data-loading-disable>
        <input name="name" required />
        <input name="email" type="email" required />
        
        <button type="submit" data-loading-class="opacity-50">
            <span data-loading-class="hidden">Submit</span>
            <span data-loading class="hidden">
                <svg class="animate-spin h-4 w-4">...</svg>
                Submitting...
            </span>
        </button>
    </fieldset>
    
    <div data-loading data-loading-delay="500ms" class="hidden">
        This is taking longer than expected...
    </div>
</form>
```

---

### B.2.6 preload

Preloads content on mouseenter or focus for faster perceived performance.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/preload.js"></script>
```

**Activation:**
```html
<body hx-ext="preload">
```

**Attributes:**

| Attribute | Values | Description |
|-----------|--------|-------------|
| `preload` | (none) | Preload on mouseenter |
| `preload` | `mousedown` | Preload on mousedown |
| `preload` | `mouseover` | Preload on mouseover (same as default) |

**Basic Usage:**

```html
<!-- Preload on hover -->
<a href="/page" hx-get="/page" hx-target="#content" preload>
    Go to Page
</a>

<!-- Preload on mousedown (faster click response) -->
<button hx-get="/data" hx-target="#output" preload="mousedown">
    Load Data
</button>
```

**Navigation Pattern:**

```html
<nav hx-ext="preload">
    <a href="/dashboard" hx-get="/dashboard" hx-target="#main" hx-push-url="true" preload>
        Dashboard
    </a>
    <a href="/artists" hx-get="/artists" hx-target="#main" hx-push-url="true" preload>
        Artists
    </a>
    <a href="/albums" hx-get="/albums" hx-target="#main" hx-push-url="true" preload>
        Albums
    </a>
</nav>
```

**Configuration via meta tag:**

```html
<meta name="htmx-config" content='{
    "preload": {
        "timeout": 500,
        "images": true,
        "ignoreClass": "no-preload"
    }
}'>
```

**Notes:**
- Preloaded content is cached in memory
- Don't use for content that changes frequently
- Consider server load with many preload links
- Use `preload="mousedown"` for actions that happen on click

---

### B.2.7 remove-me

Automatically removes elements from the DOM after a delay.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/remove-me.js"></script>
```

**Activation:**
```html
<body hx-ext="remove-me">
```

**Attributes:**

| Attribute | Description | Example |
|-----------|-------------|---------|
| `remove-me` | Remove after delay | `remove-me="5s"` |

**Usage:**

```html
<!-- Remove after 5 seconds -->
<div class="notification" remove-me="5s">
    Your changes have been saved!
</div>

<!-- Remove after 2 seconds -->
<div class="toast" remove-me="2s">
    Item deleted
</div>
```

**Toast Notification Pattern:**

Server response includes self-removing toast:

```csharp
public IActionResult OnPostDelete(int id)
{
    _service.Delete(id);
    
    Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new {
        showToast = new { message = "Item deleted", type = "success" }
    }));
    
    return Content("");
}
```

Toast partial (`_Toast.cshtml`):

```html
@model (string Message, string Type)

<div class="toast toast-@Model.Type" 
     remove-me="5s"
     hx-ext="remove-me">
    @Model.Message
</div>
```

**With CSS Animation:**

```html
<style>
.toast {
    animation: slideIn 0.3s ease-out;
}
.toast[removing] {
    animation: slideOut 0.3s ease-out;
}
</style>

<!-- The extension adds [removing] attribute before removal -->
<div class="toast" remove-me="3s">
    Message
</div>
```

---

### B.2.8 response-targets

Specifies different target elements for different HTTP response codes.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/response-targets.js"></script>
```

**Activation:**
```html
<body hx-ext="response-targets">
```

**Attributes:**

| Attribute | Description | Example |
|-----------|-------------|---------|
| `hx-target-*` | Target for status code | `hx-target-404="#not-found"` |
| `hx-target-4*` | Target for 4xx codes | `hx-target-4*="#client-error"` |
| `hx-target-5*` | Target for 5xx codes | `hx-target-5*="#server-error"` |
| `hx-target-error` | Target for any error | `hx-target-error="#error"` |

**Usage:**

```html
<form hx-post="/api/save"
      hx-target="#success-message"
      hx-target-422="#validation-errors"
      hx-target-500="#server-error"
      hx-target-error="#generic-error">
    
    <input name="email" type="email" required />
    <button type="submit">Save</button>
</form>

<div id="success-message"></div>
<div id="validation-errors"></div>
<div id="server-error"></div>
<div id="generic-error"></div>
```

**ASP.NET Core Integration:**

```csharp
public IActionResult OnPost(CreateModel model)
{
    if (!ModelState.IsValid)
    {
        Response.StatusCode = 422; // Unprocessable Entity
        return Partial("_ValidationErrors", ModelState);
    }
    
    try
    {
        var result = _service.Create(model);
        return Partial("_SuccessMessage", result);
    }
    catch (Exception ex)
    {
        Response.StatusCode = 500;
        return Partial("_ServerError", ex.Message);
    }
}
```

**Wildcard Patterns:**

```html
<div hx-get="/api/data"
     hx-target="#content"
     hx-target-4*="#client-error"
     hx-target-5*="#server-error">
    Load
</div>
```

**Complete Error Handling Example:**

```html
<div id="form-container">
    <form hx-post="/api/register"
          hx-target="#form-container"
          hx-target-400="#validation-errors"
          hx-target-409="#conflict-error"
          hx-target-5*="#server-error"
          hx-swap="outerHTML">
        
        <div id="validation-errors"></div>
        <div id="conflict-error"></div>
        
        <input name="username" required />
        <input name="email" type="email" required />
        <button type="submit">Register</button>
    </form>
    
    <div id="server-error" class="hidden"></div>
</div>
```

---

### B.2.9 head-support

Merges `<head>` elements from htmx responses into the document head.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/head-support.js"></script>
```

**Activation:**
```html
<body hx-ext="head-support">
```

**Supported Elements:**
- `<title>` - Updates page title
- `<meta>` - Merges meta tags
- `<link>` - Adds stylesheets
- `<style>` - Adds inline styles
- `<script>` - Executes scripts (with care)

**Usage:**

Server response can include `<head>` elements:

```html
<!-- Response from /artists page -->
<head>
    <title>Artists - Chinook Dashboard</title>
    <meta name="description" content="Manage your artists">
    <link rel="stylesheet" href="/css/artists.css">
</head>

<div id="content">
    <!-- Page content -->
</div>
```

**ASP.NET Core Partial with Head:**

```csharp
// _ArtistsPage.cshtml
@{
    ViewData["Title"] = "Artists";
}

<head>
    <title>@ViewData["Title"] - Chinook Dashboard</title>
</head>

<div class="artists-page">
    @* Page content *@
</div>
```

**Configuration:**

| Option | Default | Description |
|--------|---------|-------------|
| `headSupport.mergeMode` | `merge` | How to handle existing elements |

```html
<meta name="htmx-config" content='{
    "headSupport": {
        "mergeMode": "append"
    }
}'>
```

**Notes:**
- Duplicate prevention: Elements with same key attributes aren't duplicated
- Title always replaces
- Meta tags merge by name/property
- Scripts execute once (tracked by src)

---

### B.2.10 multi-swap

Enables swapping multiple elements from a single response.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/multi-swap.js"></script>
```

**Activation:**
```html
<body hx-ext="multi-swap">
```

**Usage:**

Use `hx-swap="multi:#id1:swapStyle,#id2:swapStyle"`:

```html
<button hx-get="/update-all"
        hx-swap="multi:#header:innerHTML,#content:innerHTML,#footer:innerHTML">
    Update All Sections
</button>
```

**Server Response:**

```html
<div id="header">New header content</div>
<div id="content">New main content</div>
<div id="footer">New footer content</div>
```

**Alternative: Using hx-swap-oob**

For most cases, `hx-swap-oob` is simpler:

```html
<!-- Primary target -->
<button hx-get="/data" hx-target="#main">Load</button>

<!-- Response includes OOB swaps -->
<div id="main">Main content</div>
<div id="sidebar" hx-swap-oob="true">Updated sidebar</div>
<div id="stats" hx-swap-oob="innerHTML">Updated stats</div>
```

---

### B.2.11 morphdom-swap

Uses morphdom for intelligent DOM diffing during swaps.

**Installation:**
```html
<script src="https://unpkg.com/morphdom@2.7.0/dist/morphdom-umd.min.js"></script>
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/morphdom-swap.js"></script>
```

**Activation:**
```html
<body hx-ext="morphdom-swap">
```

**Usage:**

```html
<div hx-get="/content" hx-swap="morph">
    Content that will be morphed
</div>

<!-- Or with innerHTML -->
<div hx-get="/content" hx-swap="morph:innerHTML">
    Content
</div>

<!-- Outer morph (replace element) -->
<div hx-get="/content" hx-swap="morph:outerHTML">
    Content
</div>
```

**Benefits:**
- Preserves focus state in form inputs
- Preserves scroll position
- Smoother transitions for dynamic content
- Preserves video/audio playback state

**When to Use:**
- Real-time updating content
- Forms with many inputs
- Content with embedded media
- Chat interfaces
- Live data displays

**Example - Live Data Table:**

```html
<table hx-get="/live-data" 
       hx-trigger="every 2s"
       hx-swap="morph:innerHTML">
    <tbody>
        <!-- Rows update without losing state -->
    </tbody>
</table>
```

---

### B.2.12 alpine-morph

Uses Alpine.js morph for swaps, preserving Alpine component state.

**Installation:**
```html
<script defer src="https://unpkg.com/alpinejs@3.x.x/dist/cdn.min.js"></script>
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/alpine-morph.js"></script>
```

**Activation:**
```html
<body hx-ext="alpine-morph">
```

**Usage:**

```html
<div x-data="{ open: false }" 
     hx-get="/content" 
     hx-swap="morph">
    
    <button @click="open = !open">Toggle</button>
    
    <div x-show="open">
        <!-- This state preserved during htmx swap -->
    </div>
</div>
```

**Notes:**
- Requires Alpine.js 3.x
- Alpine state (`x-data`) is preserved during swaps
- Useful when combining htmx with Alpine for client-side interactivity

---

### B.2.13 ws (WebSocket)

Enables WebSocket connections for real-time bidirectional communication.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/ws.js"></script>
```

**Activation:**
```html
<div hx-ext="ws">
```

**Attributes:**

| Attribute | Description | Example |
|-----------|-------------|---------|
| `ws-connect` | Connect to WebSocket | `ws-connect="/ws/chat"` |
| `ws-send` | Send form data | `ws-send` |

**Basic Usage:**

```html
<div hx-ext="ws" ws-connect="/ws/chat">
    <div id="messages">
        <!-- Messages appear here -->
    </div>
    
    <form ws-send>
        <input name="message" />
        <button type="submit">Send</button>
    </form>
</div>
```

**Server sends HTML that gets swapped:**

```html
<div id="messages" hx-swap-oob="beforeend">
    <div class="message">Hello from server!</div>
</div>
```

**ASP.NET Core WebSocket Endpoint:**

```csharp
app.UseWebSockets();

app.Map("/ws/chat", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await HandleWebSocket(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

async Task HandleWebSocket(WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    
    while (webSocket.State == WebSocketState.Open)
    {
        var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
        
        if (result.MessageType == WebSocketMessageType.Text)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            
            // Parse form data and create response HTML
            var responseHtml = $@"
                <div id=""messages"" hx-swap-oob=""beforeend"">
                    <div class=""message"">{HttpUtility.HtmlEncode(message)}</div>
                </div>";
            
            var responseBytes = Encoding.UTF8.GetBytes(responseHtml);
            await webSocket.SendAsync(responseBytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
```

**SignalR Integration:**

For more robust real-time features, use SignalR:

```csharp
// Hub
public class ChatHub : Hub
{
    public async Task SendMessage(string message)
    {
        var html = $@"<div id=""messages"" hx-swap-oob=""beforeend"">
            <div class=""message"">{HttpUtility.HtmlEncode(message)}</div>
        </div>";
        
        await Clients.All.SendAsync("ReceiveMessage", html);
    }
}
```

```javascript
// Client
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

connection.on("ReceiveMessage", function(html) {
    htmx.swap("#messages", html, { swapStyle: "beforeend" });
});

connection.start();
```

---

### B.2.14 sse (Server-Sent Events)

Enables Server-Sent Events for server-to-client push.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/sse.js"></script>
```

**Activation:**
```html
<div hx-ext="sse">
```

**Attributes:**

| Attribute | Description | Example |
|-----------|-------------|---------|
| `sse-connect` | Connect to SSE endpoint | `sse-connect="/events"` |
| `sse-swap` | Swap on event | `sse-swap="message"` |

**Basic Usage:**

```html
<div hx-ext="sse" sse-connect="/events">
    <!-- Swapped when 'notifications' event received -->
    <div sse-swap="notifications">
        Notifications appear here
    </div>
    
    <!-- Swapped when 'stats' event received -->
    <div sse-swap="stats">
        Stats appear here
    </div>
</div>
```

**ASP.NET Core SSE Endpoint:**

```csharp
app.MapGet("/events", async context =>
{
    context.Response.Headers.Append("Content-Type", "text/event-stream");
    context.Response.Headers.Append("Cache-Control", "no-cache");
    context.Response.Headers.Append("Connection", "keep-alive");
    
    while (!context.RequestAborted.IsCancellationRequested)
    {
        // Send notification event
        await context.Response.WriteAsync($"event: notifications\n");
        await context.Response.WriteAsync($"data: <div>New notification at {DateTime.Now}</div>\n\n");
        await context.Response.Body.FlushAsync();
        
        await Task.Delay(5000);
    }
});
```

**Multiple Event Types:**

```csharp
app.MapGet("/dashboard-events", async context =>
{
    context.Response.Headers.Append("Content-Type", "text/event-stream");
    
    while (!context.RequestAborted.IsCancellationRequested)
    {
        // Stats event
        var stats = await GetCurrentStats();
        await context.Response.WriteAsync($"event: stats\n");
        await context.Response.WriteAsync($"data: <div class=\"stats\">{stats.Total} items</div>\n\n");
        
        // Notifications event
        var notifications = await GetNotifications();
        if (notifications.Any())
        {
            await context.Response.WriteAsync($"event: notifications\n");
            await context.Response.WriteAsync($"data: <ul>{string.Join("", notifications.Select(n => $"<li>{n}</li>"))}</ul>\n\n");
        }
        
        await context.Response.Body.FlushAsync();
        await Task.Delay(3000);
    }
});
```

**Client HTML:**

```html
<div hx-ext="sse" sse-connect="/dashboard-events">
    <div class="panel">
        <h3>Live Stats</h3>
        <div sse-swap="stats">Loading...</div>
    </div>
    
    <div class="panel">
        <h3>Notifications</h3>
        <div sse-swap="notifications">No notifications</div>
    </div>
</div>
```

---

### B.2.15 debug

Enables debug logging for htmx operations.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/debug.js"></script>
```

**Activation:**
```html
<body hx-ext="debug">
```

**Usage:**

Once enabled, all htmx events are logged to the browser console:

```
htmx:configRequest {elt: button, ...}
htmx:beforeRequest {xhr: XMLHttpRequest, ...}
htmx:afterRequest {xhr: XMLHttpRequest, successful: true, ...}
htmx:beforeSwap {xhr: XMLHttpRequest, target: div#content, ...}
htmx:afterSwap {xhr: XMLHttpRequest, target: div#content, ...}
```

**Scoped Debugging:**

```html
<!-- Only debug this section -->
<div hx-ext="debug">
    <button hx-get="/data">This is logged</button>
</div>

<button hx-get="/other">This is not logged</button>
```

**Custom Logger:**

```javascript
htmx.logger = function(elt, event, data) {
    if (console) {
        console.log(`[htmx] ${event}`, { element: elt, data: data });
    }
};
```

---

### B.2.16 event-header

Includes the triggering event in request headers.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/event-header.js"></script>
```

**Activation:**
```html
<body hx-ext="event-header">
```

**Headers Added:**

| Header | Content |
|--------|---------|
| `Triggering-Event` | JSON of the triggering event |

**Usage:**

```html
<button hx-get="/action" hx-ext="event-header">
    Click Me
</button>
```

Server receives header:
```
Triggering-Event: {"type":"click","target":"button","x":123,"y":456}
```

**ASP.NET Core:**

```csharp
public IActionResult OnGet()
{
    var eventJson = Request.Headers["Triggering-Event"].FirstOrDefault();
    if (!string.IsNullOrEmpty(eventJson))
    {
        var evt = JsonSerializer.Deserialize<TriggeringEvent>(eventJson);
        // Use event information
    }
    
    return Partial("_Content");
}
```

---

### B.2.17 restored

Adds the `restored` event and class when content is restored from history.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/restored.js"></script>
```

**Activation:**
```html
<body hx-ext="restored">
```

**Behavior:**
- Adds `.restored` class to restored elements
- Fires `htmx:restored` event

**Usage:**

```css
/* Style restored content differently */
.restored {
    animation: highlight 1s ease-out;
}
```

```javascript
document.body.addEventListener('htmx:restored', function(event) {
    // Re-initialize components after history restore
    initializeComponents(event.detail.elt);
});
```

---

### B.2.18 disable-element

Disables specified elements during htmx requests.

**Installation:**
```html
<script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/disable-element.js"></script>
```

**Activation:**
```html
<body hx-ext="disable-element">
```

**Attributes:**

| Attribute | Description | Example |
|-----------|-------------|---------|
| `hx-disable-element` | Element(s) to disable | `hx-disable-element="#submit-btn"` |

**Usage:**

```html
<form hx-post="/save" hx-disable-element="#submit-btn, #cancel-btn">
    <input name="data" />
    <button id="submit-btn" type="submit">Save</button>
    <button id="cancel-btn" type="button">Cancel</button>
</form>
```

**Self-Disable:**

```html
<button hx-post="/action" hx-disable-element="this">
    Click Me
</button>
```

---

## B.3 Creating Custom Extensions

### Extension API

```javascript
htmx.defineExtension('my-extension', {
    // Called when extension is initialized
    init: function(api) {
        // api provides htmx internals
    },
    
    // Called on htmx events
    onEvent: function(name, event) {
        // name: event name (e.g., 'htmx:configRequest')
        // event: the event object
        // Return false to prevent default handling
    },
    
    // Transform response before processing
    transformResponse: function(text, xhr, elt) {
        // Return modified text
        return text;
    },
    
    // Check if this is a special swap style
    isInlineSwap: function(swapStyle) {
        return swapStyle === 'my-swap';
    },
    
    // Handle custom swap
    handleSwap: function(swapStyle, target, fragment, settleInfo) {
        // Perform swap
        // Return true if handled
    },
    
    // Encode parameters
    encodeParameters: function(xhr, parameters, elt) {
        // Modify parameters or xhr
        // Return null to use default encoding
    }
});
```

### Example: Request Timing Extension

```javascript
htmx.defineExtension('request-timing', {
    onEvent: function(name, event) {
        if (name === 'htmx:beforeRequest') {
            event.detail.elt.dataset.requestStart = Date.now();
        }
        
        if (name === 'htmx:afterRequest') {
            const start = parseInt(event.detail.elt.dataset.requestStart);
            const duration = Date.now() - start;
            
            console.log(`Request took ${duration}ms`);
            
            // Add timing to element
            event.detail.elt.dataset.lastRequestTime = duration;
            
            // Dispatch custom event
            event.detail.elt.dispatchEvent(new CustomEvent('requestTimed', {
                detail: { duration }
            }));
        }
    }
});
```

### Example: Request Retry Extension

```javascript
htmx.defineExtension('auto-retry', {
    init: function() {
        this.retryCount = new Map();
        this.maxRetries = 3;
        this.retryDelay = 1000;
    },
    
    onEvent: function(name, event) {
        if (name === 'htmx:responseError' || name === 'htmx:sendError') {
            const elt = event.detail.elt;
            const path = event.detail.requestConfig?.path;
            const key = `${elt.id || 'anon'}-${path}`;
            
            const count = (this.retryCount.get(key) || 0) + 1;
            this.retryCount.set(key, count);
            
            if (count <= this.maxRetries) {
                console.log(`Retrying request (${count}/${this.maxRetries})...`);
                
                setTimeout(() => {
                    htmx.trigger(elt, 'htmx:trigger');
                }, this.retryDelay * count);
                
                return false; // Prevent default error handling
            } else {
                this.retryCount.delete(key);
            }
        }
        
        if (name === 'htmx:afterRequest' && event.detail.successful) {
            // Clear retry count on success
            const elt = event.detail.elt;
            const path = event.detail.requestConfig?.path;
            const key = `${elt.id || 'anon'}-${path}`;
            this.retryCount.delete(key);
        }
    }
});
```

### Example: Offline Support Extension

```javascript
htmx.defineExtension('offline-support', {
    init: function() {
        this.queue = [];
        
        window.addEventListener('online', () => {
            this.processQueue();
        });
    },
    
    onEvent: function(name, event) {
        if (name === 'htmx:configRequest') {
            if (!navigator.onLine) {
                // Queue the request
                this.queue.push({
                    method: event.detail.verb,
                    path: event.detail.path,
                    parameters: event.detail.parameters,
                    elt: event.detail.elt
                });
                
                // Show offline message
                event.detail.elt.dispatchEvent(new CustomEvent('offlineQueued'));
                
                return false; // Cancel the request
            }
        }
    },
    
    processQueue: function() {
        while (this.queue.length > 0) {
            const request = this.queue.shift();
            htmx.ajax(request.method, request.path, {
                values: request.parameters
            });
        }
    }
});
```

---

## B.4 Extension Compatibility

### Browser Support

All official extensions support:
- Chrome 60+
- Firefox 55+
- Safari 12+
- Edge 79+

### htmx Version Requirements

| Extension | Minimum htmx Version |
|-----------|---------------------|
| json-enc | 1.0.0 |
| client-side-templates | 1.0.0 |
| path-deps | 1.0.0 |
| class-tools | 1.0.0 |
| loading-states | 1.8.0 |
| preload | 1.0.0 |
| remove-me | 1.0.0 |
| response-targets | 1.8.0 |
| head-support | 1.7.0 |
| multi-swap | 1.9.0 |
| morphdom-swap | 1.0.0 |
| alpine-morph | 1.9.0 |
| ws | 1.0.0 |
| sse | 1.0.0 |
| debug | 1.0.0 |

### Known Conflicts

| Extensions | Issue | Solution |
|------------|-------|----------|
| morphdom-swap + alpine-morph | Both handle swap | Use only one |
| json-enc + multipart forms | Encoding conflict | Disable json-enc for file uploads |
| loading-states + custom indicators | May double-show | Use one approach |

---

## Quick Reference

### Most-Used Extensions

| Extension | Purpose | Key Attribute |
|-----------|---------|---------------|
| `json-enc` | JSON request bodies | (automatic) |
| `response-targets` | Error handling | `hx-target-*` |
| `loading-states` | Loading indicators | `data-loading` |
| `preload` | Preload on hover | `preload` |
| `head-support` | Update page head | (automatic) |

### Extension Loading Template

```html
<!DOCTYPE html>
<html>
<head>
    <title>htmx App</title>
</head>
<body hx-ext="json-enc, loading-states, response-targets">
    
    <!-- Your content -->
    
    <!-- Core htmx -->
    <script src="https://unpkg.com/htmx.org@1.9.10"></script>
    
    <!-- Extensions -->
    <script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/json-enc.js"></script>
    <script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/loading-states.js"></script>
    <script src="https://unpkg.com/htmx.org@1.9.10/dist/ext/response-targets.js"></script>
</body>
</html>
```

### Error Handling Pattern

```html
<form hx-post="/api/save"
      hx-ext="json-enc, response-targets, loading-states"
      hx-target="#result"
      hx-target-422="#validation-errors"
      hx-target-5*="#server-error">
    
    <div id="validation-errors"></div>
    
    <input name="data" data-loading-disable />
    
    <button type="submit">
        <span data-loading-class="hidden">Save</span>
        <span data-loading class="hidden">Saving...</span>
    </button>
</form>

<div id="result"></div>
<div id="server-error"></div>
```

---

*For detailed implementation examples, refer to the relevant chapters in the main text.*
