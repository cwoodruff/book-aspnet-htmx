---
order: 28
icon: stack
label: Chap 3 - First Steps with htmx
meta:
title: "First Steps with htmx"
---

# First Steps with htmx

The way we build web applications is changing. Client-side JavaScript frameworks dominate modern development, yet they often introduce complexity that many projects do not need. State management, bundlers, elaborate frontend tooling: these concerns consume development time and add maintenance burden. htmx takes a different path. It enhances HTML's native capabilities, allowing you to craft interactive experiences with minimal JavaScript. This chapter guides you through integrating htmx with ASP.NET Core Razor Pages to build dynamic web applications without the overhead of complex frontend frameworks.

By focusing on server-driven interactions, htmx makes it possible to create fluid user experiences while keeping your application logic where it belongs: on the server. It uses declarative attributes to handle AJAX requests, event-driven interactions, and form submissions. Your markup stays clean and understandable. When combined with Razor Pages, htmx offers a way to blend server-side rendering with targeted interactivity, making your applications efficient and maintainable.

This chapter takes you from fundamental principles to fully functional Razor Pages. You will learn to dynamically fetch, update, and submit content without full page reloads. We cover everything from using `hx-get` and `hx-post` for data retrieval and submission to debugging common issues and fine-tuning request behaviors. By the end, you will be ready to build interactive web applications that feel modern while remaining easy to maintain.

## Adding htmx to Your Razor Pages Project

Before writing any htmx code, you need to include the library in your project. The simplest approach is adding a script tag to your layout file.

Open `Pages/Shared/_Layout.cshtml` and add the htmx script before the closing `</body>` tag:

```html
    <!-- Other content -->
    
    <script src="https://unpkg.com/htmx.org@2.0.4"></script>
</body>
</html>
```

Alternatively, you can download htmx and serve it locally:

```html
<script src="~/js/htmx.min.js"></script>
```

With htmx loaded, every page in your application can use htmx attributes. No build step required. No npm packages to manage. Just one script tag and you are ready to go.

## Understanding the Basics of htmx in Razor Pages

htmx allows you to send AJAX requests using simple HTML attributes. The two most fundamental ones are:

- `hx-get`: Makes an HTTP GET request to fetch content from the server
- `hx-post`: Sends a POST request to submit data to the server

Here is a basic example:

```html
<button hx-get="/Index?handler=Hello" hx-target="#message">Click me!</button>
<div id="message"></div>
```

When the button is clicked, htmx sends an asynchronous GET request to `/Index?handler=Hello`. The server processes the request and returns HTML. htmx then injects that HTML into the `#message` div. No page reload. No JavaScript to write. The interaction happens through HTML attributes alone.

### The Handler Naming Convention

In Razor Pages, the `handler` query parameter maps to methods in your PageModel. The naming convention follows this pattern: `On{HttpMethod}{HandlerName}()`.

- `handler=Hello` with a GET request calls `OnGetHello()`
- `handler=Submit` with a POST request calls `OnPostSubmit()`
- No handler parameter with GET calls `OnGet()`

This convention keeps your server-side code organized while giving htmx clear endpoints to call.

## The Request-Response Cycle with htmx

htmx works like a normal browser request but without a full-page reload. Here is how a typical request-response cycle flows:

1. User interacts with an element (clicks a button, types in an input, submits a form)
2. htmx sends an AJAX request to the specified endpoint
3. The Razor Page handler processes the request and returns partial HTML
4. htmx updates the target element with the returned HTML

This means you can build dynamic experiences while keeping your application logic centralized in your PageModel classes. The server remains in control. The client handles presentation.

## Examining How Partial Updates Work

Unlike traditional AJAX where you manually manipulate the DOM, htmx automatically swaps the response into a specified target. Two attributes control this behavior:

```html
<button hx-get="/Index?handler=Snippet" hx-target="#container" hx-swap="outerHTML">
    Load Content
</button>
<div id="container"></div>
```

- `hx-target` determines which element will be updated
- `hx-swap` decides how the response is inserted

The `hx-swap` attribute accepts several values:

- `innerHTML` (default): Replaces the target's inner content
- `outerHTML`: Replaces the entire target element
- `beforebegin`: Inserts before the target element
- `afterbegin`: Inserts inside the target, before its first child
- `beforeend`: Inserts inside the target, after its last child
- `afterend`: Inserts after the target element

By returning only the relevant snippet from the server, you make updates feel instant and keep bandwidth usage low.

## Creating Your First Interactive Razor Page

Let us create a Razor Page that dynamically loads a message when a button is clicked.

**Pages/Index.cshtml**

```html
@page
@model IndexModel

<h1>Welcome to htmx!</h1>
<button hx-get="/Index?handler=Message" hx-target="#message">Load Message</button>
<div id="message"></div>
```

**Pages/Index.cshtml.cs**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    public void OnGet()
    {
    }

    public IActionResult OnGetMessage()
    {
        return Content("<p>Hello from the server!</p>", "text/html");
    }
}
```

When you click the button, htmx calls `OnGetMessage()`, which returns a paragraph of HTML. htmx injects this into the `#message` div. The page does not reload. The URL does not change. Only the targeted element updates.

