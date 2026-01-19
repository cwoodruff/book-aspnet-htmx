---
order: 17
icon: stack
label: Chap 14 - Building Blocks with HTML Components- Buttons and Action Links
meta:
title: "Building Blocks with HTML Components: Buttons and Action Links"
---

# Building Blocks with HTML Components: Buttons and Action Links

Buttons and links are how users take action in your application. Every click represents intent: create this record, delete that item, toggle this setting, submit this form. Traditional web applications handle these clicks with full page reloads or complex JavaScript event handlers. htmx lets you wire up responsive, immediate interactions using HTML attributes alone.

This chapter focuses on button and link patterns that go beyond basic form submission: inline prompts that collect input without forms, confirmation dialogs that prevent accidents, toggle buttons that flip state instantly, batch actions that operate on multiple items, and buttons that disable themselves during requests to prevent double-submission. Each pattern includes complete server-side code and proper security handling.

## Anti-Forgery Token Configuration

All POST, PUT, PATCH, and DELETE requests require anti-forgery tokens. Add this configuration to your layout once, and all htmx requests will include the token automatically:

**Pages/Shared/_Layout.cshtml:**

```html
@Html.AntiForgeryToken()

<script>
document.body.addEventListener('htmx:configRequest', function(event) {
    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    if (token) {
        event.detail.headers['RequestVerificationToken'] = token.value;
    }
});
</script>
```

## Inline Prompts with `hx-prompt`

The `hx-prompt` attribute displays a browser prompt dialog and sends the user's input to the server. This works well for quick edits that don't warrant a full form.

### Rename Button

A button that prompts for a new name and updates the item inline:

```html
<tr id="item-@item.Id">
    <td class="item-name">@item.Name</td>
    <td>
        <button hx-post="/Items?handler=Rename&amp;id=@item.Id"
                hx-prompt="Enter a new name:"
                hx-target="#item-@item.Id"
                hx-swap="outerHTML"
                hx-indicator="#rename-spinner-@item.Id"
                class="btn btn-sm btn-outline-secondary">
            Rename
            <span id="rename-spinner-@item.Id" class="htmx-indicator spinner-border spinner-border-sm"></span>
        </button>
    </td>
</tr>
```

**Important:** The prompt value arrives in the `HX-Prompt` request header, not as a form parameter.

**Pages/Items.cshtml.cs:**

```csharp
public class ItemsModel : PageModel
{
    private readonly IItemService _itemService;

    public ItemsModel(IItemService itemService)
    {
        _itemService = itemService;
    }

    public List<Item> Items { get; set; } = new();

    public void OnGet()
    {
        Items = _itemService.GetAll();
    }

    public IActionResult OnPostRename(int id)
    {
        // Get the prompt value from the HX-Prompt header
        var newName = Request.Headers["HX-Prompt"].ToString();

        if (string.IsNullOrWhiteSpace(newName))
        {
            Response.StatusCode = 400;
            return Content("<td colspan=\"2\" class=\"text-danger\">Name cannot be empty</td>", "text/html");
        }

        var item = _itemService.GetById(id);
        if (item == null) return NotFound();

        item.Name = newName;
        _itemService.Update(item);

        return Partial("_ItemRow", item);
    }
}
```

**Pages/Shared/_ItemRow.cshtml:**

```html
@model Item

<tr id="item-@Model.Id">
    <td class="item-name">@Model.Name</td>
    <td>
        <button hx-post="/Items?handler=Rename&amp;id=@Model.Id"
                hx-prompt="Enter a new name:"
                hx-target="#item-@Model.Id"
                hx-swap="outerHTML"
                class="btn btn-sm btn-outline-secondary">
            Rename
        </button>
        <button hx-delete="/Items?handler=Delete&amp;id=@Model.Id"
                hx-target="#item-@Model.Id"
                hx-swap="outerHTML"
                hx-confirm="Delete '@Model.Name'?"
                class="btn btn-sm btn-outline-danger">
            Delete
        </button>
    </td>
</tr>
```

### Quick Add with Prompt

Add items without a form:

