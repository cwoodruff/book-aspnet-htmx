---
order: 27
icon: stack
label: Chap 4 - Understanding htmx Commands
meta:
title: "Understanding htmx Commands"
---

# Understanding htmx Commands

Chapter 3 introduced the basics of htmx with `hx-get`, `hx-post`, `hx-target`, and `hx-swap`. You built simple interactions and learned how htmx sends requests and updates the DOM. Now it is time to go deeper. This chapter expands your command vocabulary with `hx-put`, `hx-patch`, `hx-delete`, and advanced targeting techniques. You will build complete CRUD operations, handle complex swap scenarios, and learn patterns that scale to real applications.

By the end of this chapter, you will understand what each command does and when to choose one over another. You will have working code for create, read, update, and delete operations. You will know how to update multiple page elements from a single response. These are the building blocks for the interactive dashboards, admin panels, and data-driven applications you will build in later chapters.

## The Complete htmx Command Set

htmx provides HTTP method attributes that map directly to RESTful operations:

| Attribute | HTTP Method | Typical Use |
|-----------|-------------|-------------|
| `hx-get` | GET | Retrieve and display data |
| `hx-post` | POST | Create new resources, submit forms |
| `hx-put` | PUT | Replace an existing resource entirely |
| `hx-patch` | PATCH | Partially update an existing resource |
| `hx-delete` | DELETE | Remove a resource |

Each attribute takes a URL as its value. When the element is triggered (by default, on click for buttons and links, on submit for forms), htmx sends the corresponding HTTP request to that URL.

## Building a Complete CRUD Example

Theory only takes you so far. Let us build a working task list that demonstrates all five HTTP methods. This example will show you how htmx commands work together in a realistic scenario.

### Project Setup

Create a new Razor Pages project or add these files to an existing one. We will need a model, a page, and several partial views.

**Models/TaskItem.cs**

```csharp
namespace YourApp.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
}
```

**Services/TaskService.cs**

For simplicity, we will use an in-memory store. In a real application, this would be a database.

```csharp
namespace YourApp.Services;

using YourApp.Models;

public class TaskService
{
    private static readonly List<TaskItem> Tasks = new()
    {
        new TaskItem { Id = 1, Title = "Learn htmx basics", IsComplete = true },
        new TaskItem { Id = 2, Title = "Build CRUD operations", IsComplete = false },
        new TaskItem { Id = 3, Title = "Master advanced patterns", IsComplete = false }
    };
    
    private static int _nextId = 4;

    public List<TaskItem> GetAll() => Tasks.ToList();
    
    public TaskItem? GetById(int id) => Tasks.FirstOrDefault(t => t.Id == id);
    
    public TaskItem Create(string title)
    {
        var task = new TaskItem { Id = _nextId++, Title = title };
        Tasks.Add(task);
        return task;
    }
    
    public TaskItem? Update(int id, string title)
    {
        var task = GetById(id);
        if (task != null)
        {
            task.Title = title;
        }
        return task;
    }
    
    public TaskItem? ToggleComplete(int id)
    {
        var task = GetById(id);
        if (task != null)
        {
            task.IsComplete = !task.IsComplete;
        }
        return task;
    }
    
    public bool Delete(int id)
    {
        var task = GetById(id);
        if (task != null)
        {
            Tasks.Remove(task);
            return true;
        }
        return false;
    }
}
```

Register the service in `Program.cs`:

```csharp
builder.Services.AddSingleton<TaskService>();
```

### The Main Page

**Pages/Tasks.cshtml**

```html
@page
@model TasksModel
@{
    ViewData["Title"] = "Task Manager";
}

<h1>Task Manager</h1>

<section>
    <h2>Add New Task</h2>
    <form hx-post="/Tasks?handler=Create" 
          hx-target="#task-list" 
          hx-swap="beforeend"
          hx-on::after-request="this.reset()">
        @Html.AntiForgeryToken()
        <input type="text" name="title" required placeholder="Enter task title" />
        <button type="submit">Add Task</button>
    </form>
</section>

<section>
    <h2>Your Tasks</h2>
    <div id="task-list">
        @foreach (var task in Model.Tasks)
        {
            <partial name="_TaskItem" model="task" />
        }
    </div>
</section>

<div id="edit-modal" class="modal" style="display: none;">
    <!-- Edit form loads here -->
</div>
```

