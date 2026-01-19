---
order: 22
icon: stack
label: Chap 9 - Integrating Hyperscript for Complex Logic
meta:
title: "Integrating Hyperscript for Complex Logic"
---

# Integrating Hyperscript for Complex Logic

htmx handles most interactions beautifully. Click a button, fetch content, swap it into the page. But some interactions need client-side logic that htmx alone cannot provide: toggling classes based on conditions, waiting for animations to complete, orchestrating multi-step sequences, or managing temporary UI state. You have two choices: write JavaScript or use Hyperscript.

Hyperscript is a scripting language built for HTML. It lives in your markup using the `_` attribute, reads like English, and integrates tightly with htmx events. For many interactions, it provides a cleaner alternative to JavaScript event listeners and DOM manipulation.

This chapter teaches you when Hyperscript makes sense, when htmx alone suffices, and how to combine both for sophisticated interactions. You will learn the syntax, see complete working examples with Razor Pages, and understand the trade-offs involved.

## Installing Hyperscript

Add Hyperscript to your `_Layout.cshtml` after htmx:

```html
<script src="https://unpkg.com/htmx.org@2.0.4"></script>
<script src="https://unpkg.com/hyperscript.org@0.9.12"></script>
```

Or install locally:

```bash
npm install hyperscript.org
```

Then reference from your wwwroot:

```html
<script src="~/lib/hyperscript.org/dist/_hyperscript.min.js"></script>
```

Hyperscript uses the `_` attribute (underscore) for its code. This keeps it visually distinct from htmx's `hx-` attributes.

## Hyperscript Syntax Basics

Hyperscript reads like English. Here are the fundamental patterns:

### Event Handling

```html
<!-- Single event -->
<button _="on click add .active to me">
    Click Me
</button>

<!-- Multiple events -->
<div _="on mouseenter add .hover to me
        on mouseleave remove .hover from me">
    Hover Area
</div>
```

### Class Manipulation

```html
<!-- Add class -->
<button _="on click add .loading to me">Start</button>

<!-- Remove class -->
<button _="on click remove .hidden from #panel">Show</button>

<!-- Toggle class -->
<button _="on click toggle .open on #menu">Toggle Menu</button>

<!-- Add to another element -->
<button _="on click add .highlight to #target">Highlight Target</button>
```

### Waiting and Timing

```html
<!-- Wait a duration -->
<button _="on click 
           add .processing to me
           wait 2s
           remove .processing from me">
    Process
</button>

<!-- Wait for an event -->
<button _="on click 
           trigger loadContent on #content
           wait for htmx:afterSwap from #content
           add .loaded to #content">
    Load
</button>
```

### Conditionals

```html
<!-- If statement -->
<button _="on click
           if I match .active
               remove .active from me
           else
               add .active to me
           end">
    Toggle State
</button>

<!-- Confirm dialog -->
<button _="on click
           if confirm('Are you sure?')
               trigger delete on #form
           end">
    Delete
</button>
```

### Targeting Elements

```html
<!-- me/I refers to current element -->
<button _="on click add .clicked to me">Click</button>

<!-- CSS selectors -->
<button _="on click add .open to #modal">Open Modal</button>

<!-- Relative selectors -->
<button _="on click remove .error from closest .form-group">Clear Error</button>

<!-- Multiple targets -->
<button _="on click add .hidden to .notification">Hide All</button>
```

### Triggering Events

```html
<!-- Trigger on another element -->
<button _="on click trigger submit on #myForm">Submit Form</button>

<!-- Send custom event -->
<button _="on click send refreshData to #dashboard">Refresh</button>

<!-- Trigger htmx request -->
<button _="on click trigger click on #hidden-htmx-button">Load via htmx</button>
```

## When to Use Hyperscript vs htmx Alone

Before reaching for Hyperscript, check if htmx already provides what you need.

### Use htmx Alone For:

**Confirmation dialogs** - htmx has `hx-confirm`:

```html
<!-- Don't need Hyperscript for this -->
<button hx-delete="/Items?handler=Remove&amp;id=5"
        hx-target="closest .item"
        hx-swap="outerHTML"
        hx-confirm="Delete this item?">
    Delete
</button>
```

**Triggering requests from other elements** - Use `hx-trigger` with `from:`:

```html
<!-- Don't need Hyperscript for this -->
<div hx-get="/Content?handler=Load"
     hx-trigger="load, customEvent from:body"
     hx-target="this">
    Content loads on page load and when customEvent fires
</div>
```

