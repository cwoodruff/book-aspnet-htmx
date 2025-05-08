---
order: 13
icon: stack
label: Chap 18 - Drag, Drop, and Dynamism- Creating Interactive UIs with Drag and Drop
meta:
title: "Drag, Drop, and Dynamism: Creating Interactive UIs with Drag and Drop"
visibility: hidden
---

# Drag, Drop, and Dynamism: Creating Interactive UIs with Drag and Drop

Up until now, you've built dynamic pages with responsive forms, snappy updates, and server-powered interactions, all without writing a single line of JavaScript. You’ve tamed modal forms, made lists more dynamic, and even optimized how users navigate with browser history and enhanced URLs. But as our interfaces become more tactile and user expectations grow, the next frontier of interactivity demands a more visceral kind of engagement: drag and drop.

In this chapter, we’re moving beyond clicks and form inputs to explore what it means to give users a true sense of control. Drag-and-drop interfaces feel intuitive and natural, but implementing them in a clean, server-centric way without a JavaScript framework is where things get interesting. We’ll show you how to introduce this kind of rich interactivity using htmx, Razor Pages, and a few creative techniques.

You’ll learn how to track user intent through movement, update the UI responsively, and even persist those interactions back to the server, all without leaving behind the principles that have guided this book so far: simplicity, clarity, and a server-first mindset. Whether it’s reordering a list, assigning items to groups, or creating visual workflows, this chapter gives you the building blocks to make drag-and-drop interfaces not just possible, but elegant.

Before we move into performance optimization and request caching in the next chapter, this is your chance to master a more immersive layer of interaction. It’s about bringing your UI to life, one drag at a time.

## Understanding the Role of Drag and Drop in Interactive UIs

Drag and drop isn’t just about flair, it’s about flow. When users can physically move things around on the screen, they feel more in control. That kind of interaction boosts usability by reducing friction and making the interface feel more intuitive. Whether it’s dragging a task to a new column or rearranging a playlist, it maps digital actions to real-world behavior. It’s tactile, direct, and satisfying when done right.

Traditionally, implementing drag-and-drop in web applications meant reaching for a JavaScript library. From jQuery UI in the early days to full-blown integrations with libraries like Sortable.js or the HTML5 Drag and Drop API, developers often had to build complex client-side logic. These tools got the job done, but they brought along extra JavaScript, increased cognitive load, and a need to tightly coordinate client-side state with server-side reality.

With htmx and Hyperscript, we can approach this challenge from a different angle that aligns with the philosophy you’ve embraced throughout this book. Instead of building a miniature SPA inside your Razor Pages app, you can let the server remain the source of truth and use simple HTML attributes to handle drag interactions. Hyperscript enhances this by letting you write logic directly in your markup in a more declarative, readable style.

Let’s say you’re building a kanban board. Instead of wiring up JavaScript events for dragstart, dragover, drop, and all the event bubbling madness that comes with it, you can use Hyperscript to handle the interaction locally and use htmx to notify the server of changes. For example, dragging a task card to a new column might look like this:

```html
<div class="kanban-column" hx-post="/Tasks/Move" hx-vals="js:{ taskId: draggedTaskId, newColumn: 'in-progress' }"
     _="on drop 
         set draggedTaskId to event.dataTransfer.getData('text/plain')
         then trigger send">
    <h3>In Progress</h3>
    <div class="task-list" ondragover="event.preventDefault()" ondrop="event.preventDefault()">
        <!-- Tasks go here -->
    </div>
</div>
```

Here, we intercept the drop event, grab the ID of the dragged task using the browser’s drag data store, and send a `POST` request using `hx-post`. The Hyperscript `_=` syntax keeps the logic tight and close to the markup, and the server can handle updating the database and rerendering the updated kanban column.

We follow a similar model for sortable lists, like reordering items in a playlist or prioritizing a task list. Instead of updating the entire client-side state via JavaScript, you capture the new order and send it back to the server for persistence. Let’s say we want to let users rearrange items in a list and save the new order:

```html
<ul id="sortable-list">
    <li draggable="true" data-id="1" 
        ondragstart="event.dataTransfer.setData('text/plain', '1')">Item 1</li>
    <li draggable="true" data-id="2" 
        ondragstart="event.dataTransfer.setData('text/plain', '2')">Item 2</li>
    <li draggable="true" data-id="3" 
        ondragstart="event.dataTransfer.setData('text/plain', '3')">Item 3</li>
</ul>

<div hx-post="/List/UpdateOrder" hx-vals="js:{ order: newOrder }"
     _="on drop
            set draggedId to event.dataTransfer.getData('text/plain')
            set newOrder to getUpdatedOrder(draggedId, event.target)
            then trigger send">
</div>
```

