---
order: 16
icon: stack
label: Chap 15 - Visual Feedback with hx-indicator and hx-preserve
meta:
title: "Visual Feedback with hx-indicator and hx-preserve"
visibility: protected
---

# Visual Feedback with `hx-indicator` and `hx-preserve`

In modern web applications, responsiveness is more than just speed, it’s about communication. Users want to know that something is happening the moment they click a button or submit a form. This chapter introduces two powerful tools in the htmx toolbox, `hx-indicator` and `hx-preserve`, that help your ASP.NET Core Razor Pages applications speak clearly to your users by providing smooth, purposeful visual feedback.

So far, we’ve explored how htmx enhances interactions through `hx-get`, `hx-post`, and a rich collection of other attributes like `hx-target`, `hx-trigger`, and `hx-swap`. We’ve dynamically loaded content, managed modal dialogs, and even refined our UI components like tabs and tables. Now, we turn to the often overlooked, but absolutely essential, aspect of user experience: giving users immediate, visible clues that their actions are being processed.

Whether it’s a loading spinner appearing during a long request or maintaining scroll position across updates, htmx gives us tools to craft applications that feel seamless and intuitive. `hx-indicator` lets us show activity during requests without manual JavaScript, while `hx-preserve` helps us keep parts of the page steady during dynamic changes. These features may not shout for attention, but they can quietly make or break how polished your app feels.

In the next few sections, we’ll walk through how to wire up indicators for various scenarios and when to preserve specific elements to avoid jarring UI transitions. By the end of this chapter, you’ll be able to add the finishing touches that turn functional interactivity into a delightful user experience. Let’s bring your interfaces to life.

## Loading in Style: Enhancing UX with `hx-indicator`

When a user interacts with your web app, submitting a form, clicking a button, filtering a table, there’s often a tiny window where nothing seems to happen. No new content, no animation, just silence. In that gap, even if the server is hard at work, users can become uncertain or frustrated. That’s where visual feedback becomes essential. It reassures users that their input was received and that the system is actively processing it. For asynchronous requests triggered by htmx, the `hx-indicator` attribute provides a straightforward way to bridge that communication gap.

At its core, `hx-indicator` lets you define an HTML element that will automatically receive a CSS class (usually htmx-request) whenever an htmx request is in flight. That means you can toggle loading spinners, fade overlays, or even just disable buttons, all without writing any JavaScript. It’s a simple mechanism that unlocks significant improvements to your app’s perceived performance and usability.

Here’s a basic example. Let’s say you have a search form that queries a database and updates a list of results without reloading the page. You’d like to show a loading spinner next to the search button while the query is running. Here’s how you’d do it:

```html
<form hx-get="/Search" hx-target="#results" hx-indicator="#spinner">
    <input type="text" name="query" placeholder="Search users..." />
    <button type="submit">Search</button>
    <span id="spinner" class="spinner visually-hidden">🔄</span>
</form>
<div id="results"></div>
```

And with a little CSS:

```css
.spinner {
    margin-left: 8px;
    opacity: 0;
    transition: opacity 0.3s;
}

.htmx-request.spinner {
    opacity: 1;
}
```

When the user submits the form, htmx automatically adds the htmx-request class to the `#spinner` element, making it visible during the request. Once the server responds and the update is complete, the class is removed and the spinner fades away. This approach keeps your UI responsive and your users informed, without a single line of JavaScript.

You can also apply `hx-indicator` to more than just inline spinners. Want to block an entire section while loading? Wrap your content in a container with an overlay and trigger it using `hx-indicator`. For example, on a table that updates dynamically, you could show a semi-transparent overlay:

```html
<div class="table-container" hx-indicator="#table-loader">
    <div class="overlay visually-hidden" id="table-loader">Loading...</div>
    <table>
        <!-- rows rendered here -->
    </table>
</div>
```

This scales beautifully to larger components like modals, card lists, or any area where you need to signal that something is happening. And because the attribute can target any CSS selector, you can define very specific and context-aware indicators without duplicating logic across your app.

For an even smoother user experience, consider pairing `hx-indicator` with `hx-target` and `hx-swap` for fine-tuned updates. For instance, as a user types into a search box, you can debounce the requests and still show a subtle spinner, keeping the UI lively without being overwhelming.

Visual feedback might seem like a small detail, but in fast, interactive applications, it’s what separates “feels broken” from “feels polished.” With `hx-indicator`, you’ve got an elegant, server-driven way to let your users know the system is responsive and attentive to their actions. It's another example of htmx and Razor Pages working together to keep things simple, powerful, and user-friendly.

## Maintaining State Between Updates with `hx-preserve`

One of the subtle challenges in building interactive web apps with htmx is handling what happens after a partial update. When htmx replaces content inside a div or a section, it swaps out the HTML as if the user never touched it. That works great for displaying fresh content, but it can be jarring and even frustrating when it erases something the user was in the middle of doing. Think of a partially filled form that gets refreshed after an update, or a toggle switch that resets unexpectedly. It’s a small issue that can quickly turn into a big annoyance.