```html
<button hx-post="/Items?handler=QuickAdd"
        hx-prompt="Enter item name:"
        hx-target="#items-list"
        hx-swap="beforeend"
        class="btn btn-primary">
    Quick Add Item
</button>

<table class="table">
    <thead>
        <tr>
            <th>Name</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody id="items-list">
        @foreach (var item in Model.Items)
        {
            <partial name="_ItemRow" model="item" />
        }
    </tbody>
</table>
```

```csharp
public IActionResult OnPostQuickAdd()
{
    var name = Request.Headers["HX-Prompt"].ToString();

    if (string.IsNullOrWhiteSpace(name))
    {
        return Content("", "text/html"); // User cancelled or empty input
    }

    var item = new Item { Name = name };
    _itemService.Add(item);

    return Partial("_ItemRow", item);
}
```

## Confirmation Dialogs

Destructive actions need confirmation. htmx provides two approaches: the simple `hx-confirm` attribute and custom modal confirmations.

### Simple Confirmation with `hx-confirm`

The `hx-confirm` attribute shows a browser confirmation dialog before sending the request:

```html
<button hx-delete="/Tasks?handler=Delete&amp;id=@task.Id"
        hx-target="#task-@task.Id"
        hx-swap="outerHTML"
        hx-confirm="Delete '@task.Name'? This cannot be undone."
        class="btn btn-sm btn-danger">
    Delete
</button>
```

If the user clicks "OK," the request proceeds. If they click "Cancel," nothing happens.

**Server handler:**

```csharp
public IActionResult OnDeleteDelete(int id)
{
    var task = _taskService.GetById(id);
    if (task == null) return NotFound();

    _taskService.Delete(id);

    // Return empty content to remove the row
    return Content("", "text/html");
}
```

### Custom Modal Confirmation

For styled confirmations with more context, load a modal:

**Trigger button:**

```html
<button hx-get="/Tasks?handler=ConfirmDelete&amp;id=@task.Id"
        hx-target="#modal-content"
        hx-swap="innerHTML"
        _="on htmx:afterSwap add .open to #modal"
        class="btn btn-sm btn-danger">
    Delete
</button>
```

**Modal structure (in layout):**

```html
<div id="modal" class="modal" _="on click if event.target is #modal remove .open from #modal">
    <div class="modal-backdrop"></div>
    <div class="modal-dialog">
        <div id="modal-content"></div>
    </div>
</div>
```

**Pages/Shared/_ConfirmDeleteTask.cshtml:**

```html
@model TaskItem

<div class="modal-header">
    <h5>Delete Task</h5>
    <button type="button" class="btn-close" _="on click remove .open from #modal"></button>
</div>
<div class="modal-body">
    <p>Are you sure you want to delete <strong>@Model.Name</strong>?</p>
    @if (Model.DueDate.HasValue)
    {
        <p class="text-muted">Due: @Model.DueDate.Value.ToString("MMM d, yyyy")</p>
    }
    <p class="text-danger">This action cannot be undone.</p>
</div>
<div class="modal-footer">
    <button type="button" 
            class="btn btn-secondary"
            _="on click remove .open from #modal">
        Cancel
    </button>
    <button hx-delete="/Tasks?handler=Delete&amp;id=@Model.Id"
            hx-target="#task-@Model.Id"
            hx-swap="outerHTML"
            _="on htmx:afterRequest remove .open from #modal"
            class="btn btn-danger">
        Delete Task
    </button>
</div>
```

**Server handlers:**

```csharp
public IActionResult OnGetConfirmDelete(int id)
{
    var task = _taskService.GetById(id);
    if (task == null) return NotFound();
    return Partial("_ConfirmDeleteTask", task);
}

public IActionResult OnDeleteDelete(int id)
{
    var task = _taskService.GetById(id);
    if (task == null) return NotFound();

    _taskService.Delete(id);
    return Content("", "text/html");
}
```

**Modal CSS:**

```css
.modal {
    display: none;
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    z-index: 1050;
}

.modal.open {
    display: flex;
    align-items: center;
    justify-content: center;
}

.modal-backdrop {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0, 0, 0, 0.5);
}

.modal-dialog {
    position: relative;
    background: white;
    border-radius: 8px;
    max-width: 500px;
    width: 90%;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
}

.modal-header {
    padding: 1rem;
    border-bottom: 1px solid #dee2e6;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.modal-body {
    padding: 1rem;
}

.modal-footer {
    padding: 1rem;
    border-top: 1px solid #dee2e6;
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
}
```