The `getUpdatedOrder` function (defined in a separate script or inline Hyperscript helper) calculates the new order based on where the item was dropped. Again, the key insight is that the actual state lives and updates on the server, not in a sprawling JavaScript object tree.

In inventory management tools, drag and drop can help shift items between warehouses, update storage bins, or assemble packages. You might have draggable inventory items and droppable zones for categories or destinations. The interaction remains the same: detect what’s being dragged, detect where it lands, then inform the server with a lightweight, declarative request.

What’s remarkable here is how little JavaScript you actually need. htmx and Hyperscript work with native browser capabilities to make drag-and-drop both approachable and powerful. You get to keep your UI server-driven, stateless, where possible, and easy to reason about. And best of all, you’re not wrestling with state synchronization bugs at 2 AM.

So yes, drag and drop is traditionally a "JavaScript thing," but in this new world of server-enhanced HTML, it's a server-thing again with just enough interactivity layered on top. In the next section, we’ll start implementing a working drag-and-drop list that persists changes to the backend. It's easier than you think.

## Building the Markup: Making Elements Draggable and Droppable

Now that we understand the value of drag-and-drop in building more engaging UIs, it’s time to roll up our sleeves and build the actual HTML. Fortunately, making elements draggable in the browser is surprisingly straightforward. It starts with the humble `draggable="true"` attribute. Add that to any element, say, a task card in a to-do list, and the browser will treat it as a draggable item.

But native drag-and-drop is only half the battle. The real challenge lies in coordinating the drag events and making something happen when the item is dropped. That’s where Hyperscript steps in to simplify the chaos. Instead of writing JavaScript to register event listeners and fiddling with `event.dataTransfer`, we’ll embed concise logic right in the HTML markup where it belongs.

Let’s look at a simple example: a task list where each task can be dragged to a different status column, from “To Do” to “In Progress.” First, here’s a basic draggable item:

```html
<div class="task" draggable="true"
     data-task-id="42"
     _="on dragstart
          set dataTransfer = event.dataTransfer
          call dataTransfer.setData('text/plain', me.dataset.taskId)">
    Write Chapter Introduction
</div>
```

When the drag starts, we grab the browser’s `dataTransfer` object and stash the task ID using Hyperscript. This means that later, when we drop the element onto a new area, we can recover that task ID and take action.

Now we need a place to drop it. Suppose we have a container for the “In Progress” column. We’ll wire it up to respond to a drop and trigger a server request using htmx:

```html
<div class="column in-progress"
     hx-post="/Tasks/Move"
     hx-vals="js:{ taskId: draggedTaskId, newStatus: 'in-progress' }"
     _="on dragover
           call event.preventDefault()
        on drop
           set draggedTaskId to event.dataTransfer.getData('text/plain')
           then trigger send">
    <h3>In Progress</h3>
    <div class="task-list">
        <!-- Dropped tasks appear here -->
    </div>
</div>
```

The dragover handler prevents the drop by preventing the default browser behavior. Then, when the task is dropped, we retrieve the ID we stored earlier, assign it to `draggedTaskId`, and let htmx do the heavy lifting with a `POST` request to the server. Razor Pages handles that cleanly via an `OnPostMove` handler.

Here’s what the handler might look like in your Razor Page model:

```csharp
public class TasksModel : PageModel
{
    [BindProperty] public int TaskId { get; set; }
    [BindProperty] public string NewStatus { get; set; }

    public IActionResult OnPostMove()
    {
        // Update the task in the database
        TaskService.UpdateStatus(TaskId, NewStatus);
        return Partial("_TaskList", TaskService.GetTasksByStatus(NewStatus));
    }
}
```

By using `[BindProperty]`, Razor Pages automatically wires up the `POST` data to your properties. This keeps your backend tidy and easy to test. When the status update succeeds, we return an updated task list partial for the relevant column.

This setup scales nicely. Each task is its draggable element. Each column is a reusable drop zone. Hyperscript keeps the drag logic declarative and readable, while htmx manages the server sync. And because it all runs inside your existing Razor Pages structure, you don’t have to fight a client-side framework to get it done.

In the next section, we’ll make this even more dynamic by updating the UI to reflect task movement visually and improving accessibility and keyboard support. But for now, you’ve got a working drag-and-drop list powered by Razor Pages, htmx, and a touch of Hyperscript.