**Pages/Tasks.cshtml.cs**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YourApp.Models;
using YourApp.Services;

public class TasksModel : PageModel
{
    private readonly TaskService _taskService;
    
    public List<TaskItem> Tasks { get; set; } = new();

    public TasksModel(TaskService taskService)
    {
        _taskService = taskService;
    }

    public void OnGet()
    {
        Tasks = _taskService.GetAll();
    }

    // CREATE: hx-post
    public IActionResult OnPostCreate(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Content("<p class=\"error\">Title is required</p>", "text/html");
        }
        
        var task = _taskService.Create(title);
        return Partial("_TaskItem", task);
    }

    // READ: hx-get (load edit form)
    public IActionResult OnGetEdit(int id)
    {
        var task = _taskService.GetById(id);
        if (task == null)
        {
            return Content("<p class=\"error\">Task not found</p>", "text/html");
        }
        
        return Partial("_TaskEditForm", task);
    }

    // UPDATE: hx-put (full update)
    public IActionResult OnPutUpdate(int id, string title)
    {
        var task = _taskService.Update(id, title);
        if (task == null)
        {
            return Content("<p class=\"error\">Task not found</p>", "text/html");
        }
        
        return Partial("_TaskItem", task);
    }

    // PARTIAL UPDATE: hx-patch (toggle complete)
    public IActionResult OnPatchToggle(int id)
    {
        var task = _taskService.ToggleComplete(id);
        if (task == null)
        {
            return Content("<p class=\"error\">Task not found</p>", "text/html");
        }
        
        return Partial("_TaskItem", task);
    }

    // DELETE: hx-delete
    public IActionResult OnDeleteRemove(int id)
    {
        var success = _taskService.Delete(id);
        if (!success)
        {
            return Content("<p class=\"error\">Task not found</p>", "text/html");
        }
        
        // Return empty content to remove the element
        return Content("", "text/html");
    }
}
```

### Partial Views

**Pages/Shared/_TaskItem.cshtml**

```html
@model YourApp.Models.TaskItem

<div id="task-@Model.Id" class="task-item @(Model.IsComplete ? "complete" : "")">
    <span class="task-title">@Model.Title</span>
    
    <div class="task-actions">
        <button hx-patch="/Tasks?handler=Toggle&amp;id=@Model.Id"
                hx-target="#task-@Model.Id"
                hx-swap="outerHTML">
            @(Model.IsComplete ? "Undo" : "Complete")
        </button>
        
        <button hx-get="/Tasks?handler=Edit&amp;id=@Model.Id"
                hx-target="#edit-modal"
                hx-swap="innerHTML"
                onclick="document.getElementById('edit-modal').style.display='block'">
            Edit
        </button>
        
        <button hx-delete="/Tasks?handler=Remove&amp;id=@Model.Id"
                hx-target="#task-@Model.Id"
                hx-swap="outerHTML"
                hx-confirm="Delete this task?">
            Delete
        </button>
    </div>
</div>
```

**Pages/Shared/_TaskEditForm.cshtml**

```html
@model YourApp.Models.TaskItem

<div class="edit-form">
    <h3>Edit Task</h3>
    <form hx-put="/Tasks?handler=Update&amp;id=@Model.Id"
          hx-target="#task-@Model.Id"
          hx-swap="outerHTML"
          hx-on::after-request="document.getElementById('edit-modal').style.display='none'">
        @Html.AntiForgeryToken()
        <input type="text" name="title" value="@Model.Title" required />
        <button type="submit">Save</button>
        <button type="button" onclick="document.getElementById('edit-modal').style.display='none'">
            Cancel
        </button>
    </form>