## Toggle Buttons

Toggle buttons switch state and update their appearance immediately.

### Complete/Incomplete Toggle

```html
@model TaskItem

<button hx-patch="/Tasks?handler=Toggle&amp;id=@Model.Id"
        hx-target="this"
        hx-swap="outerHTML"
        class="btn btn-sm @(Model.IsComplete ? "btn-success" : "btn-outline-secondary")">
    @if (Model.IsComplete)
    {
        <span>✓ Complete</span>
    }
    else
    {
        <span>Mark Complete</span>
    }
</button>
```

**Server handler:**

```csharp
public IActionResult OnPatchToggle(int id)
{
    var task = _taskService.GetById(id);
    if (task == null) return NotFound();

    task.IsComplete = !task.IsComplete;
    task.CompletedAt = task.IsComplete ? DateTime.UtcNow : null;
    _taskService.Update(task);

    return Partial("_ToggleButton", task);
}
```

**Pages/Shared/_ToggleButton.cshtml:**

```html
@model TaskItem

<button hx-patch="/Tasks?handler=Toggle&amp;id=@Model.Id"
        hx-target="this"
        hx-swap="outerHTML"
        class="btn btn-sm @(Model.IsComplete ? "btn-success" : "btn-outline-secondary")">
    @if (Model.IsComplete)
    {
        <span>✓ Complete</span>
    }
    else
    {
        <span>Mark Complete</span>
    }
</button>
```

### Favorite/Unfavorite Toggle

```html
<button hx-post="/Products?handler=ToggleFavorite&amp;id=@product.Id"
        hx-target="this"
        hx-swap="outerHTML"
        class="btn-favorite @(product.IsFavorite ? "favorited" : "")"
        title="@(product.IsFavorite ? "Remove from favorites" : "Add to favorites")">
    @(product.IsFavorite ? "★" : "☆")
</button>
```

```css
.btn-favorite {
    background: none;
    border: none;
    font-size: 1.5rem;
    cursor: pointer;
    color: #ccc;
    transition: color 0.2s;
}

.btn-favorite:hover {
    color: #ffc107;
}

.btn-favorite.favorited {
    color: #ffc107;
}
```

```csharp
public IActionResult OnPostToggleFavorite(int id)
{
    var product = _productService.GetById(id);
    if (product == null) return NotFound();

    product.IsFavorite = !product.IsFavorite;
    _productService.Update(product);

    return Partial("_FavoriteButton", product);
}
```

## Preventing Double Submission

Users sometimes click buttons multiple times during slow requests. Prevent this with the `hx-disabled-elt` attribute or CSS.

### Using `hx-disabled-elt`

The `hx-disabled-elt` attribute specifies which element(s) to disable during the request:

```html
<button hx-post="/Orders?handler=Submit"
        hx-target="#order-result"
        hx-disabled-elt="this"
        class="btn btn-primary">
    Place Order
</button>
```

During the request, htmx adds the `disabled` attribute to the button. When the request completes, it removes it.

### Disabling Multiple Elements

Disable the entire form during submission:

```html
<form hx-post="/Checkout?handler=Process"
      hx-target="#checkout-result"
      hx-disabled-elt="find button, find input">
    @Html.AntiForgeryToken()
    
    <input type="text" name="cardNumber" placeholder="Card Number" required />
    <input type="text" name="expiry" placeholder="MM/YY" required />
    <input type="text" name="cvv" placeholder="CVV" required />
    
    <button type="submit" class="btn btn-primary">
        Pay Now
    </button>
</form>

<div id="checkout-result"></div>
```

### CSS-Based Disable with Loading State

For better visual feedback, combine `hx-disabled-elt` with loading indicators:

```html
<button hx-post="/Reports?handler=Generate"
        hx-target="#report-content"
        hx-disabled-elt="this"
        hx-indicator="find .loading-state"
        class="btn btn-primary generate-btn">
    <span class="default-state">Generate Report</span>
    <span class="loading-state htmx-indicator">
        <span class="spinner-border spinner-border-sm"></span>
        Generating...
    </span>
</button>
```