## Syncing Drag Events with the Server Using htmx

Dragging elements around the page is fun and interactive, but it has to mean something at some point. Moving a task, reordering a playlist, or assigning a ticket to a new category isn’t just about the visuals; it’s about reflecting those changes in your data. That’s where htmx shines. Syncing drag-and-drop actions with your server keeps the UI honest and the backend authoritative.

To do this, you’ll typically need to pass some metadata to the server on what was dragged, where it was dropped, and possibly what else changed as a result. htmx gives you a couple of great tools for that: `hx-vals` lets you pass values directly from client to server in a clean way, and `data-*` attributes can be used to decorate HTML elements with useful identifiers that Hyperscript can grab during the interaction.

Let’s say we have a sortable list of items and want to save the new order after a drag-and-drop operation. Each item will be draggable, and we’ll use Hyperscript to capture the reorder event. Here’s a basic markup setup for that:

```html
<ul id="task-list">
  <li draggable="true" data-id="1"
      _="on dragstart call event.dataTransfer.setData('text/plain', me.dataset.id)">Task A</li>
  <li draggable="true" data-id="2"
      _="on dragstart call event.dataTransfer.setData('text/plain', me.dataset.id)">Task B</li>
  <li draggable="true" data-id="3"
      _="on dragstart call event.dataTransfer.setData('text/plain', me.dataset.id)">Task C</li>
</ul>
```

Now we need a drop target to detect where the item was dropped and capture the new order. We can use Hyperscript to reconstruct the list order and htmx to submit it via `hx-post`. Here's one way to handle the drop logic:

```html
<div hx-post="/Tasks/Reorder"
     hx-vals="js:{ order: newOrder }"
     hx-target="#task-list"
     hx-swap="outerHTML"
     _="on drop
           call event.preventDefault()
           set draggedId to event.dataTransfer.getData('text/plain')
           set newOrder to getUpdatedOrder(draggedId, event.target)
           then trigger send">
</div>
```

The magic here happens in the `getUpdatedOrder` function, which you'd define in a small inline script or helper library. After the drop, it determines the new order of items and returns it as an array of IDs. This array is passed to the server using `hx-vals`, allowing your Razor Page handler to receive it as a bound model.

On the backend, you might handle that like this:

```csharp
public class TasksModel : PageModel
{
    [BindProperty]
    public List<int> Order { get; set; }

    public IActionResult OnPostReorder()
    {
        TaskService.UpdateTaskOrder(Order);
        var tasks = TaskService.GetAllTasksOrdered();
        return Partial("_TaskList", tasks);
    }
}
```

This is where Razor Pages makes your life easy. When a new order comes in, your service layer updates the database, and the server returns a fresh rendering of the list, all without a full-page reload. Thanks to `hx-target` and `hx-swap`, only the relevant part of the page is updated, keeping the experience fast and fluid.

With this setup, drag-and-drop interactions become more than just eye candy. They’re fully wired into your application’s state. And because the source of truth remains on the server, your UI stays in sync even when users refresh or return later. You’ve now built a responsive, dynamic list that looks good and stays accurate and persistent, without relying on a mountain of JavaScript.

## Enhancing UX with Hyperscript

Drag-and-drop may power the functionality, but it’s the little details that make it feel smooth and polished. Users expect feedback, visual hints telling where they can drop an item, shadows that follow their cursor, and animations that show elements shifting into place. Without these cues, drag-and-drop can feel janky or even broken. Hyperscript gives us a declarative, elegant way to add that feedback without reaching for a full animation library or writing verbose JavaScript.

Let’s begin with visual cues. A drop zone should light up when a draggable item enters it and reset when the item leaves. Using Hyperscript, you can easily toggle classes to add a highlight. Here’s a basic example of how a kanban column might behave when a card is dragged over it:

```html
<div class="kanban-column"
     _="on dragenter add .highlight to me
        on dragleave remove .highlight from me
        on drop remove .highlight from me">
    <h3>In Progress</h3>
    <div class="task-list">
        <!-- Dropped tasks appear here -->
    </div>
</div>
```

This approach makes the column glow or change color, providing clear feedback to the user that the area is droppable. The highlight class can be styled however you like in CSS, maybe a border or background color change, to indicate it's a valid target.

Another layer of user feedback is the drag shadow, the visual representation of the dragged item as it moves. While the browser provides a default drag image, you can override it or supplement it with a ghosted version using Hyperscript. For example, you could clone the dragged element and style it semi-transparent as it follows the cursor. Hyperscript isn’t directly tied into pointer tracking, but you can simulate this behavior for basic cases by creating visual overlays.

