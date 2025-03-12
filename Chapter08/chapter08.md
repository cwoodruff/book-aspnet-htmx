---
order: 23
icon: stack
label: Chap 8 - Handling Events with hx-trigger and hx-on
meta:
title: "Handling Events with hx-trigger and hx-on"
visibility: hidden
---
# 8

# Handling Events with `hx-trigger` and `hx-on`

Welcome to a deeper look at how htmx manages user interactions in Razor Pages. Until now, we’ve explored how attributes like `hx-get` and hx-post can handle requests on a basic click or form submission. In this chapter, we’ll focus on precisely controlling when those requests occur, as well as how to set up custom behaviors for more interactive scenarios. This is where `hx-trigger` and `hx-on` step into the spotlight—two powerful attributes that give you fine-grained control over user events and responses.

We’ll explore how `hx-trigger` can turn practically any browser event into a chance for your server to provide dynamic content. Whether you want to react to a simple mouse hover, a keyboard press, or something more creative, `hx-trigger` allows you to specify exactly when your htmx-powered elements should call home. Beyond that, we’ll look at `hx-on`, a feature that lets you define your own event listeners, opening the door to custom logic and expanded interactivity.

By the end of this chapter, you’ll see just how flexible your Razor Pages can become once you master these event-driven techniques. With `hx-trigger` and `hx-on`, your application will be equipped to respond to user actions in real time without piling on extra JavaScript, keeping the focus on clean, server-side code that feels both modern and refreshingly simple.

## Mastering `hx-trigger` for Real-Time User Engagement

``hx-trigger`` is an htmx attribute that controls when server requests get fired, giving you precise control over how your pages interact with user actions. If you leave `hx-trigger` unspecified, htmx typically defaults to firing a request on the most logical event for that element, such as click for a button or submit for a form. By customizing `hx-trigger`, you can expand beyond these built-in defaults and tap into virtually any browser event, from the common click to more advanced possibilities like intersect or a timed interval.

The difference between default events and custom triggers can significantly influence user experience. Default events keep your code simple and work perfectly for many scenarios. On the other hand, custom triggers let you tune performance and responsiveness, especially in situations where you want an action to occur only after a specific sequence of events or under certain conditions. By treating triggers as your application’s traffic lights, you ensure data moves between client and server at precisely the right moments.

Standard triggers like click, change, and submit generally work out of the box. A button with `hx-get` will fire on click, an input with `hx-post` will fire on form submission, and a select element might default to change for updating on selection. These defaults reduce boilerplate code and make everyday interactions seamless for you and your users, while still allowing you to override or refine them when needed.

Advanced triggers shine in more specialized scenarios. If you want repeated polling from the server, you can set `hx-trigger` to every 5s. If you need a request to happen when an element scrolls into view, `hx-trigger` can be assigned intersect. For real-time text monitoring, keyup changed ensures updates happen only if the user actually modifies the input. This allows you to avoid excessive requests and handle user actions more gracefully.

A powerful illustration of `hx-trigger` in action is building a live search. Imagine a simple text input that hits your server with each keystroke, but only when the input has truly changed. In your Razor Page, you might have something like this:

```HTML
<input type="text" hx-get="/Search?handler=Live" `hx-trigger`="keyup changed delay:300ms" hx-target="#searchResults" placeholder="Type to search..." />
<div id="searchResults"></div>
```

The keyup changed delay:300ms tells htmx to wait 300 milliseconds after the most recent keyup event before sending a GET request, and only if the text has indeed changed. This helps reduce noisy interactions and keeps server load under control. On the server side, your Razor Page might include a handler like this:

```C#
public IActionResult OnGetLive(string searchTerm)
{
    var results = _context.Products
        .Where(p => p.Name.Contains(searchTerm))
        .Select(p => p.Name)
        .ToList();

    var htmlSnippet = "<ul>";
    foreach (var item in results)
    {
        htmlSnippet += $"<li>{item}</li>";
    }
    htmlSnippet += "</ul>";

    return Content(htmlSnippet, "text/html");
}
```

The user begins typing in the search field, triggers are activated on keyup if the input changes, and the server responds with matching items. By mixing standard and advanced triggers, you can design interactions that feel modern and efficient without the overhead of a sprawling JavaScript setup.

## Elevating Interactions with `hx-on`

`hx-on` is an attribute that lets you tap into both built-in and custom JavaScript events in htmx, effectively broadening your control beyond the straightforward timing of requests that `hx-trigger` provides. While `hx-trigger` is all about specifying when a request should fire, `hx-on` is about telling htmx, “When you see this event, run that action.” You can think of `hx-trigger` as scheduling server requests, whereas `hx-on` is about hooking into an event-driven workflow where you can validate input, run animations, or even stop a request from ever leaving the browser.

You might decide to use `hx-on` when you need to respond to something that’s not purely about firing a request. A custom JavaScript event is a perfect example. You could dispatch an event from your client code—say, `document.dispatchEvent(new CustomEvent("myCustomEvent", { detail: "someData" }))`—and `hx-on` can then listen for `myCustomEvent` on a specific HTML element. Once caught, you can do anything from updating the UI to prepping data before a request goes out, all within a clean, attribute-based syntax.