```css
.generate-btn .loading-state {
    display: none;
}

.generate-btn.htmx-request .default-state {
    display: none;
}

.generate-btn.htmx-request .loading-state {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
}

button[disabled] {
    opacity: 0.6;
    cursor: not-allowed;
}
```

## Batch Actions

Select multiple items and apply an action to all of them.

### Batch Delete

```html
<form id="batch-form">
    <div class="batch-toolbar mb-3" style="display: none;"
         _="on change from .item-checkbox in #items-table
            if (<.item-checkbox:checked/>.length > 0)
                show me
            else
                hide me
            end
            put (<.item-checkbox:checked/>.length) + ' selected' into #selection-count">
        <span id="selection-count">0 selected</span>
        <button type="button"
                hx-delete="/Items?handler=BatchDelete"
                hx-target="#items-table tbody"
                hx-include=".item-checkbox:checked"
                hx-confirm="Delete selected items?"
                class="btn btn-sm btn-danger">
            Delete Selected
        </button>
    </div>

    <table class="table" id="items-table">
        <thead>
            <tr>
                <th>
                    <input type="checkbox" 
                           _="on change set .item-checkbox.checked to my.checked" />
                </th>
                <th>Name</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in Model.Items)
            {
                <tr id="item-@item.Id">
                    <td>
                        <input type="checkbox" 
                               name="ids" 
                               value="@item.Id" 
                               class="item-checkbox" />
                    </td>
                    <td>@item.Name</td>
                    <td>
                        <button hx-delete="/Items?handler=Delete&amp;id=@item.Id"
                                hx-target="#item-@item.Id"
                                hx-swap="outerHTML"
                                hx-confirm="Delete '@item.Name'?"
                                class="btn btn-sm btn-outline-danger">
                            Delete
                        </button>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</form>
```

**Server handler:**

```csharp
public IActionResult OnDeleteBatchDelete([FromForm] int[] ids)
{
    if (ids == null || ids.Length == 0)
    {
        return Content("", "text/html");
    }

    foreach (var id in ids)
    {
        _itemService.Delete(id);
    }

    // Return the updated table body
    var remainingItems = _itemService.GetAll();
    return Partial("_ItemsTableBody", remainingItems);
}
```

**Pages/Shared/_ItemsTableBody.cshtml:**

```html
@model List<Item>

@if (Model.Any())
{
    @foreach (var item in Model)
    {
        <tr id="item-@item.Id">
            <td>
                <input type="checkbox" name="ids" value="@item.Id" class="item-checkbox" />
            </td>
            <td>@item.Name</td>
            <td>
                <button hx-delete="/Items?handler=Delete&amp;id=@item.Id"
                        hx-target="#item-@item.Id"
                        hx-swap="outerHTML"
                        hx-confirm="Delete '@item.Name'?"
                        class="btn btn-sm btn-outline-danger">
                    Delete
                </button>
            </td>
        </tr>
    }
}
else
{
    <tr>
        <td colspan="3" class="text-center text-muted py-4">
            No items found.
        </td>
    </tr>
}
```

## Action Links

Links that trigger htmx actions instead of page navigation.

### Load Content Link

```html
<a hx-get="/Help?handler=Topic&amp;topic=getting-started"
   hx-target="#help-content"
   hx-push-url="/Help/getting-started"
   class="help-link">
    Getting Started Guide
</a>

<div id="help-content">
    Select a topic to view help content.
</div>
```

### Tab-Style Links

```html
<nav class="nav nav-tabs">
    <a hx-get="/Dashboard?handler=Overview"
       hx-target="#dashboard-content"
       hx-push-url="/Dashboard?tab=overview"
       class="nav-link active"
       _="on click remove .active from .nav-link then add .active to me">
        Overview
    </a>
    <a hx-get="/Dashboard?handler=Analytics"
       hx-target="#dashboard-content"
       hx-push-url="/Dashboard?tab=analytics"
       class="nav-link"
       _="on click remove .active from .nav-link then add .active to me">
        Analytics
    </a>
    <a hx-get="/Dashboard?handler=Settings"
       hx-target="#dashboard-content"
       hx-push-url="/Dashboard?tab=settings"
       class="nav-link"
       _="on click remove .active from .nav-link then add .active to me">
        Settings
    </a>
</nav>

<div id="dashboard-content" hx-history-elt>
    <partial name="_DashboardOverview" model="Model.OverviewData" />
</div>
```