</div>
```

### Configuring Anti-Forgery Tokens for All Methods

ASP.NET Core requires anti-forgery tokens for POST, PUT, PATCH, and DELETE requests. Add this script to your `_Layout.cshtml`:

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

For pages that use `hx-delete`, `hx-put`, or `hx-patch` outside of forms, include a hidden token somewhere on the page:

```html
@Html.AntiForgeryToken()
```

### Understanding Each Operation

Let us examine what each htmx command does in this example.

**CREATE with `hx-post`**

```html
<form hx-post="/Tasks?handler=Create" 
      hx-target="#task-list" 
      hx-swap="beforeend">
```

When submitted, this form sends a POST request to `OnPostCreate()`. The handler creates a new task and returns the `_TaskItem` partial. The `hx-swap="beforeend"` directive appends the new task to the end of the list rather than replacing the entire list.

The `hx-on::after-request="this.reset()"` attribute clears the form after successful submission, ready for the next entry.

**READ with `hx-get`**

```html
<button hx-get="/Tasks?handler=Edit&amp;id=@Model.Id"
        hx-target="#edit-modal"
        hx-swap="innerHTML">
    Edit
</button>
```

Clicking Edit sends a GET request to `OnGetEdit()`, which returns the edit form partial. The form loads into the modal div. This pattern separates the read operation (loading the form) from the update operation (submitting changes).

**UPDATE with `hx-put`**

```html
<form hx-put="/Tasks?handler=Update&amp;id=@Model.Id"
      hx-target="#task-@Model.Id"
      hx-swap="outerHTML">
```

The edit form uses PUT because we are replacing the entire task resource. The handler returns an updated `_TaskItem` partial, which replaces the old task element. Using `outerHTML` ensures the entire task div gets replaced, including its id attribute.

**PARTIAL UPDATE with `hx-patch`**

```html
<button hx-patch="/Tasks?handler=Toggle&amp;id=@Model.Id"
        hx-target="#task-@Model.Id"
        hx-swap="outerHTML">
```

Toggling completion status is a partial update, so we use PATCH. We are not replacing the entire resource, just changing one property. The distinction between PUT and PATCH matters for API design clarity, even when both return the same partial view.

**DELETE with `hx-delete`**

```html
<button hx-delete="/Tasks?handler=Remove&amp;id=@Model.Id"
        hx-target="#task-@Model.Id"
        hx-swap="outerHTML"
        hx-confirm="Delete this task?">
```

The delete button sends a DELETE request and targets the task element itself. The handler returns empty content, so `outerHTML` swap effectively removes the element from the DOM. The `hx-confirm` attribute shows a browser confirmation dialog before sending the request.

## Advanced Targeting with `hx-target`

The `hx-target` attribute accepts CSS selectors, giving you precise control over where responses appear.

### Targeting by ID

The most common pattern targets elements by ID:

```html
<button hx-get="/Data?handler=Load" hx-target="#content">Load</button>
<div id="content"></div>
```

### Targeting Relative to the Element

htmx provides special selectors for targeting elements relative to the trigger:

```html
<!-- Target the closest ancestor with class 'card' -->
<button hx-delete="/Items?handler=Remove&amp;id=5" 
        hx-target="closest .card"
        hx-swap="outerHTML">
    Remove
</button>

<!-- Target the next sibling element -->
<button hx-get="/Details?handler=Show" 
        hx-target="next .details">
    Show Details
</button>
<div class="details"></div>

<!-- Target a child element -->
<div class="container">
    <button hx-get="/Content?handler=Load" 
            hx-target="find .content-area">
        Load
    </button>
    <div class="content-area"></div>
</div>
```

Available relative selectors:

- `this`: The element that triggered the request
- `closest <selector>`: The nearest ancestor matching the selector
- `next <selector>`: The next element in the DOM matching the selector
- `previous <selector>`: The previous element matching the selector
- `find <selector>`: The first child element matching the selector

### Targeting the Document Body

For full-page updates or navigation-like behavior:

```html
<a hx-get="/Dashboard" hx-target="body" hx-push-url="true">
    Go to Dashboard
</a>
```

This replaces the entire body content and updates the browser URL, simulating navigation without a full page reload.

## Mastering `hx-swap` Options

The `hx-swap` attribute controls how the response content gets inserted. Each option serves different use cases.

### Swap Options Comparison

```html
<!-- Replace inner content (default) -->
<div hx-get="/Content" hx-swap="innerHTML">
    <p>This paragraph will be replaced</p>