That’s where `hx-preserve` comes in. It tells htmx: "Hey, if you’re about to replace this element with something new, please keep the original version if nothing has changed structurally." In other words, `hx-preserve` helps you maintain a user's place or input in the UI while still letting the server send back fresh content. It works by tracking DOM elements with the id attribute and comparing them during the update.

Here’s a practical scenario. Let’s say you have a multi-field search form that updates a results list when submitted. If the server returns a refreshed version of the form (maybe with a validation message or updated placeholder), you don't want it to clear out everything the user just typed. By adding `hx-preserve` to the form inputs, htmx will keep their current values intact during the update.

```html
<form hx-post="/Search" hx-target="#results">
    <input id="query" name="query" type="text" placeholder="Search..." hx-preserve />
    <select id="filter" name="filter" hx-preserve>
        <option value="all">All</option>
        <option value="active">Active</option>
        <option value="archived">Archived</option>
    </select>
    <button type="submit">Go</button>
</form>
<div id="results"></div>
```

In this case, even if your Razor Page returns a refreshed <form> block (maybe to display error messages or change UI elements), the values in the input and select fields won’t be lost. The user won’t have to re-enter anything unless something actually changed in the response.

Here’s how the backend might look in Search.cshtml.cs:

```csharp
public class SearchModel : PageModel
{
    [BindProperty]
    public string Query { get; set; }

    [BindProperty]
    public string Filter { get; set; }

    public IActionResult OnPost()
    {
        var results = PerformSearch(Query, Filter);
        return Partial("_Results", results);
    }
}
```

With this setup, you can re-render the entire form or page section on each post without breaking the user’s flow. This makes for a smoother, more forgiving user experience, especially in dynamic applications where UI elements are expected to change frequently.

`hx-preserve` shines in other places too, like editable table rows, multi-step forms, or anywhere users are working with interactive controls that shouldn’t reset unless they choose to. It's not a magic bullet, but it's a powerful little attribute that shows how htmx values thoughtful defaults and practical design.

As you start adding more interactivity to your Razor Pages with htmx, don’t overlook how much preserving small pieces of state can improve the feel of your application. `hx-preserve` makes your UI smarter and friendlier by letting the user stay right where they left off, even when the DOM is changing under the hood.

## Optimizing Visual Feedback for Performance and Accessibility

Good visual feedback isn’t just about showing that “something is happening.” It’s about doing so gracefully without overwhelming users or creating friction in the experience. When using htmx with Razor Pages, it’s tempting to add indicators and animations everywhere. But the best user experiences are often the quietest ones: subtle transitions, well-timed loaders, and minimal distractions that let the user stay focused on their task.

To strike the right balance, consider adding simple CSS transitions to your loading indicators. A sudden pop-in of a spinner can feel jarring, especially if the request finishes quickly. By easing in and out, you make the interaction feel smooth and intentional. Here’s an example of a reusable spinner with a fade-in effect:

```html
<span id="spinner" class="spinner visually-hidden">🔄</span>
```

```css
.spinner {
    opacity: 0;
    transition: opacity 0.3s ease-in-out;
}

.htmx-request.spinner {
    opacity: 1;
}
```

This simple transition ensures that even if a request lasts only a split second, the spinner doesn’t flash harshly; it gently fades in and out, improving the overall feel of the app. But keep in mind: not every action needs a visual cue. If a response happens so fast the user doesn’t notice the delay, adding an indicator may cause more harm than good by drawing unnecessary attention to what should feel instantaneous.

Another key to scaling visual feedback is thinking in components. Whether it’s a button-level spinner, a card overlay, or a full-page loader, define small, focused UI elements that can be reused across your app. A Razor partial that wraps content with a loader div and binds the indicator dynamically can save time and keep your layout DRY. These UI helpers don’t just improve performance, they simplify accessibility too.

Speaking of which, accessibility matters here. Make sure your indicators are not just visual. If a spinner or overlay hides important content, add appropriate ARIA attributes. For example, you might use `aria-busy="true"` on sections that are being updated, or `role="status"` on indicators so screen readers can alert users that something is loading. These small touches ensure everyone has a good experience, regardless of how they interact with your app.

As your app grows, resist the urge to treat every dynamic interaction the same. Tailor the level of visual feedback to the user’s intent and the perceived cost of the action. Submitting a large form or fetching a long list? A spinner makes sense. Clicking a filter that updates a portion of the page quickly? Maybe the change itself is enough of a cue.

We’ve now covered how `hx-indicator` and `hx-preserve` help you create smoother, smarter experiences. In the next chapter, we’ll take it a step further by refining which parts of the page get updated with `hx-select` and `hx-select-oob`. These tools let you surgically control what gets swapped, allowing for even more precise and efficient UI updates without replacing more of the DOM than necessary.

So while your server continues to do the heavy lifting, your front end can remain light, responsive, and thoughtfully designed. That's the kind of experience users remember.