## Reusable Button Components

Create partial views for consistent button styling and behavior.

### Delete Button Partial

**Pages/Shared/_DeleteButton.cshtml:**

```html
@model DeleteButtonModel

<button hx-delete="@Model.Url"
        hx-target="@Model.Target"
        hx-swap="@(Model.Swap ?? "outerHTML")"
        hx-confirm="@Model.ConfirmMessage"
        hx-disabled-elt="this"
        class="btn btn-sm @(Model.Outline ? "btn-outline-danger" : "btn-danger")">
    @(Model.Text ?? "Delete")
</button>
```

**DeleteButtonModel.cs:**

```csharp
public class DeleteButtonModel
{
    public string Url { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string? Swap { get; set; }
    public string ConfirmMessage { get; set; } = "Are you sure?";
    public string? Text { get; set; }
    public bool Outline { get; set; } = true;
}
```

**Usage:**

```html
<partial name="_DeleteButton" model="new DeleteButtonModel
{
    Url = $"/Tasks?handler=Delete&id={task.Id}",
    Target = $"#task-{task.Id}",
    ConfirmMessage = $"Delete '{task.Name}'?"
}" />
```

### Action Button Partial

**Pages/Shared/_ActionButton.cshtml:**

```html
@model ActionButtonModel

<button hx-@Model.Method.ToLower()="@Model.Url"
        hx-target="@Model.Target"
        hx-swap="@(Model.Swap ?? "innerHTML")"
        @if (!string.IsNullOrEmpty(Model.Confirm)) { <text>hx-confirm="@Model.Confirm"</text> }
        @if (!string.IsNullOrEmpty(Model.Prompt)) { <text>hx-prompt="@Model.Prompt"</text> }
        hx-disabled-elt="this"
        hx-indicator="find .btn-spinner"
        class="btn @Model.CssClass">
    <span class="btn-text">@Model.Text</span>
    <span class="btn-spinner htmx-indicator spinner-border spinner-border-sm"></span>
</button>
```

## Error Handling

Handle failed requests gracefully:

```html
<div hx-on::response-error="handleButtonError(event, this)"
     hx-on::send-error="handleButtonError(event, this)">
    <button hx-post="/Items?handler=Process&amp;id=@item.Id"
            hx-target="#result-@item.Id"
            class="btn btn-primary">
        Process
    </button>
    <span id="result-@item.Id"></span>
</div>

<script>
function handleButtonError(event, container) {
    var resultSpan = container.querySelector('[id^="result-"]');
    if (resultSpan) {
        resultSpan.innerHTML = '<span class="text-danger">Action failed. Please try again.</span>';
    }
}
</script>
```

Or handle errors server-side with appropriate status codes:

```csharp
public IActionResult OnPostProcess(int id)
{
    try
    {
        var item = _itemService.GetById(id);
        if (item == null)
        {
            Response.StatusCode = 404;
            return Content("<span class=\"text-warning\">Item not found</span>", "text/html");
        }

        _itemService.Process(item);
        return Content("<span class=\"text-success\">Processed successfully</span>", "text/html");
    }
    catch (Exception ex)
    {
        Response.StatusCode = 500;
        return Content("<span class=\"text-danger\">Processing failed</span>", "text/html");
    }
}
```

## Summary

This chapter covered button and link patterns with htmx:

- **`hx-prompt`** displays a browser prompt and sends input via the `HX-Prompt` header
- **`hx-confirm`** shows a confirmation dialog before proceeding
- **Custom modal confirmations** provide styled dialogs with more context
- **Toggle buttons** switch state and update appearance immediately
- **`hx-disabled-elt`** prevents double-submission by disabling elements during requests
- **Batch actions** operate on multiple selected items
- **Action links** trigger htmx requests instead of page navigation
- **Reusable partials** create consistent button components

These patterns make user interactions feel immediate and responsive while keeping all business logic on the server.

## Preview of Next Chapter

Chapter 15 covers loading indicators with `hx-indicator` and state preservation with `hx-preserve`. You will learn to show spinners during requests, maintain scroll position and form state across swaps, and create loading skeletons that make your application feel fast and polished.