</div>

<!-- Replace the entire element -->
<div hx-get="/Content" hx-swap="outerHTML">
    This entire div will be replaced
</div>

<!-- Insert before the element -->
<ul>
    <li hx-get="/NewItem" hx-swap="beforebegin">
        New items appear above this
    </li>
</ul>

<!-- Insert after the element -->
<li hx-get="/NewItem" hx-swap="afterend">
    New items appear below this
</li>

<!-- Insert at the start of the element's children -->
<ul hx-get="/NewItem" hx-swap="afterbegin">
    <li>New items appear before this</li>
</ul>

<!-- Insert at the end of the element's children -->
<ul hx-get="/NewItem" hx-swap="beforeend">
    <li>New items appear after this</li>
</ul>

<!-- Delete the target element -->
<div hx-delete="/Items?id=5" hx-swap="delete">
    This element will be removed
</div>

<!-- Keep current content, useful for side effects only -->
<button hx-post="/Track" hx-swap="none">
    Track Click
</button>
```

### Swap Modifiers

You can add modifiers to control swap timing and behavior:

```html
<!-- Wait 1 second before swapping -->
<div hx-get="/Content" hx-swap="innerHTML swap:1s">
    Loading...
</div>

<!-- Fade out old content, fade in new -->
<div hx-get="/Content" hx-swap="innerHTML transition:true">
    Content with transitions
</div>

<!-- Scroll to top of swapped content -->
<div hx-get="/Content" hx-swap="innerHTML scroll:top">
    Long content area
</div>

<!-- Show swapped content at top of viewport -->
<div hx-get="/Content" hx-swap="innerHTML show:top">
    Content that should be visible
</div>
```

## Out-of-Band Swaps

Sometimes a single server action needs to update multiple unrelated parts of the page. Out-of-band (OOB) swaps solve this problem cleanly.

### The Problem

Consider a scenario where adding a task also needs to update a task count in the header. With standard htmx, you would need two separate requests. OOB swaps let you update both with one response.

### The Solution

Your server response includes additional elements marked with `hx-swap-oob="true"`:

**Updated OnPostCreate handler:**

```csharp
public IActionResult OnPostCreate(string title)
{
    if (string.IsNullOrWhiteSpace(title))
    {
        return Content("<p class=\"error\">Title is required</p>", "text/html");
    }
    
    var task = _taskService.Create(title);
    var totalCount = _taskService.GetAll().Count;
    
    // Return the new task item plus an OOB update for the counter
    var response = new StringBuilder();
    response.Append(RenderPartial("_TaskItem", task));
    response.Append($"<span id=\"task-count\" hx-swap-oob=\"true\">{totalCount}</span>");
    
    return Content(response.ToString(), "text/html");
}
```

Or using a partial view that includes OOB elements:

**Pages/Shared/_TaskItemWithCount.cshtml**

```html
@model YourApp.ViewModels.TaskItemWithCountViewModel

<!-- Primary response -->
<partial name="_TaskItem" model="Model.Task" />

<!-- Out-of-band update -->
<span id="task-count" hx-swap-oob="true">@Model.TotalCount</span>
```

The page needs a matching element:

```html
<header>
    <span id="task-count">@Model.Tasks.Count</span> tasks
</header>
```

When htmx receives the response, it:
1. Swaps the primary content (the new task) into the target
2. Finds any elements with `hx-swap-oob="true"`
3. Swaps those elements into matching elements on the page by ID

### OOB Swap Modes

You can specify how OOB elements should be swapped:

```html
<!-- Replace the element (default) -->
<div id="notifications" hx-swap-oob="true">New content</div>

<!-- Append to the element -->
<div id="log" hx-swap-oob="beforeend">New log entry</div>