## Understanding `hx-trigger` for Event-Driven Interactions

By default, htmx triggers requests on click events for buttons and links, and on submit events for forms. You can customize this behavior with `hx-trigger`:

```html
<input type="text" 
       name="query"
       hx-get="/Index?handler=Search" 
       hx-target="#results" 
       hx-trigger="keyup changed delay:500ms" />
<div id="results"></div>
```

This input field triggers a search request when the user types. The trigger expression breaks down as follows:

- `keyup`: Fire on keyup events
- `changed`: Only fire if the value actually changed
- `delay:500ms`: Wait 500 milliseconds after the last keyup before firing

This debouncing prevents flooding your server with requests on every keystroke. The user types, pauses briefly, and then the request fires. It feels responsive without being wasteful.

## Implementing Dynamic Content Updates

Let us modify our earlier example to display different content each time the button is clicked.

**Pages/Index.cshtml.cs**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private static readonly string[] Messages =
    {
        "Hello, World!",
        "Welcome to htmx!",
        "Dynamic content is fun!",
        "Server-side rendering rocks!",
        "No JavaScript framework needed!"
    };

    public void OnGet()
    {
    }

    public IActionResult OnGetMessage()
    {
        var message = Messages[Random.Shared.Next(Messages.Length)];
        return Content($"<p>{message}</p>", "text/html");
    }
}
```

Now each click returns a different message from the array. The `Random.Shared` property provides a thread-safe random instance that you can use across requests without creating new instances.

## Handling Form Submissions with htmx

Forms are where htmx truly shines. Traditional form submissions reload the entire page. With htmx, you submit data and update only what needs to change.

### Setting Up Anti-Forgery Token Handling

ASP.NET Core Razor Pages require anti-forgery tokens for POST requests. Before creating forms, configure htmx to include this token automatically.

Add this script to your `_Layout.cshtml` after the htmx script:

```html
<script src="https://unpkg.com/htmx.org@2.0.4"></script>
<script>
document.body.addEventListener('htmx:configRequest', function(event) {
    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    if (token) {
        event.detail.headers['RequestVerificationToken'] = token.value;
    }
});
</script>
```

This event listener runs before every htmx request and adds the anti-forgery token to the request headers.

### Using `hx-post` to Submit Forms

With token handling configured, you can create forms that submit without page reloads:

**Pages/Index.cshtml**

```html
@page
@model IndexModel

<form hx-post="/Index?handler=Submit" hx-target="#result">
    @Html.AntiForgeryToken()
    <div>
        <label for="name">Your Name:</label>
        <input type="text" id="name" name="name" required />
    </div>
    <button type="submit">Submit</button>
</form>
<div id="result"></div>
```

**Pages/Index.cshtml.cs**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    public void OnGet()
    {
    }

    public IActionResult OnPostSubmit(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Content("<p class=\"error\">Please enter your name.</p>", "text/html");
        }
        
        return Content($"<p class=\"success\">Thank you, {name}!</p>", "text/html");
    }
}
```

When the form submits, htmx sends a POST request to `OnPostSubmit()`. The handler validates the input and returns HTML that htmx injects into `#result`. The form stays on the page. The user sees immediate feedback.

### Appending Responses Instead of Replacing

Sometimes you want to add content rather than replace it. The `hx-swap` attribute with `beforeend` appends new responses:

```html
<form hx-post="/Index?handler=AddItem" hx-target="#item-list" hx-swap="beforeend">
    @Html.AntiForgeryToken()
    <input type="text" name="item" required placeholder="Enter item" />
    <button type="submit">Add Item</button>
</form>
<ul id="item-list"></ul>
```

```csharp
public IActionResult OnPostAddItem(string item)
{
    return Content($"<li>{item}</li>", "text/html");
}
```

Each submission adds a new list item without clearing previous entries. This pattern works well for todo lists, comment sections, or any accumulating content.

### Form Validation with HTML5

htmx respects standard HTML5 form validation. The browser validates fields before htmx sends the request:

```html
<form hx-post="/Index?handler=Register" hx-target="#result">
    @Html.AntiForgeryToken()
    <div>
        <label for="email">Email:</label>
        <input type="email" id="email" name="email" required />
    </div>
    <div>
        <label for="age">Age:</label>
        <input type="number" id="age" name="age" min="18" max="120" required />
    </div>
    <button type="submit">Register</button>
</form>
<div id="result"></div>
```

The `type="email"` attribute validates email format. The `min` and `max` attributes constrain the number range. The `required` attribute ensures fields are not empty. All of this happens in the browser before htmx sends anything to the server.

For complex validation logic, handle it server-side and return appropriate error messages in your HTML response.

## Debugging and Observing htmx Requests

When things do not work as expected, htmx provides several ways to investigate.

### Inspecting Network Requests

Your browser's developer tools show every htmx request in the Network tab. Filter by XHR or Fetch to see only AJAX requests. For each request, examine:

