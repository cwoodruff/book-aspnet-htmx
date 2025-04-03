---
order: 17
icon: stack
label: Chap 14 - Building Blocks with HTML Components- Buttons and Action Links
meta:
title: "Building Blocks with HTML Components: Buttons and Action Links"
visibility: hidden
---

# Building Blocks with HTML Components: Buttons and Action Links

Buttons and links may seem like the simplest elements in your HTML toolbox, but in an htmx-powered Razor Pages application, they unlock a whole world of interactivity. In this chapter, we’ll take a focused look at how these humble components can become powerful conduits for user-driven behavior—without requiring a single line of custom JavaScript. Whether it’s triggering actions, prompting confirmations, or dynamically swapping content, buttons and action links form the bridge between your users and your server.

By now, you've built dynamic forms, interactive lists, and even modal popups using htmx. But as we step into this chapter, the spotlight is on how to make user interaction feel intuitive and immediate. Buttons and links are often the primary way users engage with your app, so it's crucial they behave as expected—with just the right amount of feedback and control. We'll explore how `hx-trigger` gives you granular control over when and how actions fire, and how prompts and confirmations can guide user intent without being intrusive.

The beauty of htmx is that you can enrich these components without leaving HTML. Want a delete button that asks for confirmation before proceeding? Easy. Need a link that fetches and renders content inline? No sweat. With the right combination of attributes and server responses, your application starts to feel dynamic and responsive, all while keeping the logic server-side and your markup clean.

By the end of this chapter, you’ll see that buttons and links aren’t just navigational or form-related—they’re versatile building blocks for almost any kind of interaction. You’ll be able to confidently build user flows that feel modern and fluid, all while staying true to the Razor Pages model. Let’s dig into the essential ways htmx can elevate even the most basic HTML components into smart, interactive experiences.

## Smarter Clicks: Using `hx-trigger` and `hx-prompt` for Responsive Interactions

Buttons and links might be the smallest parts of your UI, but when wired up correctly, they can drastically improve the usability of your application. In traditional web apps, these elements often trigger full page reloads or are wired up with bloated JavaScript just to perform a simple task. With htmx, however, you can make interactions feel smooth, fast, and responsive using a couple of attributes—most notably `hx-trigger` and `hx-prompt`.

Let’s start with `hx-trigger`. By default, htmx will fire a request when the triggering element is clicked. But that’s just the beginning. With `hx-trigger`, you can precisely define when an interaction should happen—on click, hover, focus, or even custom JavaScript events. You can also delay requests or require multiple events. This gives you flexibility to fine-tune how your app responds to user behavior.

Imagine a list of items with a “Rename” button next to each one. Instead of navigating to a new page or opening a modal, we can prompt the user for a new name right there in the list. This is where `hx-prompt` shines. With a single attribute, you can collect user input from a simple dialog and pass that data to the server without writing a single line of JavaScript.

Here’s what the button might look like in Razor syntax:

```html
<button
    hx-post="/Items/Rename"
    hx-include="[data-id]"
    hx-prompt="Enter a new name:"
    hx-trigger="click"
    data-id="@item.Id">
    Rename
</button>
```

In this snippet, when the user clicks the button, htmx opens a browser prompt with the message “Enter a new name:”. The value the user enters is sent as the `prompt` parameter in the POST request to `/Items/Rename`. We’re also including the item ID using `hx-include` and a `data-id` attribute to help the server know which item is being renamed.

On the server side, the Razor Page handler might look like this:

```csharp
public class ItemsModel : PageModel
{
    private readonly IItemService _itemService;

    public ItemsModel(IItemService itemService)
    {
        _itemService = itemService;
    }

    public IActionResult OnPostRename(int id, string prompt)
    {
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            _itemService.RenameItem(id, prompt);
        }

        var updatedItem = _itemService.GetItemById(id);
        return Partial("_ItemRow", updatedItem);
    }
}
```

This method receives both the item ID and the user’s input from the prompt. After updating the item, it returns a partial view of the updated row to replace the existing one on the page. This approach makes editing feel instant and keeps users focused on the task at hand.

Here's what the `_ItemRow.cshtml` partial might look like:

```html
<tr id="item-@Model.Id">
    <td>@Model.Name</td>
    <td>
        <button 
            hx-post="/Items/Rename" 
            hx-include="[data-id]" 
            hx-prompt="Enter a new name:" 
            hx-trigger="click" 
            data-id="@Model.Id">
            Rename
        </button>
    </td>
</tr>
```

When used thoughtfully, `hx-trigger` and `hx-prompt` allow you to build interactive features that feel elegant and efficient. They eliminate the need for custom JavaScript just to ask a question or handle a click. And because the server is still in charge of processing the input and returning updated HTML, you maintain a clean, testable, and maintainable backend.

As you build more complex apps, you’ll start to appreciate how these simple attributes give you the tools to build highly usable interfaces. Buttons and links are no longer just passive UI controls—they become responsive, interactive features that improve the overall flow of your application.

## Confirm Before You Commit: Handling Destructive Actions with Confidence

When you're building any application that lets users create, update, or delete records, it's your job to make sure nothing gets wiped out by accident. Destructive actions—like deleting a user, clearing a list, or resetting settings—should never happen without giving the user a chance to reconsider. That’s where confirmation dialogs come in. They act as a simple safety net, preventing irreversible changes caused by an accidental click.