**Refreshing elements after actions** - Use `HX-Trigger` response header:

```csharp
public IActionResult OnPostCreate()
{
    _service.Create();
    Response.Headers.Append("HX-Trigger", "itemCreated");
    return Content("<div>Created!</div>", "text/html");
}
```

```html
<!-- List auto-refreshes when itemCreated event fires -->
<div hx-get="/Items?handler=List"
     hx-trigger="load, itemCreated from:body"
     hx-target="this">
</div>
```

### Use Hyperscript For:

**Class toggling with conditions:**

```html
<button _="on click
           if I match .active
               remove .active from me
               put 'Enable' into me
           else
               add .active to me
               put 'Disable' into me
           end">
    Enable
</button>
```

**Sequenced animations:**

```html
<button _="on click
           add .fade-out to #old-content
           wait 300ms
           trigger loadNew on #content-area
           wait for htmx:afterSwap from #content-area
           add .fade-in to #content-area">
    Transition Content
</button>
```

**Temporary UI state:**

```html
<button _="on click
           add .copied to me
           writeText('Copied!') into navigator.clipboard
           wait 2s
           remove .copied from me">
    Copy to Clipboard
</button>
```

**Multi-step user flows:**

```html
<button _="on click
           if #email.value is ''
               add .error to #email
               exit
           end
           add .loading to me
           trigger submit on #form">
    Submit
</button>
```

## Integrating Hyperscript with htmx Events

htmx fires events throughout the request lifecycle. Hyperscript can listen for these events and respond accordingly.

### Key htmx Events

| Event | When It Fires |
|-------|---------------|
| `htmx:beforeRequest` | Before request is sent |
| `htmx:afterRequest` | After request completes |
| `htmx:beforeSwap` | Before content is swapped |
| `htmx:afterSwap` | After content is swapped |
| `htmx:afterSettle` | After CSS transitions complete |
| `htmx:responseError` | When server returns error |

### Responding to htmx Events

```html
<form hx-post="/Contact?handler=Submit" 
      hx-target="#result"
      _="on htmx:beforeRequest add .submitting to me
         on htmx:afterRequest remove .submitting from me">
    @Html.AntiForgeryToken()
    <input type="text" name="message" required />
    <button type="submit">Send</button>
</form>
<div id="result"></div>
```

### Triggering htmx from Hyperscript

Sometimes you need Hyperscript to initiate an htmx request. The cleanest approach triggers a click on an htmx-enabled element:

```html
<!-- Hyperscript triggers the htmx request -->
<button _="on click
           if confirm('Load fresh data?')
               trigger click on #refresh-btn
           end">
    Confirm and Refresh
</button>

<!-- htmx handles the actual request -->
<button id="refresh-btn" 
        hx-get="/Dashboard?handler=Refresh"
        hx-target="#dashboard"
        style="display: none">
</button>

<div id="dashboard">Dashboard content</div>
```

### Chaining Actions After htmx Completes

```html
<button hx-get="/Notifications?handler=List"
        hx-target="#notifications"
        hx-swap="innerHTML"
        _="on htmx:afterSwap 
           wait 100ms
           add .loaded to #notifications
           send notificationsLoaded to body">
    Load Notifications
</button>

<div id="notifications"></div>

<!-- Another element responds to the custom event -->
<div _="on notificationsLoaded from body
        put 'Updated just now' into me">
    Last updated: Never
</div>
```

## Practical Examples

### Tooltip System

A tooltip that appears on hover with smooth transitions:

```html
<span class="has-tooltip"
      _="on mouseenter
           add .visible to next .tooltip
         on mouseleave
           remove .visible from next .tooltip">
    Hover over me
    <span class="tooltip">This is the tooltip content</span>
</span>
```

```css
.tooltip {
    position: absolute;
    background: #333;
    color: white;
    padding: 0.5rem 1rem;
    border-radius: 4px;
    opacity: 0;
    transition: opacity 0.2s ease;
    pointer-events: none;
}

.tooltip.visible {
    opacity: 1;
}
```

### Modal Dialog

A modal that opens, loads content via htmx, and closes on completion:

```html
@page
@model ModalDemoModel

<!-- Open button -->
<button _="on click add .open to #modal">
    View Product Details
</button>

<!-- Modal structure -->
<div id="modal" class="modal">
    <div class="modal-backdrop" _="on click remove .open from #modal"></div>
    <div class="modal-content">
        <button class="modal-close" _="on click remove .open from #modal">X</button>
        <div id="modal-body"
             hx-get="/ModalDemo?handler=ProductDetails&amp;id=42"
             hx-trigger="load"
             hx-target="this">
            Loading...
        </div>
    </div>
</div>
```

