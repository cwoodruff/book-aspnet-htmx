---
order: 20
icon: stack
label: Chap 11 - Building Blocks with HTML Components- Forms and Modal Forms
meta:
title: "Building Blocks with HTML Components: Forms and Modal Forms"
visibility: hidden
---

# Building Blocks with HTML Components: Forms and Modal Forms

In previous chapters, we’ve explored how htmx breathes new life into our Razor Pages by enhancing interactivity without the need for heavy JavaScript frameworks. We’ve connected pages with dynamic partials, layered in real-time updates, and even built reusable UI components. Now, it’s time to focus on a web staple that no application can do without—forms. Whether it’s creating a new account, submitting a support ticket, or updating a product listing, forms are where users interact most directly with your application.

This chapter dives into building clean, responsive, and user-friendly forms using htmx in an ASP.NET Core Razor Pages app. We’ll approach this not just with usability in mind, but also from the angle of maintaining modular, component-based structures that align with the patterns we’ve been building throughout the book. You’ll see how to construct forms that feel seamless and immediate, using htmx to load, submit, and respond to input without full page reloads.

We’ll also take a closer look at modal forms—compact, focused interfaces that pop into view without navigating away from the current page. These can elevate your user experience by reducing friction and making workflows smoother. And with htmx, implementing them is refreshingly straightforward. We'll walk through the key considerations to keep these modals lightweight and performant, while still providing a polished, professional user experience.

By the end of this chapter, you’ll have the confidence to build forms and modal forms that are not only functionally solid but also easy to maintain and integrate into your broader application architecture. You already have the foundation—now let’s add these essential building blocks to your htmx toolkit.

## Smoother Interactions with `hx-post` and Form Enhancements

One of the most common frustrations with traditional HTML forms is the full-page reload that follows a submission. While this behavior has been the norm for decades, it breaks the fluidity of the user experience, especially in modern web applications where users expect seamless interaction. Each reload discards the current page state, flashes the screen, and introduces a noticeable delay. For simple actions like submitting a comment or updating a profile field, this feels like overkill.

This is where htmx steps in with hx-post, a simple yet powerful attribute that replaces traditional form submission with a dynamic request. Instead of submitting the entire page to the server and waiting for a full HTML response, hx-post makes an asynchronous HTTP POST call to the specified endpoint and swaps the result into a target element. With this one attribute, you get smoother UX, less flicker, and more control over what gets updated on the page.

Let’s say we’re building a basic comment form in Razor Pages. Here’s the form markup using htmx:

```html
<form hx-post="/comments" hx-target="#comments-list" hx-swap="beforeend">
    <textarea name="message" rows="4" class="form-control" required></textarea>
    <button type="submit" class="btn btn-primary" hx-disable="true">Post Comment</button>
</form>
```

This form posts to the /comments endpoint when submitted, and whatever HTML is returned will be inserted just before the end of the #comments-list element. No page reload, no scroll jump—just a new comment appearing instantly where it belongs. This feels far more natural for users and avoids jarring context shifts.

We’ve also added hx-disable="true" to the submit button. This attribute prevents users from submitting the form multiple times by disabling the button during the request. It's a small touch that helps maintain clean server-side data and reduces accidental duplicates—something that becomes increasingly important as your forms grow in complexity or involve transactional operations.

On the server side, the handler in the Razor Page’s OnPostAsync method should return a partial view containing just the new comment HTML. For example:

```csharp
public async Task<PartialViewResult> OnPostAsync()
{
    var comment = new Comment { Message = Request.Form["message"], CreatedAt = DateTime.UtcNow };
    _db.Comments.Add(comment);
    await _db.SaveChangesAsync();
    return Partial("_CommentPartial", comment);
}
```

This returns a small, focused HTML fragment rendered from _CommentPartial.cshtml, which represents a single comment block. htmx drops that into the page where the user expects to see it. From the user’s perspective, their comment appears instantly, right after hitting submit—no waiting, no reloading, no wondering if it worked.

This small enhancement makes a big difference. Your users stay in context. They don’t lose their place. And for you as the developer, you get to keep your UI logic modular and focused. As we move forward, we’ll build on this idea with modal forms and error handling, but for now, this setup is already a solid leap forward from the old form-post-refresh cycle.

## Real-Time Feedback and Confirmation with Validation Tools