<!-- Prepend to the element -->
<div id="messages" hx-swap-oob="afterbegin">New message</div>
```

## Debugging htmx Applications

When htmx requests do not behave as expected, you have several debugging options.

### Enable Logging

Add this to your page during development:

```html
<script>
htmx.logAll();
</script>
```

This outputs every htmx event to the browser console, showing you exactly what htmx is doing.

### Listen to Specific Events

For targeted debugging, listen to specific htmx events:

```html
<script>
document.body.addEventListener('htmx:beforeRequest', function(event) {
    console.log('Request starting:', event.detail.pathInfo.requestPath);
    console.log('Method:', event.detail.verb);
});

document.body.addEventListener('htmx:afterRequest', function(event) {
    console.log('Response status:', event.detail.xhr.status);
    console.log('Response body:', event.detail.xhr.responseText);
});

document.body.addEventListener('htmx:responseError', function(event) {
    console.error('Request failed:', event.detail.xhr.status);
});

document.body.addEventListener('htmx:swapError', function(event) {
    console.error('Swap failed:', event.detail.error);
});
</script>
```

### Network Tab Inspection

The browser's Network tab shows every htmx request. Check:

- **URL**: Is the request going to the correct endpoint?
- **Method**: Is it GET, POST, PUT, PATCH, or DELETE as expected?
- **Request Headers**: Is the anti-forgery token present?
- **Request Body**: For POST/PUT, is the form data correct?
- **Response Status**: 200, 400, 404, 500?
- **Response Body**: What HTML is the server returning?

### Common Issues and Solutions

**Problem**: Request returns 400 Bad Request

**Solution**: The anti-forgery token is likely missing. Ensure `@Html.AntiForgeryToken()` is on the page and the token-forwarding script is in your layout.

**Problem**: Handler not found (404)

**Solution**: Verify the handler name matches your method. `handler=Create` needs `OnPostCreate()` for POST, `OnGetCreate()` for GET.

**Problem**: PUT/PATCH/DELETE returns 405 Method Not Allowed

**Solution**: ASP.NET Core needs handler methods named `OnPut*`, `OnPatch*`, `OnDelete*`. Verify spelling and that the method is public.

**Problem**: Response appears but in wrong location

**Solution**: Check your `hx-target` selector. Use browser dev tools to verify the target element exists and has the expected ID.

**Problem**: Element not removed after delete

**Solution**: Ensure you return empty content and use `hx-swap="outerHTML"` so the element replaces itself with nothing.

## Performance Considerations

### Minimize Response Size

Return only the HTML needed for the swap. Avoid returning full page layouts for partial updates:

```csharp
// Good: Returns only the needed fragment
public IActionResult OnGetItem(int id)
{
    var item = _service.GetById(id);
    return Partial("_Item", item);
}

// Avoid: Returns more than necessary
public IActionResult OnGetItem(int id)
{
    var item = _service.GetById(id);
    ViewData["Item"] = item;
    return Page(); // Returns entire page
}
```

### Use Appropriate HTTP Methods

Match your HTTP method to the operation semantics:

- GET for retrieving data (cacheable, safe)
- POST for creating resources
- PUT for full updates
- PATCH for partial updates
- DELETE for removal

Using the correct method helps with caching, browser behavior, and API clarity.

### Debounce Rapid Triggers

For inputs that fire on every keystroke, add a delay:

```html
<input type="text"
       hx-get="/Search?handler=Results"
       hx-target="#results"
       hx-trigger="keyup changed delay:300ms" />
```

The request only fires 300ms after the user stops typing, preventing server overload.

## Summary

This chapter covered the complete htmx command set for building interactive Razor Pages applications:

- `hx-get` retrieves data without page reloads
- `hx-post` creates new resources and submits forms
- `hx-put` replaces existing resources entirely
- `hx-patch` partially updates resources
- `hx-delete` removes resources
- `hx-target` controls where responses appear using CSS selectors
- `hx-swap` determines how responses are inserted into the DOM
- Out-of-band swaps update multiple page elements from one response

You built a complete CRUD application demonstrating all five HTTP methods with proper anti-forgery protection. You learned advanced targeting with relative selectors and mastered swap options for different update scenarios.

## Preview of Next Chapter

In the next chapter, we will explore `hx-trigger` in depth. You will learn how to fire requests on custom events, combine multiple triggers, add conditions and modifiers, and create responsive interfaces that react to user behavior in sophisticated ways.