```csharp
public class ModalDemoModel : PageModel
{
    private readonly IProductService _productService;

    public ModalDemoModel(IProductService productService)
    {
        _productService = productService;
    }

    public void OnGet() { }

    public IActionResult OnGetProductDetails(int id)
    {
        var product = _productService.GetById(id);
        if (product == null) return NotFound();
        return Partial("_ProductDetails", product);
    }
}
```

**Pages/Shared/_ProductDetails.cshtml:**

```html
@model Product

<h2>@Model.Name</h2>
<p>@Model.Description</p>
<p><strong>Price:</strong> @Model.Price.ToString("C")</p>

<form hx-post="/ModalDemo?handler=AddToCart"
      hx-target="#cart-count"
      _="on htmx:afterRequest remove .open from #modal">
    @Html.AntiForgeryToken()
    <input type="hidden" name="productId" value="@Model.Id" />
    <button type="submit">Add to Cart</button>
</form>
```

```css
.modal {
    display: none;
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    z-index: 1000;
}

.modal.open {
    display: flex;
    align-items: center;
    justify-content: center;
}

.modal-backdrop {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0, 0, 0, 0.5);
}

.modal-content {
    position: relative;
    background: white;
    padding: 2rem;
    border-radius: 8px;
    max-width: 500px;
    width: 90%;
}
```

### Accordion Component

An accordion that allows only one section open at a time:

```html
<div class="accordion">
    <div class="accordion-item">
        <button class="accordion-header"
                _="on click
                   if next .accordion-body matches .open
                       remove .open from next .accordion-body
                   else
                       remove .open from .accordion-body in closest .accordion
                       add .open to next .accordion-body
                   end">
            Section 1
        </button>
        <div class="accordion-body">
            <p>Content for section 1</p>
        </div>
    </div>
    
    <div class="accordion-item">
        <button class="accordion-header"
                _="on click
                   if next .accordion-body matches .open
                       remove .open from next .accordion-body
                   else
                       remove .open from .accordion-body in closest .accordion
                       add .open to next .accordion-body
                   end">
            Section 2
        </button>
        <div class="accordion-body">
            <p>Content for section 2</p>
        </div>
    </div>
</div>
```

```css
.accordion-body {
    max-height: 0;
    overflow: hidden;
    transition: max-height 0.3s ease;
}

.accordion-body.open {
    max-height: 500px;
}
```

### Form with Client-Side Validation

Validate before submitting, show loading state, handle errors:

```html
@page
@model ContactModel

<form id="contact-form"
      hx-post="/Contact?handler=Submit"
      hx-target="#form-result"
      _="on submit
           if #email.value is ''
               halt the event
               add .error to #email
               put 'Email is required' into #email-error
               exit
           end
           if not #email.value contains '@'
               halt the event
               add .error to #email
               put 'Enter a valid email' into #email-error
               exit
           end
           remove .error from #email
           put '' into #email-error
         on htmx:beforeRequest
           add .submitting to me
         on htmx:afterRequest
           remove .submitting from me">
    @Html.AntiForgeryToken()
    
    <div class="form-group">
        <label for="email">Email</label>
        <input type="email" id="email" name="email" 
               _="on input remove .error from me" />
        <span id="email-error" class="error-message"></span>
    </div>
    
    <div class="form-group">
        <label for="message">Message</label>
        <textarea id="message" name="message" required></textarea>
    </div>
    
    <button type="submit">
        <span class="normal-text">Send Message</span>
        <span class="loading-text">Sending...</span>
    </button>
</form>

<div id="form-result"></div>
```

```css
.form-group input.error {
    border-color: red;
}

.error-message {
    color: red;
    font-size: 0.875rem;
}

form.submitting button .normal-text {
    display: none;
}

form.submitting button .loading-text {
    display: inline;
}

button .loading-text {
    display: none;
}
```

```csharp
public class ContactModel : PageModel
{
    private readonly IEmailService _emailService;

    public ContactModel(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostSubmit(string email, string message)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
        {
            Response.StatusCode = 400;
            return Content("<div class=\"error\">All fields are required</div>", "text/html");
        }

        await _emailService.SendAsync(email, message);
        return Content("<div class=\"success\">Message sent! We will respond shortly.</div>", "text/html");
    }
}
```

### Dashboard with Refresh Controls

A dashboard with manual refresh and auto-refresh toggle:

```html
@page
@model DashboardModel

<div class="dashboard-controls">
    <button hx-get="/Dashboard?handler=Stats"
            hx-target="#stats"
            hx-indicator="#refresh-indicator">
        Refresh Now
    </button>
    
    <label>
        <input type="checkbox" id="auto-refresh"
               _="on change
                  if I am checked
                      set @hx-trigger of #stats to 'load, every 30s'
                      call htmx.process(#stats)
                  else
                      set @hx-trigger of #stats to 'load'
                      call htmx.process(#stats)
                  end" />
        Auto-refresh every 30s
    </label>
    
    <span id="refresh-indicator" class="htmx-indicator">Refreshing...</span>
</div>

<div id="stats"
     hx-get="/Dashboard?handler=Stats"
     hx-trigger="load"
     hx-target="this">
    Loading stats...
</div>
```

```csharp
public class DashboardModel : PageModel
{
    private readonly IStatsService _statsService;

    public DashboardModel(IStatsService statsService)
    {
        _statsService = statsService;
    }

    public void OnGet() { }

    public IActionResult OnGetStats()
    {
        var stats = _statsService.GetCurrent();
        return Partial("_DashboardStats", stats);
    }
}
```

### Copy to Clipboard

A button that copies text and shows feedback:

```html
<div class="code-block">
    <pre id="code-content">npm install htmx.org</pre>
    <button class="copy-btn"
            _="on click
               writeText(#code-content.innerText) on navigator.clipboard
               add .copied to me
               wait 2s
               remove .copied from me">
        <span class="copy-text">Copy</span>
        <span class="copied-text">Copied!</span>
    </button>
</div>
```

```css
.copy-btn .copied-text {
    display: none;
}

.copy-btn.copied .copy-text {
    display: none;
}

.copy-btn.copied .copied-text {
    display: inline;
}

.copy-btn.copied {
    background-color: #28a745;
}
```

## Debugging Hyperscript

### Enable Debug Mode

Add this script to see verbose output in the console:

```html
<script>
_hyperscript.config.debug = true;
</script>
```

### Common Issues

**Script not running**

- Verify Hyperscript is loaded after htmx
- Check that the `_` attribute is on the correct element
- Look for syntax errors in the browser console

**Event not firing**

- Confirm the event name is correct (htmx events use colons: `htmx:afterSwap`)
- Check that the source element exists when the event fires
- Use `from:` to listen for events bubbling from child elements

**Element not found**

- Verify the selector is correct
- Ensure the target element exists in the DOM
- Use browser dev tools to test the selector

**Action not completing**

- Check for `exit` statements that might be stopping execution
- Verify conditionals are properly closed with `end`
- Add `log` statements to trace execution: `_="on click log 'clicked' then ..."`

### Debugging with Log

```html
<button _="on click
           log 'Button clicked'
           log me
           add .active to me
           log 'Class added'">
    Debug Me
</button>
```

The `log` command outputs to the browser console.

## Performance Considerations

Hyperscript is lightweight (about 15KB minified), but keep these points in mind:

**Avoid heavy computation** - Hyperscript is interpreted at runtime. Complex logic should live in JavaScript or on the server.

**Minimize DOM queries** - Cache references when performing multiple operations on the same element.

**Use htmx for network requests** - Hyperscript can make HTTP requests, but htmx handles them better with swapping, indicators, and error handling.

**Keep scripts focused** - If a Hyperscript block exceeds 10-15 lines, consider moving the logic to JavaScript.

## Summary

Hyperscript provides a readable, English-like syntax for client-side interactions that live directly in your markup:

- **Installation**: Add the script after htmx in your layout
- **Syntax**: Uses `_` attribute with English-like commands
- **Class manipulation**: `add`, `remove`, `toggle` classes on elements
- **Event handling**: `on click`, `on mouseenter`, `on htmx:afterSwap`
- **Timing**: `wait 500ms`, `wait for eventName`
- **Conditionals**: `if ... else ... end`
- **htmx integration**: Listen for htmx events, trigger htmx requests

Use Hyperscript for UI state and sequencing. Use htmx alone when its built-in features (`hx-confirm`, `hx-trigger`, `HX-Trigger` header) suffice. The combination gives you sophisticated interactions without JavaScript frameworks.

## Preview of Next Chapter

Chapter 10 covers URL management with `hx-push-url` and `hx-replace-url`. You will learn how to update the browser's address bar during htmx navigation, enable back-button support, and create deep-linkable components that maintain state across page loads.