However, what sets Hyperscript apart is how easily it supports animation when elements move. For example, reordering cards in a kanban column can feel abrupt if items “jump” into new positions. To smooth it out, you can animate their movement. Here's a simplified snippet that animates a task fading out before being moved:

```html
<div class="task" draggable="true" data-id="42"
     _="on dragstart call event.dataTransfer.setData('text/plain', me.dataset.id)
        on leave transition opacity to 0 over 200ms
        then remove me">
    Review PR #82
</div>
```

This transition gives users a subtle cue that the task is leaving its position. You can use a corresponding fade-in animation when it reappears in the new location. You could even take it further with Hyperscript's move behavior to animate position shifts when items reorder:

```html
<div class="task-list"
     _="on htmx:afterSettle for .task
           move me smoothly over 300ms">
</div>
```

This line ensures that once htmx swaps in the updated task list, each task item is gently transitioned to its new position. There are no jump cuts or flicker; it is just clean motion that reinforces user intent.

With just a handful of Hyperscript directives, you’ve layered in a richer experience, highlighted targets, graceful entries and exits, and motion that feels like something out of a polished frontend framework. The difference this makes to users is enormous, and you didn’t need to write or manage a single JavaScript component.

As you continue to build drag-and-drop interfaces with Razor Pages and htmx, keep leaning on Hyperscript to elevate the user experience. It’s declarative and expressive and fits beautifully into the server-first architecture we’ve embraced throughout this book. Now, your Kanban board doesn’t just work; it feels right.

## Testing and Debugging Drag-and-Drop Features

Testing drag-and-drop functionality can feel a bit like chasing shadows; things happen fast, and there’s no form submission button to press when something goes wrong. But whether you're manually testing or setting up automation, a clear strategy will save you time and frustration. Start by using your browser's built-in developer tools. Open the console and network tab, and pay close attention to htmx request headers, the body payload, and any server-side responses. You’ll often catch issues by inspecting what was, or wasn’t, sent during a drop.

During manual testing, simulate dragging elements across their boundaries slowly and intentionally. Try edge cases like dropping an item on itself or dragging outside of any defined drop zone. If your drop logic uses Hyperscript to extract values from data-* attributes or the `dataTransfer` object, ensure those values are present. A single typo in `data-task-id` or `hx-vals` can cause a subtle failure without immediate feedback unless you’re watching the console closely.

Automated testing helps lock in behaviors as your app grows. Both Playwright and Cypress support drag-and-drop testing with some workarounds. These tools don’t always natively trigger browser-level dragstart, dragover, and drop in a perfect sequence, so you’ll need to simulate those events or use custom helpers. In Playwright, for example, you might manually dispatch events using JavaScript execution within the test:

```csharp
await page.EvalOnSelectorAsync(".task[data-id='1']", @"(el) => {
    const event = new DragEvent('dragstart', {
        dataTransfer: new DataTransfer()
    });
    el.dispatchEvent(event);
}");
```

Cypress users can install libraries like @4tw/cypress-drag-drop to simplify things, but still should validate what gets posted back to the server via `cy.intercept` and examine network calls just like a human tester would.

Some of the most common issues with drag-and-drop come down to event propagation. For instance, if your dragover or drop event handlers don’t explicitly call `event.preventDefault()`, your drop event may never fire. If you’re passing values via `hx-vals` or dataTransfer, double-check that Hyperscript logic is executing in the correct order. A missed then or incorrectly scoped variable might mean you’re sending a blank taskId even though the drag visually succeeded.

Another pitfall is a missing or mismatched state on the server. If your drag interaction instantly updates the UI but the backend doesn’t properly reflect that change, you’ll get inconsistencies after a page reload. Avoid relying entirely on client-side assumptions. Keep the server as the source of truth and validate updates against real data. Razor Pages make it easy to query your state and return a fresh partial view to confirm the update.

Finally, be mindful of how your drag-and-drop state interacts with page navigation. In the next chapter, we’ll explore `hx-history` and `hx-history-elt`, two htmx features designed to preserve state and user context when navigating or triggering updates that would otherwise reset the view. These tools help you make drag-and-drop interactions not just reactive but resilient, keeping the UI fast and consistent even as users move between views or reload the page.

Getting drag and drop right means testing it like a user, debugging it like a detective, and always confirming the server saw what you think it saw. Once you're confident in those pieces, you’ll have an interaction that feels effortless, and behaves just as reliably as your standard forms.