- **URL**: Is htmx calling the correct endpoint?
- **Method**: Is it GET or POST as expected?
- **Headers**: Is the anti-forgery token present?
- **Response**: What HTML is the server returning?
- **Status**: Is the server returning 200, 400, 500?

### Common Errors and Solutions

**400 Bad Request on POST**

This usually means the anti-forgery token is missing. Ensure you have `@Html.AntiForgeryToken()` in your form and the token handling script in your layout.

**404 Not Found**

The handler name in your URL does not match a method in your PageModel. Remember: `handler=Submit` maps to `OnPostSubmit()` for POST requests and `OnGetSubmit()` for GET requests.

**Response Not Appearing**

Check that your `hx-target` selector matches an element that exists on the page. Verify the element has the correct `id` attribute.

**Incorrect Content-Type**

Ensure your handler returns `Content(..., "text/html")`. Returning JSON or plain text may not render as expected.

### Using htmx Events for Debugging

htmx fires events throughout the request lifecycle. You can listen to these for debugging:

```html
<script>
document.body.addEventListener('htmx:beforeRequest', function(event) {
    console.log('Request starting:', event.detail.pathInfo);
});

document.body.addEventListener('htmx:afterRequest', function(event) {
    console.log('Request complete:', event.detail.xhr.status);
});

document.body.addEventListener('htmx:responseError', function(event) {
    console.error('Request failed:', event.detail.xhr.status);
});
</script>
```

These event listeners help you trace exactly what htmx is doing and where problems occur.

### Enabling htmx Logging

For full debugging visibility, enable htmx's built-in logging:

```html
<script>
htmx.logAll();
</script>
```

This outputs every htmx event to the console, giving you complete visibility into the library's behavior.

## Complete Working Example

Here is a complete example that ties together everything from this chapter:

**Pages/Index.cshtml**

```html
@page
@model IndexModel

<h1>htmx Demo</h1>

<section>
    <h2>Dynamic Content Loading</h2>
    <button hx-get="/Index?handler=Quote" hx-target="#quote">Get Random Quote</button>
    <div id="quote"></div>
</section>

<section>
    <h2>Live Search</h2>
    <input type="text" 
           name="query"
           placeholder="Search fruits..."
           hx-get="/Index?handler=Search" 
           hx-target="#search-results" 
           hx-trigger="keyup changed delay:300ms" />
    <ul id="search-results"></ul>
</section>

<section>
    <h2>Form Submission</h2>
    <form hx-post="/Index?handler=Greet" hx-target="#greeting">
        @Html.AntiForgeryToken()
        <input type="text" name="name" required placeholder="Enter your name" />
        <button type="submit">Greet Me</button>
    </form>
    <div id="greeting"></div>
</section>
```

**Pages/Index.cshtml.cs**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private static readonly string[] Quotes =
    {
        "The best code is no code at all.",
        "Simplicity is the ultimate sophistication.",
        "First, solve the problem. Then, write the code.",
        "Code is like humor. When you have to explain it, it is bad."
    };

    private static readonly string[] Fruits =
    {
        "Apple", "Apricot", "Banana", "Blueberry", "Cherry",
        "Grape", "Lemon", "Mango", "Orange", "Peach", "Pear",
        "Pineapple", "Raspberry", "Strawberry", "Watermelon"
    };

    public void OnGet()
    {
    }

    public IActionResult OnGetQuote()
    {
        var quote = Quotes[Random.Shared.Next(Quotes.Length)];
        return Content($"<blockquote>{quote}</blockquote>", "text/html");
    }

    public IActionResult OnGetSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Content("", "text/html");
        }

        var matches = Fruits
            .Where(f => f.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return Content("<li>No matches found</li>", "text/html");
        }

        var html = string.Join("", matches.Select(f => $"<li>{f}</li>"));
        return Content(html, "text/html");
    }

    public IActionResult OnPostGreet(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Content("<p class=\"error\">Please enter your name.</p>", "text/html");
        }

        return Content($"<p>Hello, <strong>{name}</strong>! Welcome to htmx.</p>", "text/html");
    }
}
```

This single page demonstrates dynamic content loading, live search with debouncing, and form submission. Each feature uses a few htmx attributes and a simple server-side handler. No JavaScript framework. No complex state management. Just HTML attributes and C# methods working together.

## Summary

This chapter introduced the fundamentals of htmx with ASP.NET Core Razor Pages:

- Adding htmx to your project with a single script tag
- Using `hx-get` and `hx-post` to make AJAX requests
- Targeting elements with `hx-target` and controlling insertion with `hx-swap`
- Customizing triggers with `hx-trigger` for event-driven interactions
- Handling forms with proper anti-forgery token configuration
- Using HTML5 validation for client-side form validation
- Debugging with browser tools and htmx events

The pattern is consistent: add htmx attributes to your HTML, write a handler method that returns HTML, and let htmx handle the rest. Your server stays in control. Your markup stays readable. Your users get responsive interactions.

## Preview of Next Chapter

In the next chapter, we will explore more htmx capabilities including `hx-push-url` for updating the browser URL, `hx-swap-oob` for updating multiple elements with a single response, and additional trigger modifiers for fine-grained control over when requests fire.
