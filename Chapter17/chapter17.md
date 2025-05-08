---
order: 14
icon: stack
label: Chap 17 - Form Enhancements with hx-params, hx-vals, and hx-validate
meta:
title: "Form Enhancements with hx-params, hx-vals, and hx-validate"
visibility: hidden
---

# Form Enhancements with `hx-params`, `hx-vals`, and `hx-validate`

By now, you’ve built dynamic forms, submitted data without page reloads, and even added some validation magic using `hx-validate`. You’ve seen how htmx makes basic form handling elegant and server-friendly. But real-world forms are rarely basic. Sometimes, you need to customize what gets sent. Sometimes, you want to pass extra values that aren't part of the form. And sometimes, you need to validate in more nuanced ways, without turning to JavaScript.

That’s where this chapter comes in. We’ll explore the finer controls htmx gives you when dealing with form submissions. `hx-params` lets you choose exactly which fields get sent. `hx-vals` helps you inject values that don’t even exist in the form. And `hx-validate`? It goes beyond browser-level constraints and lets you create more interactive, user-friendly validation flows while keeping your JavaScript footprint nearly invisible.

This isn’t a rehash of the basics. You’ve already mastered how to submit a form with `hx-post`. Here, we’ll fine-tune the experience and make your forms work smarter. You’ll learn how to craft forms that respond to the user’s context, reduce unnecessary data transfer, and give immediate feedback, all while staying true to the server-driven, Razor Pages first design.

Once you're comfortable with these advanced form strategies, you'll be in the perfect position to take on the next challenge: optimizing performance. In the following chapter, we’ll explore caching and browser history tricks that make your htmx-powered apps feel lightning-fast.

## Customizing Form Submission with `hx-params`

Forms can quickly get messy, especially when they’re long, dynamic, or reused across multiple steps. Sometimes, you don’t want to send every single field to the server on every submission. Maybe you’ve got a multi-step form and only want to process one section at a time. Or maybe you're using hidden fields to manage layout or state and don’t want them interfering with your model binding. That’s where `hx-params` comes in.

The hx-params attribute gives you control over which fields get included in the request triggered by htmx. By default, htmx submits all fields inside a form when an interaction happens. But you can override this by applying `hx-params` to any element that triggers a request, whether it's the <form> itself or an individual button inside it.

Let’s walk through the three most common uses of `hx-params: *`, not fieldName, and none.

Setting `hx-params="*"` means “send all the parameters,” which is actually the default behavior. It’s useful when you're overriding a narrower setting higher up and want to re-enable full submission on a specific button or input.

On the other hand, `hx-params="not fieldName"` tells htmx to submit every field except the one you name. You can also exclude multiple fields by separating them with spaces. This is incredibly handy when you've got data you don’t want to send back just yet, like fields from a different step in a wizard-style form or a large textarea that the user hasn’t completed.

Then there’s `hx-params="none"`, which does exactly what it sounds like: no form fields are included in the request. This can be useful when you're triggering a refresh or some background action that doesn’t rely on the form data at all.

Here’s a quick example. Let’s say you’re building a three-step account setup form using Razor Pages. Each step is a partial that gets loaded and submitted independently. You want to make sure that only the fields from the current step are submitted when the user clicks "Next".

In your Razor Page:

```html
<form id="step-form" hx-post="/AccountSetup?handler=Step1" hx-target="#step-container">
    <input type="text" name="FirstName" placeholder="First Name" />
    <input type="text" name="LastName" placeholder="Last Name" />
    <input type="email" name="Email" placeholder="Email" />
    <input type="hidden" name="Step" value="1" />
    <button type="submit" hx-params="not Step Email">Next</button>
</form>
<div id="step-container"></div>
```

In this case, when the user clicks "Next", only FirstName and LastName are submitted; Email and the hidden Step field are excluded. This keeps the payload clean and lets your server focus on validating just the current step. Your handler might look like this:

```csharp
public IActionResult OnPostStep1(string FirstName, string LastName)
{
    // Store progress or validate names
    return Partial("_Step2");
}
```

You could also flip it around and have a button that triggers a backend validation or refresh that doesn’t require any form data. Simply add `hx-params="none"` to that button, and htmx will send the request without any fields included.

The best part? You don’t have to restructure your form to make this work. `hx-params` is flexible, composable, and doesn’t interfere with your Razor Page model binding. Hence, you get clean, predictable behavior with minimal effort.