Preventing a form submission is an essential use case for `hx-on`. Suppose you want to validate that a user’s input meets certain criteria before letting the request proceed. You can intercept the form submission event by using something like `hx-on="submit: if(!validateInputs()) event.preventDefault()"`. This means if your `validateInputs()` function fails, you keep the submission local. The rest of your Razor Page remains blissfully unaware that the user tried to submit invalid data, which can significantly improve your user experience.

Here’s a small example of preventing form submission with `hx-on`. The form tries to post data to the server, but we only let the request proceed if our validation logic passes:

```HTML
<form hx-post="/Contact?handler=Submit" hx-target="#response" `hx-on`="submit: if(!validateForm()) event.preventDefault()">
    <input type="email" name="email" id="email" placeholder="Enter your email" />
    <button type="submit">Send</button>
</form>
<div id="response"></div>
<script>
    function validateForm() {
        const email = document.getElementById('email').value;
        return email.includes('@');
    }
</script>
```

Another place `hx-on` shines is in orchestrating UI animations on request completion. Suppose you want a fade-in effect after the server returns new content. You can attach `hx-on` to watch for an htmx event like `htmx:afterSwap`, which fires once the response is placed into the DOM. If you have a fadeIn(element) helper, you can tie it in by writing `hx-on="htmx:afterSwap: fadeIn(document.getElementById('response'))"`. This approach ensures the animation triggers only after the new HTML chunk is actually rendered.

Debugging event-based issues sometimes requires pinpointing the exact moment your code runs. htmx includes a suite of lifecycle events like `htmx:configRequest`, `htmx:beforeRequest`, and `htmx:afterRequest` that can be valuable for logging. In your HTML, you might write `hx-on="htmx:beforeRequest: console.log('About to send request');"` to see if the event is firing as expected. That way, you can confirm the request sequence and see where things might be going wrong.

`hx-on` tends to shine in tandem with `hx-trigger` since the two can coexist on the same element—one controls when requests happen, and the other manages event listeners. By combining them, you can create flows where user input triggers requests at exactly the right time, and custom scripts or UI effects run reliably afterward, keeping your ASP.NET Core application responsive and maintainable.

## Custom Server-Driven Events for Seamless UI Refresh

Custom events in htmx provide a way to trigger dynamic updates without exclusively relying on user actions or polling intervals. Instead of using out-of-band swaps, which instruct htmx to render HTML outside the normal page flow, you can dispatch a custom event from the server response that initiates a new AJAX request to refresh specific parts of the UI. This approach grants you a powerful mechanism to respond to server-side changes as they happen, while still writing minimal JavaScript in your Razor Pages application.

One typical scenario is refreshing a notifications panel. Suppose your server finishes a background task that has just created new notifications for the user. Rather than waiting for another user click or a time-based poll, you can include a small script in the server’s response that dispatches a custom event. By pairing `hx-on` with that custom event, you can instruct htmx to fetch updated content for the notifications panel immediately, resulting in a near real-time experience.

A simple Razor Page might look like this in your `Notifications.cshtml` file, which is in charge of fetching the latest notifications:

```HTML
<div id="notificationsPanel" `hx-on`="notifications-updated: this.closest('[hx-get]').click()"
     hx-get="/Notifications?handler=List" hx-target="#notificationsPanel">
    <!-- existing notifications here -->
</div>
```

When the server completes a process that spawns new notifications, it can return a snippet of HTML containing a script like this:

```C#
public IActionResult OnPostCreateNotification()
{
    // ... logic that creates a new notification ...
    var scriptToTriggerRefresh = "<script>document.dispatchEvent(new CustomEvent('notifications-updated'));</script>";
    return Content(scriptToTriggerRefresh, "text/html");
}
```

As soon as this script runs in the browser, it dispatches the notifications-updated event. Because `hx-on` is listening for notifications-updated on the panel, it immediately invokes the existing hx-get request, pulling fresh notifications from the server and injecting them into the panel. This bypasses the need for out-of-band swaps, since your UI remains coherent under the direction of custom events, ensuring the data is always in sync without cluttering your markup with OOB directives.

Dynamically updating a user’s status follows the same principle. Rather than forcing the client to poll for changes, you embed a small script whenever a status update occurs on the server. The newly dispatched event triggers an htmx request that retrieves the updated status. This means your UI can instantaneously reflect shifts from “online” to “away,” or from “busy” to “available,” with minimal overhead and minimal JavaScript.

The real power here is in how little code you need to orchestrate a thoroughly responsive UI. By dispatching events only when meaningful changes occur on the server, you avoid unnecessary queries and maintain tight control over which components refresh. This helps reduce load on your server while also keeping the user experience fast and responsive.

Next, we will explore "Integrating Hyperscript for Complex Logic," where you will see how even more sophisticated client-side behaviors become possible. Rather than resorting to large JavaScript frameworks, you can solve a surprising array of interaction challenges with a few lines of Hyperscript, letting you blend dynamic server updates with advanced in-browser logic in an elegant, minimalistic way.