---
order: 12
icon: stack
label: Chap 19 - Optimizing Requests with hx-disable and hx-request
meta:
title: "Optimizing Requests with hx-disable and hx-request"
visibility: protected
---

# Optimizing Requests with `hx-disable` and `hx-request`

Chapter 19 marks a shift in focus from enhancing visual interactivity to optimizing the underlying behavior of your htmx-powered requests. Now that we’ve explored history-based caching and ways to preserve state and improve perceived performance, it's time to look at how you can further streamline your application's behavior through two lesser-known but powerful attributes: `hx-disable` and `hx-request`. These tools give you more control over when and how requests are made, helping you prevent accidental overloads, race conditions, and performance bottlenecks.

This chapter is all about control—controlling when elements can be interacted with, how requests are shaped, and what metadata accompanies them. With `hx-disable`, you can prevent duplicate submissions and unnecessary user interactions while requests are in-flight. It's an elegant way to create a polished user experience that feels thoughtful and intentional. On the other hand, `hx-request` gives you a configurable mechanism for adding headers, handling credentials, and shaping requests to match your server's expectations.

As your app grows more dynamic, the potential for noisy or unnecessary requests increases. Without careful attention, even well-designed UIs can fall prey to over-triggered network calls and inefficient interactions. The features in this chapter are about staying ahead of those issues—building with intention and performance in mind, while still delivering fluid experiences that respond quickly to user actions.

By the end of this chapter, you’ll know how to fine-tune request behavior at a granular level and understand how these optimizations contribute to a more stable and responsive Razor Pages app. You’ve already built interactive, engaging features; now it’s time to make them robust and production-ready. Let’s dive in.

## Fine-Tuning the Conversation: Mastering `hx-request` for Custom Interactions

The more dynamic your application becomes, the more control you’ll want over the requests it sends to the server. That’s where `hx-request` comes into play. This attribute lets you customize how htmx crafts the `request—from` the headers it includes to whether credentials should be sent, and even how requests are prioritized. It's like stepping behind the curtain and telling htmx exactly how to behave when it talks to your server.

So, when should you reach for `hx-request`? Think of scenarios where the default behavior isn't quite enough. Maybe you're sending data to an internal API that expects a special authentication header. Or perhaps you want a particular request to include credentials for a cross-origin endpoint. Or maybe, you just need to ensure certain background updates don't interrupt more important user interactions. `hx-request` lets you shape those behaviors cleanly in HTML, keeping your markup declarative and predictable.

Let’s say you’re building a dashboard that updates several widgets asynchronously. You want to ensure that the most important widget loads first, and the rest follow in the background. Here’s how you could assign priorities using `hx-request`:

```html
<div hx-get="/Dashboard?handler=PrimaryWidget"
     hx-target="#primary-widget"
     hx-trigger="load"
     hx-request='{"priority":"high"}'>
</div>

<div hx-get="/Dashboard?handler=SecondaryWidget"
     hx-target="#secondary-widget"
     hx-trigger="load"
     hx-request='{"priority":"low"}'>
</div>
```

By using `"priority":"high"` or `"low"`, you're giving htmx some direction on what matters most. While htmx doesn’t enforce strict request queuing, these hints help browsers and network optimizations take a more thoughtful approach under the hood.

Another valuable use case for `hx-request` is injecting custom headers. Suppose you’re integrating with an internal API that requires an API key. Instead of rewriting server-side logic or reaching for JavaScript, you can pass headers declaratively. Here’s how:

```html
<button hx-post="/api/data"
        hx-target="#api-result"
        hx-request='{
            "headers": {
                "X-API-Key": "abc123securetoken"
            }
        }'>
    Fetch Secure Data
</button>

<div id="api-result"></div>
```

The server on the receiving end can now look for the X-API-Key header and validate the request accordingly. If you're using ASP.NET Core to receive this on the backend, accessing the custom header is straightforward:

```C#
public IActionResult OnPostData()
{
    var apiKey = Request.Headers["X-API-Key"].ToString();
    if (apiKey != "abc123securetoken")
    {
        return Unauthorized();
    }

    return Content("<p>Data successfully retrieved.</p>", "text/html");
}
```
This pattern is incredibly useful for internal tools, admin panels, or scenarios where you need lightweight authentication without setting up full OAuth or cookie sessions. It also pairs well with `hx-headers`, which can dynamically attach headers on the client if needed.

You can also use `hx-request` to control whether cookies or credentials are sent with a request. For cross-origin requests involving authentication, this is a game-changer. By specifying `"credentials":"include"` or `"credentials":"same-origin"`, you can align your frontend behavior with backend expectations for secure communication.

Ultimately, `hx-request` lets you build smarter, more intentional interactions between your client and server. Instead of wiring up headers and behaviors through JavaScript fetch calls or `XMLHttpRequest`, you simply tell htmx what you need through attributes. It keeps your markup readable, your behavior declarative, and your performance sharp. It’s a subtle but powerful tool for crafting production-ready, highly interactive Razor Pages.

## Staying One Step Ahead: Managing User Input with `hx-disable`

User interaction can be unpredictable. Some users click once and wait patiently. Others click repeatedly, either out of habit or because they’re unsure whether their first click did anything. This can create problems, especially in web apps that rely on htmx for dynamic server interactions. Without a safeguard in place, one click might turn into five requests, leading to duplicate form entries, frustrated users, and unnecessary load on your server. That’s where `hx-disable` shines.