This kind of precision becomes critical as your forms grow in complexity. Whether you’re building a checkout process, onboarding flow, or admin panel, `hx-params` helps you send only what matters, when it matters. It’s one of those features you don’t realize you need until you do.

## Dynamically Modifying Request Data with `hx-vals`

Sometimes, the data you need to send with a form submission or button click isn’t in the form at all. Maybe you need to pass metadata like a timestamp, a role identifier, or a contextual flag based on what the user is doing. Instead of cluttering your form with hidden fields or patching things together with JavaScript, `hx-vals` gives you a clean, declarative way to inject additional data into your htmx requests.

The `hx-vals` attribute allows you to add extra name-value pairs to the request payload. It works like an invisible, dynamic field set that’s sent along with your form data or triggered request. This is incredibly useful when your server-side handler expects more than just the visible inputs, like flags for conditional logic, environment details, or user-supplied metadata.

You define `hx-vals` using a JavaScript object (as a JSON string) right in your markup. htmx parses that string and merges the values into the outgoing request. These values are included in the same way as form fields, so your Razor Page handler can access them just like any other bound parameter.

Imagine you have a form that allows users to submit feedback, and you want to include the current timestamp and a user role to help categorize the submissions. You don’t need those fields in the UI, but you want them on the server. Here’s how you can do it:

```html
<form hx-post="/Feedback?handler=Submit" hx-target="#result">
    <textarea name="Message" placeholder="Your feedback..."></textarea>
    <button type="submit"
            hx-vals='{"SubmittedAt": "' + new Date().toISOString() + '", "UserRole": "anonymous"}'>
        Send Feedback
    </button>
</form>
<div id="result"></div>
```

If you’re using this directly in Razor, you'll want to handle the date formatting with a bit more care. Alternatively, you can use JavaScript to update the attribute dynamically just before the request. Here's an example using Hyperscript:

```html
<button _="on click
    set my hx-vals to JSON.stringify({'SubmittedAt': new Date().toISOString(), 'UserRole': 'anonymous'})"
    hx-post="/Feedback?handler=Submit"
    hx-target="#result">
    Send Feedback
</button>
```

This approach allows you to compute values on the fly based on user actions or other dynamic conditions. For example, you might adjust the payload based on which button was clicked, what content is visible, or values pulled from other parts of the page.

This is easy to handle on the server. Here’s what your Razor Page handler might look like:

```csharp
public IActionResult OnPostSubmit(string Message, DateTime SubmittedAt, string UserRole)
{
    // Log the metadata, store the feedback, or trigger different flows
    // based on the user role
    return Content($"<p>Thanks for your feedback! Received at {SubmittedAt} from a {UserRole} user.</p>", "text/html");
}
```

With `hx-vals`, you don’t need to reshape your forms or maintain hidden inputs just to sneak in that extra bit of data. You get a clean, declarative tool that respects the boundary between UI and intent, keeping your forms lightweight and your request payloads purposeful.

This becomes even more powerful when paired with other htmx features like `hx-trigger` and `hx-target`, allowing you to build nuanced, data-rich interactions without a heavy JavaScript framework. You're not just submitting forms anymore, you’re shaping intent-driven messages to your backend.

## Implementing Real-Time Validation with `hx-validate`

Forms are at their best when they respond like a helpful guide, not a strict gatekeeper. One of the most considerable improvements you can make to the user experience is to provide real-time validation, letting users know when something is wrong while they’re filling out the form, not after they hit submit. This kind of instant feedback reduces frustration and helps users complete forms more quickly and confidently. With htmx, this pattern is easy to implement using `hx-validate` alongside `hx-trigger` and server-side checks.

The `hx-validate` attribute tells htmx to use native browser validation before making a request. It works great for basic checks, like whether a required field is filled out or an email address is formatted correctly. But if you want to check something more dynamic, like whether a username is already taken, you’ll want to go a step further and involve the server. This is where htmx really shines, making lightweight, targeted requests for validation while users interact with the form.

Let’s build a real-time username availability checker. You want the server to validate input while the user types, and give immediate feedback if the username is taken. You’ll use `hx-get` with `hx-trigger="keyup changed delay:500ms"` to avoid spamming the server, and return a validation message if needed.

Here’s the Razor Page form:

```html
<form>
    <input type="text"
           name="username"
           placeholder="Choose a username"
           hx-get="/Register?handler=CheckUsername"
           hx-trigger="keyup changed delay:500ms"
           hx-target="#username-validation"
           hx-swap="innerHTML"
           hx-validate />
    <div id="username-validation" class="validation-message"></div>
</form>
```