When it comes to form interactions, validation plays a critical role—not just in protecting your server, but in guiding your users toward successful submissions. In traditional web applications, validation is often a clunky mix of client-side JavaScript and full-page refreshes when something goes wrong. With htmx and ASP.NET Core, we can achieve a far more refined experience by combining real-time validation feedback, dynamic error handling, and confirmation prompts—all without leaving the page.

htmx offers a helpful hx-validate attribute that wires into the browser’s built-in form validation. When used alongside hx-trigger, it allows you to validate user input before sending anything to the server. This keeps the experience responsive and makes it easier to highlight missing or incorrect fields without relying on external JavaScript libraries.

Here’s a basic registration form with htmx validation in place:

```html
<form hx-post="/account/register"
      hx-trigger="submit"
      hx-validate
      hx-target="#form-messages"
      hx-swap="innerHTML">
    <input type="text" name="username" required class="form-control" placeholder="Username" />
    <input type="email" name="email" required class="form-control" placeholder="Email" />
    <input type="password" name="password" required minlength="6" class="form-control" placeholder="Password" />
    <button type="submit" class="btn btn-primary" hx-confirm="Are you sure you want to register with this information?">Register</button>
</form>
```

The hx-validate attribute ensures that the browser’s native validation kicks in before any request is sent. Fields marked required or with other validation attributes like minlength must pass before the form is submitted. This reduces unnecessary server traffic and gives the user instant feedback. Meanwhile, the hx-confirm attribute adds a simple confirmation dialog before the form is actually posted—perfect for actions like registration, deletion, or any irreversible changes.

Even with strong client-side validation, server-side validation is still essential. You can’t trust every request that hits your endpoint. In ASP.NET Core, validation errors are typically returned as part of the ModelState. With htmx, you can return a partial view containing just the validation messages and insert them into the form dynamically.

Here’s an example server-side handler in your Razor Page:

```csharp
public async Task<IActionResult> OnPostAsync()
{
    if (!ModelState.IsValid)
    {
        return Partial("_ValidationErrors", ModelState);
    }
    
    var user = new User { Username = Input.Username, Email = Input.Email };
    await _userManager.CreateAsync(user, Input.Password);
    return Partial("_RegistrationSuccess", user);
}
```

If the model is invalid, the server returns a _ValidationErrors.cshtml partial containing a summary of the issues. The hx-target="#form-messages" in the form ensures these messages appear in the designated spot on the page without reloading or losing the user’s input.

With this combination of hx-validate, hx-confirm, and thoughtful server-side handling, you can build forms that feel responsive, respectful of the user’s time, and safe to use. This approach scales well for other types of forms, too—think billing updates, profile settings, or anything where mistakes need to be caught early. The result is a friendlier, more modern web experience that users will appreciate, all while keeping your Razor Pages clean and maintainable.

## Dynamic Modals and Form Workflows with htmx and Hyperscript

Modals are a familiar part of modern web apps—offering a way to keep users in context while completing small, focused tasks like filling out a form or confirming an action. Traditionally, they require a fair amount of JavaScript to manage their visibility and state. But with htmx and a bit of help from Hyperscript, you can create dynamic, responsive modal forms with surprisingly little effort and no front-end framework overhead.

htmx makes it easy to load modal content on demand, rather than embedding all possible modals in your HTML from the start. This keeps your pages clean and snappy. To get started, all you need is a trigger element that loads the form via htmx into a designated modal container.

Here’s the trigger button to launch a modal for adding a new user:

```html
<button hx-get="/users/create"
        hx-target="#modal-content"
        hx-trigger="click"
        _="on htmx:afterOnLoad add .show to #modal">
    Add New User
</button>
```

The hx-get fetches the Razor Partial View at /users/create, which contains the form. That form is injected into the #modal-content element inside your modal wrapper. The _= attribute uses Hyperscript to add a .show class to the modal after the content is loaded, making it visible. Here's a simple modal container:

```html
<div id="modal" class="modal" _="on click if event.target is #modal remove .show">
    <div class="modal-dialog">
        <div class="modal-content" id="modal-content">
            <!-- Form gets loaded here -->
        </div>
    </div>
</div>
```

Notice the Hyperscript on the #modal div: it closes the modal when the background is clicked by removing the .show class. This keeps the experience clean without needing custom JavaScript. You can enhance the styling with your favorite CSS framework, or roll your own—htmx and Hyperscript don’t get in the way.

Now, let’s look at what happens when the user submits the form inside the modal. The form itself can be a typical Razor Partial, enhanced with htmx to post the data and update the user list dynamically. Here's the form loaded into the modal:

```html
<form hx-post="/users/create"
      hx-target="#user-list"
      hx-swap="beforeend"
      _="on htmx:afterRequest remove .show from #modal">
    <input type="text" name="name" required class="form-control" placeholder="Name" />
    <input type="email" name="email" required class="form-control" placeholder="Email" />
    <button type="submit" class="btn btn-primary">Create User</button>
</form>
```

When the form is submitted, htmx posts it to the server. The server returns a small HTML fragment representing the new user, which is inserted into #user-list using hx-swap="beforeend". Immediately after the request finishes, Hyperscript closes the modal by removing the .show class. The user gets instant feedback, the new data appears in the list, and the UI remains uninterrupted.

On the server side, this is just a Razor Page with an OnPostAsync that returns the HTML for a new user row:

```csharp
public async Task<PartialViewResult> OnPostAsync(UserInputModel input)
{
    var user = new User { Name = input.Name, Email = input.Email };
    _db.Users.Add(user);
    await _db.SaveChangesAsync();
    return Partial("_UserRow", user);
}
```

This kind of modal interaction is powerful because it doesn’t just look nice—it also keeps your app logic decoupled and modular. Each modal pulls in only the content it needs, performs its job, and cleans up after itself. With Hyperscript managing transitions and visibility, and htmx handling the data flow, you end up with a solution that’s elegant and maintainable without being JavaScript-heavy.

As you start layering in more complex modals—editing items, confirmations, multi-step forms—you’ll find this pattern holds up surprisingly well. The real beauty is that your Razor Pages stay focused, your views stay clean, and your users never have to wait for a full page reload to see their actions reflected.

## Polished Interactions and Practical Structure for Form UX

Once your form is working correctly and handling data without page reloads, the next step is smoothing out the rough edges in the user experience. A form that works is good. A form that feels fast, accessible, and intuitive is great. With htmx and ASP.NET Core, you can bring your forms into that “great” category by layering in small, thoughtful touches that reduce friction and improve responsiveness.

Let’s start with loading indicators. When users click a button, they expect some sort of feedback—especially if there’s a delay. htmx provides the hx-indicator attribute to show a visual indicator while a request is in flight. You can attach it to any element, such as a spinner or loading message, and htmx will automatically toggle its visibility based on the request lifecycle.

Here's an example using a spinner on a submit button:

```html
<form hx-post="/users/update"
      hx-target="#user-info"
      hx-indicator="#loading-spinner">
    <input type="text" name="name" required />
    <button type="submit" class="btn btn-primary">
        Save
        <span id="loading-spinner" class="spinner-border spinner-border-sm d-none"></span>
    </button>
</form>
```

The #loading-spinner element starts hidden using d-none. htmx removes the class while the request is active, and adds it back when done. This kind of feedback keeps users informed and reduces the temptation to click buttons multiple times in frustration.

Beyond interactivity, accessibility matters just as much. Dynamic forms should still play nicely with screen readers, keyboard navigation, and other assistive technologies. Make sure that all inputs have proper label tags, use semantic HTML elements, and keep your focus management predictable. If you’re showing a validation message or success notification, consider using aria-live="polite" to alert assistive tech without jarring the user experience.

As you build more forms across your application, reusability becomes essential. Try to think in terms of Razor Partial Views that represent pieces of your form: an input group, a validation summary, a submit button with a spinner. Wrap these in consistent layouts so you don’t duplicate logic. Use tag helpers and model binding conventions in your views to keep your forms strongly typed and aligned with your server-side models.

Here’s an example partial for a labeled input that can be reused across forms:

```html
@model InputFieldModel
<div class="form-group">
    <label asp-for="@Model.Name"></label>
    <input asp-for="@Model.Name" class="form-control" />
    <span asp-validation-for="@Model.Name" class="text-danger"></span>
</div>
```

By splitting your form into components like this, you can compose entire forms quickly while keeping your markup consistent and easy to maintain. It’s a small investment that pays off every time you need to add a new feature or tweak a layout.

Looking ahead, the next chapter builds on the same component-driven mindset but focuses on navigation and layout. We’ll explore tabs and accordions—two classic UI elements that can be made highly interactive with htmx and a touch of Hyperscript. Just like with forms, we’ll keep things lightweight and modular so your users can move around your app quickly, without waiting for page reloads or relying on heavy front-end libraries. If you’ve enjoyed building dynamic forms, you’ll love what’s coming next.