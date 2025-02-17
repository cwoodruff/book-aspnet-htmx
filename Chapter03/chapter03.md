---
order: 28
icon: stack
label: Chap 3 - First Steps with htmx
meta:
title: "First Steps with htmx"
---
# 3

# First Steps with htmx

![](Images/chapter03-first-steps-with-htmx.png)

_**You can find the source examples for this chapter [here](https://github.com/cwoodruff/book-aspnet-htmx/tree/main/_src/chap03).**_

The way we build web applications is evolving. While client-side JavaScript frameworks dominate modern development, they often introduce unnecessary complexity—forcing developers to juggle state management, bundlers, and intricate frontend tooling. htmx, on the other hand, plays a crucial role in simplifying this process, enhancing HTML’s native capabilities and allowing developers to craft highly interactive experiences with minimal JavaScript. This chapter will guide you through integrating htmx with ASP.NET Core Razor Pages to build seamless, dynamic web applications without the burden of complex JavaScript frameworks.

By focusing on server-driven interactions, htmx makes it possible to create fluid user experiences without requiring a full-fledged JavaScript framework. It leverages the power of declarative attributes to handle AJAX requests, event-driven interactions, and form submissions—all while keeping your markup clean and understandable. When combined with Razor Pages, htmx offers a robust way to blend server-side rendering with just the right amount of interactivity, making your applications efficient, easy to maintain, and giving you control over your projects. This efficiency and maintainability are key benefits of using htmx.

This chapter is designed to take you from the fundamental principles of htmx to implementing fully functional Razor Pages. You will learn to dynamically fetch, update, and submit content without needing a full page reload. We’ll cover everything from using `hx-get` and `hx-post` for data retrieval and submission to debugging common issues and fine-tuning request behaviors. By the end, you will be well-prepared to build interactive web applications that are both modern and maintainable.

# Understanding the Basics of htmx in Razor Pages

At its core, htmx allows you to send AJAX requests using simple HTML attributes. The two most fundamental ones are:
* `hx-get`: Makes an HTTP `GET` request to fetch content from the server.
* `hx-post`: Sends a `POST` request to submit data to the server.

Example:
```HTML
<button hx-get="/hello" hx-target="#message">Click me!</button>
<div id="message"></div>
```

When the button is clicked, an asynchronous `GET` request is made to `/hello`, and the response updates the `#message` div.

## The Request-Response Cycle with htmx in Razor Pages

Under the hood, htmx works just like a normal browser request but without a full-page reload. Here’s how a typical request-response cycle looks:

1.	User interacts with an element (e.g., clicks a button).
2.	htmx sends an AJAX request to the specified endpoint.
3.	The Razor Page processes the request and returns partial HTML.
4.	htmx updates the target element with the returned HTML.

This means you can build dynamic experiences while keeping your app’s logic centralized in your Razor Page Model (`.cshtml.cs`).

## Examining How Partial Updates Work in the Browser

Unlike traditional AJAX, where you manually manipulate the DOM, htmx automatically swaps the response into a specified target. This is controlled using `hx-target` and `hx-swap`:

```ASP.NET (C#)
<button hx-get="/getSnippet" hx-target="#container" hx-swap="outerHTML">Load Content</button>
<div id="container"></div>
```

* `hx-target` determines which element will be updated.
* `hx-swap` decides how the response is inserted (`innerHTML`, `outerHTML`, `beforebegin`, etc.).

By returning only the relevant snippet from the server, you make updates feel instant and efficient.

# Creating Your First Interactive Razor Page with htmx

Let’s create a simple Razor Page that dynamically loads a message when a button is clicked.

**Pages/Index.cshtml**

```ASP.NET (C#)
@page
@model IndexModel

<h1>Welcome to htmx!</h1>
<button hx-get="/Index?handler=Message" hx-target="#message">Load Message</button>
<div id="message"></div>
```

**Pages/Index.cshtml.cs**

```C#
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

public class IndexModel : PageModel
{
    public IActionResult OnGetMessage()
    {
        return Content("<p>Hello from the server!</p>", "text/html");
    }
}
```

## Using `hx-get` to Dynamically Load Content from Another Page

In the above example, clicking the button triggers an `hx-get` request to `OnGetMessage()`, which returns a snippet of HTML. htmx then injects this snippet into the #message div.

## Understanding `hx-trigger` for Event-Driven Interactions

By default, htmx triggers requests on clicks, but you can customize it with `hx-trigger`:

```ASP.NET (C#)
<input type="text" hx-get="/search" hx-target="#results" hx-trigger="keyup changed delay:500ms" />
<div id="results"></div>
```

Here, every keystroke (after a 500ms delay) triggers a request to update the results dynamically.

## Implementing a Button Click That Fetches and Displays Content

Let’s modify our earlier example to display different content each time the button is clicked.

**Updated Index.cshtml.cs**

```C#
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private static readonly string[] Messages =
    {
        "Hello, World!", "Welcome to htmx!", "Dynamic content is fun!"
    };

    public IActionResult OnGetMessage()
    {
        var message = Messages[new Random().Next(Messages.Length)];
        return Content($"<p>{message}</p>", "text/html");
    }
}
```

Now, each click returns a different message from the list.

# Handling Form Submissions with htmx

## Using `hx-post` to Submit Forms Without Reloading the Page

htmx makes form submissions seamless by eliminating full-page reloads.

```ASP.NET (C#)
<form method="post" hx-post="/Index?handler=Submit" hx-target="#result">
    <input type="text" name="name" required />
    <button type="submit">Submit</button>
</form>
```

## Processing Form Data in the Razor Page Model

**Index.cshtml.cs**

```C#
public IActionResult OnPostSubmit(string name)
{
    return Content($"<p>Thank you, {name}!</p>", "text/html");
}
```

When the form is submitted, htmx sends the data, and the response dynamically updates #result.

## Displaying Server Responses Dynamically with `hx-target` and `hx-swap`

By default, `hx-target` updates a specific element, and `hx-swap` determines how the response is inserted. You can change the behavior like so:

```ASP.NET (C#)
<form method="post" hx-post="/Index?handler=Submit" hx-target="#result" hx-swap="beforeend">
    <input type="text" name="name" required />
    <button type="submit">Submit</button>
</form>
```

This appends new responses instead of replacing them.

## Introducing `hx-validate` for Basic Client-Side Validation

htmx supports built-in validation with `hx-validate`:

```ASP.NET (C#)
<form method="post" hx-post="/Index?handler=Submit" hx-validate>
    <input type="email" name="email" required />
    <button type="submit">Submit</button>
</form>
```

# Debugging and Observing htmx Requests

## Inspecting htmx Network Requests Using Browser Dev Tools

You can inspect AJAX requests in the browser’s dev tools under the Network tab.

## Common Errors and How to Debug Them

* Incorrect Content-Type: Ensure responses return text/html.
* Server Errors: Check the console for 500 errors.

## Using the htmx:configRequest Event for Debugging

```C#
document.body.addEventListener('htmx:configRequest', function(event) {
    console.log("HTMX request", event.detail);
});
```

# Preview of Next Chapter: Understanding htmx Commands

In the next chapter, we’ll dive deeper into htmx commands and interactions, including `hx-push-url`, `hx-replace-url`, and Hyperscript.