When the user types in the input, htmx waits for 500ms of inactivity, then fires a `GET` request to the `CheckUsername` handler. The response replaces the content of `#username-validation`, giving feedback directly beneath the input.

On the server side, you can handle it like this:

```csharp
public IActionResult OnGetCheckUsername(string username)
{
    var takenUsernames = new[] { "admin", "user1", "guest" };
    if (string.IsNullOrWhiteSpace(username))
    {
        return Content("<span style='color: red;'>Username is required.</span>", "text/html");
    }
    if (takenUsernames.Contains(username, StringComparer.OrdinalIgnoreCase))
    {
        return Content("<span style='color: red;'>That username is already taken.</span>", "text/html");
    }

    return Content("<span style='color: green;'>Username is available!</span>", "text/html");
}
```

This approach has a few advantages. First, you keep all validation logic on the server, where you can perform database checks or enforce business rules. Second, the user sees changes instantly, helping them fix mistakes early. Third, you keep your Razor Pages and HTML in sync, and there is no need for client-side duplication or complex JavaScript state management.

One best practice is ensuring your validation responses return clean, targeted HTML. Avoid dumping large templates or full layouts into your swap targets. You want the feedback to feel snappy and unobtrusive. You can also style the response based on success or error states, or apply CSS classes dynamically with server-rendered markup.

Real-time validation like this is a slight touch that makes a big difference. When your forms guide users through their tasks instead of punishing them after submission, you build a better experience, and htmx makes that easier than ever.

## Optimizing Form Behavior for Better UX

By now, you've seen how `hx-params`, `hx-vals`, and `hx-validate` each bring a unique advantage to your forms. But where these features really shine is when you use them together. Modern forms often need to be flexible, innovative, and user-friendly, all without relying on a mountain of client-side JavaScript. With htmx and Razor Pages, you can craft interactive forms that behave exactly the way users expect, and do it with clean, server-driven logic.

Consider a scenario where you're building a reusable profile form for both admins and regular users. You might want to exclude certain fields for regular users, include a hidden role value, and perform real-time validation on the username field. This is a perfect case for combining all three htmx enhancements.

Here’s a snippet from such a form:

```html
<form hx-post="/Profile?handler=Save" hx-target="#form-messages" hx-swap="innerHTML">
    <input type="text" name="Username"
           placeholder="Username"
           hx-get="/Profile?handler=CheckUsername"
           hx-trigger="keyup changed delay:500ms"
           hx-target="#username-check"
           hx-swap="innerHTML"
           hx-validate />
    <div id="username-check"></div>

    <input type="email" name="Email" placeholder="Email" />
    <input type="hidden" name="UserId" value="@Model.UserId" />

    <button type="submit"
            hx-vals='{"Role": "@Model.UserRole"}'
            hx-params="not UserId">
        Save
    </button>
</form>

<div id="form-messages"></div>
```

In this example, `hx-validate` ensures the username is checked on the fly. The `hx-params="not UserId"` keeps sensitive identifiers out of the POST body while still retaining context in the view model. Meanwhile, `hx-vals` discreetly injects the user’s role into the submission, which helps control server-side logic without bloating your markup. The form feels lightweight, responsive, and intelligent, and yet it’s all done with declarative attributes.

To make this approach scalable, it’s smart to encapsulate your forms into partials and organize them into components. Create small, focused partial Razor files for each form piece: inputs, validation feedback, and action buttons. This promotes reuse and keeps your UI consistent across the app. When you need to update behavior, tweak validation logic or add a new field, you can do it in one place without hunting through multiple pages.

On the server, validation feedback and success messages should be treated with the same care. Return partial HTML updates that clearly indicate success or failure, and target a dedicated message area using `hx-target`. Avoid redirecting on success unless absolutely necessary. Let users stay where they are, see the result, and move forward naturally.

These optimizations aren't about flashy effects or animations but about getting the UX fundamentals right. They reduce friction, improve clarity, and build trust with your users. With Razor Pages and htmx working together, you're building an interface that feels responsive without the complexity of SPAs or client-heavy codebases.

With forms now operating more intelligently, responding in real time, and submitting only the necessary data, you can handle more complex user interface interactions, such as drag-and-drop functionality. In the next chapter, we will explore how to incorporate intuitive, dynamic movement into your web applications using htmx in conjunction with Hyperscript. This combination will enable users to rearrange elements, trigger actions, and engage with content in a natural and fluid way, all without relying on heavy frontend libraries.