The `hx-disable` attribute tells htmx to disable the triggering element as soon as a request is fired. Once the server responds and the request completes, the element is automatically re-enabled. This simple feature is incredibly effective at controlling rapid-fire interactions, especially on forms and buttons. Think of it as a polite gatekeeper that ensures every interaction gets the attention it deserves, without piling on duplicates.

Let’s look at a common scenario: a feedback form with a single button to submit. Without any protection, a user could click the button multiple times in quick succession, sending multiple requests to the server. By adding `hx-disable`, you prevent that entirely. Here's how the form might look:

```html
<form hx-post="/Feedback?handler=Submit"
      hx-target="#result">
    <textarea name="Message" placeholder="Your feedback..."></textarea>
    <button type="submit" hx-disable>Send Feedback</button>
</form>

<div id="result"></div>
```
As soon as the user clicks "Send Feedback," the button becomes disabled and stays that way until the request completes. This behavior appears polished to the user and protects your backend from handling duplicate entries.

On the server side, things remain clean and straightforward. Here’s a simple Razor Page handler that processes the submission:

```C#
public IActionResult OnPostSubmit(string message)
{
    if (string.IsNullOrWhiteSpace(message))
    {
        return Content("<p>Please enter your feedback.</p>", "text/html");
    }

    // Save to database or send to support queue
    return Content("<p>Thanks for your feedback!</p>", "text/html");
}
```

In this case, the request runs once, the message is saved once, and your server stays happy. No extra logic is required to detect duplicates, and no frustrating user experience unfolds due to repeated clicks.

You can also apply `hx-disable` to other interactive elements like links or tabs that perform expensive operations or database calls. Whether it’s a search button, a modal launcher, or a dashboard refresh, anything that kicks off a network request can benefit from being temporarily disabled while waiting for a response.

It’s a small addition with a big impact. By incorporating `hx-disable` into your forms and buttons, you help reduce noise between your client and server, ensure cleaner data, and give users a smoother, more intuitive experience. In the world of server-driven apps, these details matter. They make the difference between an app that works and one that feels refined.

## Crafting Efficient Interactions: Practical Tips for Clean Request Handling

Once you’ve mastered `hx-disable` and `hx-request`, it becomes clear that request optimization is more than just preventing duplicates or adding headers. It’s about creating a system where every request feels intentional, performs well, and contributes to a smooth user experience. Choosing the right tool for the job is the first step, and it often comes down to understanding when to use `hx-disable` versus `hx-request`.

Use `hx-disable` when your goal is to prevent rapid or repeated user input. It’s perfect for buttons, form submissions, or links where only one action should happen at a time. It stops the user from flooding the server with unintentional clicks. On the other hand, use `hx-request` when you need to shape the request itself. If your focus is on headers, credentials, or prioritization, that’s where `hx-request` becomes essential. These attributes are complementary, and you’ll often use them together for full control.

Let’s combine `hx-disable` with `hx-trigger` and `hx-indicator` to build a refined user interaction. Imagine a product search input that needs to wait for typing to settle before sending a request. You also want to show a loading indicator while it searches and prevent the user from resubmitting the form too quickly. Here's a complete example:

```html
<form hx-post="/Search?handler=Lookup"
      hx-target="#results"
      hx-trigger="keyup changed delay:500ms"
      hx-indicator="#spinner">
    <input type="text" name="query" placeholder="Search..." />
    <button type="submit" hx-disable>Go</button>
    <span id="spinner" class="spinner visually-hidden">Loading...</span>
</form>

<div id="results"></div>
```

This approach ensures the request only fires after the user stops typing, the spinner appears immediately, and the button becomes inactive during the request. The result is a more fluid and professional interaction that also protects your server and user data.

When you’re troubleshooting slow or unexpected behavior, your browser's developer tools are your best friend. Open the Network tab and inspect the requests being sent. Look at the timing, headers, and response content. You can also add `hx-debug="true"` to any element to get console logs of how htmx is interpreting your attributes. If something seems off, this debugging step often reveals the issue quickly.

Sometimes, your request behavior will need fine-tuning. Maybe a trigger is too aggressive or fires on an event you did not expect. You can use `hx-trigger` expressions like keyup changed `delay:300ms` to reduce noise and debounce interactions. Or you might realize your target element is not swapping the right content, which can be fixed by inspecting `hx-swap` behavior in the request.

As your app grows, so does the impact of inefficient requests. Monitoring tools like Application Insights or custom logging in your Razor Page handlers can help track request frequency, timing, and failure rates. This visibility is essential for identifying patterns that lead to performance slowdowns or backend strain.

The key takeaway is this: requests should feel invisible to the user and obvious to the server. They should be timely, efficient, and respectful of system resources. The combination of `hx-disable`, `hx-request`, and smart triggering patterns gives you everything you need to reach that goal.

In the next chapter, we’ll take these ideas even further by exploring the advanced side of htmx performance. From caching strategies and lazy loading to minimizing latency and improving responsiveness, you’ll learn how to squeeze every ounce of speed and reliability out of your Razor Pages app. Request optimization is just the beginning. Let’s keep going.