htmx makes it incredibly easy to add basic confirmations with the `hx-confirm` attribute. Just add it to your button or link, and htmx will show a browser-native dialog box before sending the request. If the user confirms, the request proceeds as usual; if not, it’s canceled. This is perfect for quick validations where you don’t need anything fancy.

Here’s an example of a delete button with a built-in confirmation:

```html
<button
    hx-delete="/Items/Delete/123"
    hx-target="#item-123"
    hx-swap="outerHTML swap:1s"
    hx-confirm="Are you sure you want to delete this item?">
    Delete
</button>
```

In this case, when the button is clicked, the user sees a simple browser prompt. If they confirm, the DELETE request is sent to `/Items/Delete/123`, and the matching row (`#item-123`) is removed from the page using `outerHTML`. The `swap:1s` adds a fade-out transition, just for a little visual polish.

For more complex situations—like when you want a custom-styled confirmation dialog instead of the default browser alert—you can build your own modal using htmx and partials. Instead of executing the delete immediately, you first load a confirmation UI into a modal container.

First, here’s the delete link that triggers the confirmation:

```html
<a
    hx-get="/Items/ConfirmDelete/123"
    hx-target="#modal"
    hx-trigger="click">
    Delete
</a>
<div id="modal"></div>
```

Then, your `/Items/ConfirmDelete` endpoint returns a partial view that contains a confirmation dialog:

```html
<div class="modal">
    <p>Are you sure you want to delete <strong>@Model.Name</strong>?</p>
    <button 
        hx-delete="/Items/Delete/@Model.Id" 
        hx-target="#item-@Model.Id" 
        hx-swap="outerHTML swap:1s" 
        _="on click remove closest .modal then send this request">
        Confirm
    </button>
    <button _="on click remove closest .modal">Cancel</button>
</div>
```

This custom modal gives you full control over the content, styling, and behavior. The use of Hyperscript in the `_=` attributes lets you remove the modal from the DOM after either action, without any JavaScript files involved.

On the backend, the `OnDelete` handler is straightforward:

```csharp
public IActionResult OnDelete(int id)
{
    _itemService.Delete(id);
    return new EmptyResult(); // Nothing to render, just remove the item from the page
}
```

By offering both quick `hx-confirm` prompts and more nuanced modal confirmations, you give users a sense of control and reduce the risk of mistakes. These techniques are easy to implement but go a long way in building user trust. After all, nobody likes losing work because of a misclick. A second chance—whether via a browser alert or a custom modal—is always worth the extra line of code.

## Responsive and Reliable: Streamlining Button and Link Behavior

We’ve all experienced the frustration of clicking a button and wondering if it worked—especially when there’s no immediate feedback. Even worse is when the action gets triggered multiple times because the user keeps clicking. These kinds of glitches erode trust in your application. The good news is that htmx gives you simple tools to eliminate these issues and optimize button and link interactions so they feel crisp, responsive, and reliable.

One of the best tools in your toolbox is `hx-disable`. This attribute automatically disables the triggering element during the request, which prevents duplicate submissions. It’s especially useful for forms, delete buttons, or anything that hits the server. Once the request completes, the button is re-enabled automatically. You don’t need to write JavaScript to manage this state—it just works.

Here’s an example using `hx-disable` in a Razor Page:

```html
<form 
    hx-post="/Items/Create" 
    hx-target="#items-list" 
    hx-swap="beforeend" 
    hx-disable>
    <input type="text" name="name" required />
    <button type="submit">Add Item</button>
</form>
```

In this form, when the user clicks “Add Item,” htmx disables the button until the request is complete. This prevents the form from being submitted multiple times and provides a smoother experience. You can also pair this with `hx-indicator` in the next chapter for visual loading cues, but even on its own, disabling the button during the async operation is a solid UX improvement.

If you’re building interactive features repeatedly—like edit or delete buttons across rows in a table—it makes sense to turn those into reusable components. Razor partials are perfect for this. You can create a `_DeleteButton.cshtml` partial that accepts a model with the URL, display name, and ID, and centralize your behavior there. That way, changes to interaction logic, styles, or attributes can be made in one place, and reused wherever needed.

Here’s a basic example of a reusable delete button component:

```html
@model DeleteButtonModel

<button
    hx-delete="@Model.Url"
    hx-target="#item-@Model.Id"
    hx-swap="outerHTML swap:1s"
    hx-confirm="Delete @Model.Name?"
    hx-disable>
    Delete
</button>
```

When you use this in a table:

```html
<partial name="_DeleteButton" model="new DeleteButtonModel { Url = $"/Items/Delete/{item.Id}", Id = item.Id, Name = item.Name }" />
```

You keep your views clean and your logic consistent across the board. This pattern applies equally well to links, confirmation modals, or inline editing components. It makes your app easier to maintain and scale as features grow.

Optimizing interactions isn’t just about performance—it’s about user confidence. Disabling buttons during operations and reusing consistent UI patterns helps users trust that what they clicked actually worked. And when paired with good feedback (which we’ll dive into next), you can elevate the overall feel of your application.

In the next chapter, we’ll focus on giving users clear visual cues by integrating `hx-indicator` and `hx-preserve`. You’ll learn how to show spinners during requests, maintain UI state between swaps, and ensure users always know what’s happening behind the scenes. With the combination of interaction control and visual feedback, you’ll be building UI that feels fast, polished, and reliable.