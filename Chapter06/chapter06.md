---
order: 25
icon: stack
label: Chap 6 - Working with hx-put, hx-patch, and hx-delete
meta:
title: "Working with hx-put, hx-patch, and hx-delete"
---

# Working with `hx-put`, `hx-patch`, and `hx-delete`

Chapter 4 introduced all five HTTP methods with a basic CRUD example. Chapter 5 went deep on `hx-get` and `hx-post`. Now it is time to master the update and delete operations that make your applications feel truly interactive.

This chapter goes beyond the basics. You will learn when to choose PUT over PATCH, how to handle deletion with proper user confirmation, and patterns for optimistic updates that make your UI feel instant. You will build undo functionality, handle conflicts when multiple users edit the same data, and implement soft delete for recoverable removals. These are the patterns that separate toy examples from production applications.

## Understanding PUT vs PATCH

Both PUT and PATCH modify existing resources, but they serve different purposes.

**PUT** replaces a resource entirely. When you send a PUT request, you provide the complete new state of the resource. Any fields you omit are cleared or set to defaults. Use PUT when users are editing a complete form where all fields should be saved together.

**PATCH** modifies a resource partially. You send only the fields that changed. The server merges these changes with the existing data. Use PATCH for single-field updates, toggles, or when you want to minimize data transfer.

Here is how this distinction plays out in practice:

```csharp
// PUT: Replace the entire product
public IActionResult OnPutUpdate(int id, ProductForm form)
{
    var product = _productService.GetById(id);
    if (product == null) return NotFound();

    // All fields are replaced, even if form has empty values
    product.Name = form.Name;
    product.Description = form.Description;
    product.Price = form.Price;
    product.Stock = form.Stock;
    product.Category = form.Category;
    
    _productService.Update(product);
    return Partial("_ProductRow", product);
}

// PATCH: Update only specific fields
public IActionResult OnPatchStock(int id, int stock)
{
    var product = _productService.GetById(id);
    if (product == null) return NotFound();

    // Only stock is modified, other fields remain unchanged
    product.Stock = stock;
    
    _productService.Update(product);
    return Partial("_ProductRow", product);
}
```

The HTML reflects this distinction:

```html
<!-- PUT: Full edit form -->
<form hx-put="/Products?handler=Update&amp;id=@product.Id"
      hx-target="#product-@product.Id"
      hx-swap="outerHTML">
    @Html.AntiForgeryToken()
    <input type="text" name="Name" value="@product.Name" required />
    <input type="text" name="Description" value="@product.Description" />
    <input type="number" name="Price" value="@product.Price" step="0.01" required />
    <input type="number" name="Stock" value="@product.Stock" required />
    <select name="Category">
        @foreach (var cat in Model.Categories)
        {
            <option value="@cat" selected="@(cat == product.Category)">@cat</option>
        }
    </select>
    <button type="submit">Save All Changes</button>
</form>

<!-- PATCH: Single field update -->
<form hx-patch="/Products?handler=Stock&amp;id=@product.Id"
      hx-target="#product-@product.Id"
      hx-swap="outerHTML">
    @Html.AntiForgeryToken()
    <input type="number" name="stock" value="@product.Stock" />
    <button type="submit">Update Stock</button>
</form>
```

## Anti-Forgery Tokens for PUT, PATCH, and DELETE

ASP.NET Core requires anti-forgery tokens for all state-changing requests. This protection applies to PUT, PATCH, and DELETE just as it does to POST.

### Layout Configuration

Add this script to your `_Layout.cshtml` after the htmx script:

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

### Token Placement

For forms, include the token inside the form:

```html
<form hx-put="/Products?handler=Update&amp;id=5">
    @Html.AntiForgeryToken()
    <!-- form fields -->
</form>
```

For buttons outside forms, include a token somewhere on the page:

```html
@Html.AntiForgeryToken()

<button hx-delete="/Products?handler=Remove&amp;id=5"
        hx-target="#product-5"
        hx-swap="outerHTML">
    Delete
</button>
```

The event listener in the layout will find the token and add it to all htmx requests automatically.

## Implementing Delete Operations

Deletion requires careful handling. Users need confirmation before destructive actions, and the UI needs to reflect the removal immediately.

### Basic Delete with Confirmation

The `hx-confirm` attribute shows a browser confirmation dialog before sending the request:

```html
@Html.AntiForgeryToken()

<div id="item-@item.Id" class="item-row">
    <span>@item.Name</span>
    <button hx-delete="/Items?handler=Remove&amp;id=@item.Id"
            hx-target="#item-@item.Id"
            hx-swap="outerHTML"
            hx-confirm="Delete '@item.Name'? This cannot be undone."
            class="btn btn-danger">
        Delete
    </button>
</div>
```

The handler removes the item and returns empty content:

```csharp
public IActionResult OnDeleteRemove(int id)
{
    var item = _itemService.GetById(id);
    if (item == null)
    {
        return NotFound();
    }

    _itemService.Delete(id);
    
    // Return empty content to remove the element from the DOM
    return Content("", "text/html");
}
```

When htmx receives an empty response with `hx-swap="outerHTML"`, it replaces the target element with nothing, effectively removing it from the page.

### Why NoContent() Does Not Work for Removal

You might expect `return NoContent()` (HTTP 204) to work for deletions, but htmx handles 204 responses differently. When htmx receives a 204 No Content response, it does not perform any swap operation. The target element remains unchanged.

```csharp
// This will NOT remove the element from the page
public IActionResult OnDeleteRemove(int id)
{
    _itemService.Delete(id);
    return NoContent(); // 204 status - htmx skips the swap
}

// This WILL remove the element
public IActionResult OnDeleteRemove(int id)
{
    _itemService.Delete(id);
    return Content("", "text/html"); // 200 status with empty body
}
```

Use `NoContent()` only when you do not want htmx to modify the DOM at all, such as for background operations or analytics tracking.

### Delete with Feedback Message

Sometimes you want to show a confirmation message where the deleted item was:

```csharp
public IActionResult OnDeleteRemove(int id)
{
    var item = _itemService.GetById(id);
    if (item == null)
    {
        return NotFound();
    }

    _itemService.Delete(id);
    
    return Content($"<div class=\"deleted-message\">'{item.Name}' has been deleted</div>", "text/html");
}
```

Add CSS to fade out the message:

```css
.deleted-message {
    background: #d4edda;
    padding: 10px;
    animation: fadeOut 3s forwards;
}

@keyframes fadeOut {
    0% { opacity: 1; }
    70% { opacity: 1; }
    100% { opacity: 0; height: 0; padding: 0; margin: 0; }
}
```

## Soft Delete with Restore

Production applications often use soft delete, marking records as inactive rather than removing them permanently. This allows users to restore accidentally deleted items.

### The Model

```csharp
public class Document
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

### The Partial Views

**Pages/Shared/_DocumentRow.cshtml**

```html
@model Document

<tr id="document-@Model.Id" class="@(Model.IsDeleted ? "deleted" : "")">
    <td>@Model.Title</td>
    <td>@Model.Content.Substring(0, Math.Min(50, Model.Content.Length))...</td>
    <td>
        @if (Model.IsDeleted)
        {
            <button hx-patch="/Documents?handler=Restore&amp;id=@Model.Id"
                    hx-target="#document-@Model.Id"
                    hx-swap="outerHTML"
                    class="btn btn-sm btn-success">
                Restore
            </button>
            <button hx-delete="/Documents?handler=Permanent&amp;id=@Model.Id"
                    hx-target="#document-@Model.Id"
                    hx-swap="outerHTML"
                    hx-confirm="Permanently delete this document? This cannot be undone."
                    class="btn btn-sm btn-danger">
                Delete Forever
            </button>
        }
        else
        {
            <button hx-get="/Documents?handler=Edit&amp;id=@Model.Id"
                    hx-target="#document-@Model.Id"
                    hx-swap="outerHTML"
                    class="btn btn-sm btn-primary">
                Edit
            </button>
            <button hx-delete="/Documents?handler=Remove&amp;id=@Model.Id"
                    hx-target="#document-@Model.Id"
                    hx-swap="outerHTML"
                    class="btn btn-sm btn-warning">
                Delete
            </button>
        }
    </td>
</tr>
```

### The Handlers

```csharp
public class DocumentsModel : PageModel
{
    private readonly IDocumentService _documentService;

    public DocumentsModel(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public List<Document> Documents { get; set; } = new();
    public bool ShowDeleted { get; set; }

    public void OnGet(bool showDeleted = false)
    {
        ShowDeleted = showDeleted;
        Documents = showDeleted 
            ? _documentService.GetAllIncludingDeleted()
            : _documentService.GetActive();
    }

    // Soft delete - marks as deleted but preserves data
    public IActionResult OnDeleteRemove(int id)
    {
        var document = _documentService.GetById(id);
        if (document == null) return NotFound();

        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        _documentService.Update(document);

        return Partial("_DocumentRow", document);
    }

    // Restore - brings back a soft-deleted document
    public IActionResult OnPatchRestore(int id)
    {
        var document = _documentService.GetById(id);
        if (document == null) return NotFound();

        document.IsDeleted = false;
        document.DeletedAt = null;
        _documentService.Update(document);

        return Partial("_DocumentRow", document);
    }

    // Permanent delete - removes from database entirely
    public IActionResult OnDeletePermanent(int id)
    {
        var document = _documentService.GetById(id);
        if (document == null) return NotFound();

        _documentService.PermanentDelete(id);

        return Content("", "text/html");
    }
}
```

### CSS for Deleted State

```css
tr.deleted {
    opacity: 0.6;
    background-color: #f8f9fa;
}

tr.deleted td:first-child::before {
    content: "[Deleted] ";
    font-style: italic;
    color: #6c757d;
}
```

## Optimistic Updates

Standard htmx requests wait for the server response before updating the UI. For fast operations, this works fine. But for operations with noticeable latency, users appreciate immediate feedback.

Optimistic updates show the expected result immediately, then correct if the server reports an error. This makes your application feel faster.

### Toggle with Optimistic Update

Here is a task completion toggle that updates instantly:

```html
<div id="task-@task.Id" class="task @(task.IsComplete ? "complete" : "")">
    <input type="checkbox" 
           @(task.IsComplete ? "checked" : "")
           hx-patch="/Tasks?handler=Toggle&amp;id=@task.Id"
           hx-target="#task-@task.Id"
           hx-swap="outerHTML"
           hx-on:click="this.closest('.task').classList.toggle('complete')" />
    <span class="task-title">@task.Title</span>
</div>
```

The `hx-on:click` attribute runs JavaScript immediately when clicked, toggling the visual state before the request completes. If the server returns successfully, the response replaces the element with the confirmed state. If the server returns an error, the response shows the original state.

### Handling Optimistic Update Failures

For more control over failure handling, use htmx events:

```html
<div id="task-container">
    @foreach (var task in Model.Tasks)
    {
        <partial name="_TaskItem" model="task" />
    }
</div>

<script>
document.getElementById('task-container').addEventListener('htmx:beforeRequest', function(event) {
    // Store original state before request
    var target = event.detail.target;
    target.dataset.originalHtml = target.outerHTML;
});

document.getElementById('task-container').addEventListener('htmx:responseError', function(event) {
    // Restore original state on error
    var target = event.detail.target;
    if (target.dataset.originalHtml) {
        target.outerHTML = target.dataset.originalHtml;
    }
    alert('Update failed. Please try again.');
});
</script>
```

## Handling Conflicts

When multiple users can edit the same data, conflicts arise. User A loads a record, User B modifies it, then User A tries to save their changes. Without conflict detection, User A would overwrite User B's changes.

### Optimistic Concurrency with Row Version

Add a version field to your model:

```csharp
public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
```

Include the version in your edit form:

```html
<form hx-put="/Articles?handler=Update&amp;id=@Model.Id"
      hx-target="#article-@Model.Id"
      hx-swap="outerHTML">
    @Html.AntiForgeryToken()
    <input type="hidden" name="rowVersion" value="@Convert.ToBase64String(Model.RowVersion)" />
    <input type="text" name="title" value="@Model.Title" required />
    <textarea name="content" required>@Model.Content</textarea>
    <button type="submit">Save</button>
</form>

<div id="conflict-message"></div>
```

The handler checks the version before saving:

```csharp
public IActionResult OnPutUpdate(int id, string title, string content, string rowVersion)
{
    var article = _articleService.GetById(id);
    if (article == null) return NotFound();

    var clientVersion = Convert.FromBase64String(rowVersion);
    
    if (!article.RowVersion.SequenceEqual(clientVersion))
    {
        // Conflict detected - someone else modified this article
        Response.StatusCode = 409;
        return Content(@"
            <div class=""conflict-warning"">
                <p><strong>Conflict detected!</strong></p>
                <p>This article was modified by another user. Please refresh and try again.</p>
                <button hx-get=""/Articles?handler=View&amp;id=" + id + @"""
                        hx-target=""#article-" + id + @"""
                        hx-swap=""outerHTML"">
                    Refresh
                </button>
            </div>", "text/html");
    }

    article.Title = title;
    article.Content = content;
    _articleService.Update(article);

    return Partial("_ArticleRow", article);
}
```

## Batch Operations

Sometimes you need to update or delete multiple items at once. Chapter 5 covered bulk operations with checkboxes. Here is the pattern applied to PUT and DELETE.

### Batch Update

```html
@Html.AntiForgeryToken()

<div class="batch-controls" hx-include="#inventory-form">
    <label>
        Set all selected to category:
        <select name="newCategory">
            <option value="electronics">Electronics</option>
            <option value="clothing">Clothing</option>
            <option value="food">Food</option>
        </select>
    </label>
    <button hx-patch="/Inventory?handler=BatchCategory"
            hx-target="#inventory-list">
        Apply to Selected
    </button>
</div>

<form id="inventory-form">
    <table>
        <thead>
            <tr>
                <th><input type="checkbox" id="select-all" /></th>
                <th>Name</th>
                <th>Category</th>
            </tr>
        </thead>
        <tbody id="inventory-list">
            @foreach (var item in Model.Items)
            {
                <partial name="_InventoryRow" model="item" />
            }
        </tbody>
    </table>
</form>
```

The handler processes all selected items:

```csharp
public IActionResult OnPatchBatchCategory(int[] ids, string newCategory)
{
    if (ids == null || ids.Length == 0)
    {
        return Content("<tr><td colspan=\"3\">No items selected</td></tr>", "text/html");
    }

    var items = _inventoryService.GetByIds(ids);
    
    foreach (var item in items)
    {
        item.Category = newCategory;
        _inventoryService.Update(item);
    }

    var allItems = _inventoryService.GetAll();
    return Partial("_InventoryRows", allItems);
}
```

### Batch Delete

```html
<button hx-delete="/Inventory?handler=BatchRemove"
        hx-target="#inventory-list"
        hx-confirm="Delete all selected items?"
        hx-include="#inventory-form">
    Delete Selected
</button>
```

```csharp
public IActionResult OnDeleteBatchRemove(int[] ids)
{
    if (ids == null || ids.Length == 0)
    {
        return Content("<tr><td colspan=\"3\">No items selected</td></tr>", "text/html");
    }

    _inventoryService.DeleteMany(ids);

    var remainingItems = _inventoryService.GetAll();
    return Partial("_InventoryRows", remainingItems);
}
```

## Error Handling for Update Operations

Users need clear feedback when updates fail. Return appropriate HTTP status codes and error messages.

### Validation Errors (400 Bad Request)

```csharp
public IActionResult OnPatchUpdatePrice(int id, decimal price)
{
    if (price < 0)
    {
        Response.StatusCode = 400;
        return Content("<div class=\"error\">Price cannot be negative</div>", "text/html");
    }

    var product = _productService.GetById(id);
    if (product == null) return NotFound();

    product.Price = price;
    _productService.Update(product);

    return Partial("_ProductRow", product);
}
```

### Not Found (404)

```csharp
public IActionResult OnDeleteRemove(int id)
{
    var item = _itemService.GetById(id);
    if (item == null)
    {
        Response.StatusCode = 404;
        return Content("<div class=\"error\">Item not found. It may have been deleted.</div>", "text/html");
    }

    _itemService.Delete(id);
    return Content("", "text/html");
}
```

### Server Error (500)

```csharp
public IActionResult OnPutUpdate(int id, ProductForm form)
{
    try
    {
        var product = _productService.GetById(id);
        if (product == null) return NotFound();

        product.Name = form.Name;
        product.Price = form.Price;
        _productService.Update(product);

        return Partial("_ProductRow", product);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to update product {Id}", id);
        Response.StatusCode = 500;
        return Content("<div class=\"error\">An error occurred. Please try again.</div>", "text/html");
    }
}
```

### Client-Side Error Handling

Handle errors globally with htmx events:

```html
<script>
document.body.addEventListener('htmx:responseError', function(event) {
    var status = event.detail.xhr.status;
    
    if (status === 401) {
        window.location.href = '/Login';
    } else if (status === 403) {
        alert('You do not have permission to perform this action.');
    } else if (status >= 500) {
        alert('Server error. Please try again later.');
    }
});
</script>
```

## Debugging PUT, PATCH, and DELETE

When requests fail, use these debugging techniques.

### Browser Network Tab

Open Developer Tools (F12), go to the Network tab, and filter by XHR/Fetch. Check:

- **Request Method**: Is it PUT, PATCH, or DELETE as expected?
- **Request Headers**: Is RequestVerificationToken present?
- **Request Body**: Are form fields being sent correctly?
- **Response Status**: 200, 204, 400, 404, 409, 500?
- **Response Body**: What HTML is returned?

### Common Issues

**400 Bad Request**

The anti-forgery token is missing. Ensure `@Html.AntiForgeryToken()` is on the page and the token-forwarding script is in your layout.

**404 Not Found**

The handler name does not match your method. `handler=Update` with PUT requires `OnPutUpdate()`.

**405 Method Not Allowed**

Your handler method is missing or named incorrectly. `OnPut*`, `OnPatch*`, and `OnDelete*` must be spelled exactly right and be public.

**Element Not Removed After Delete**

You may be returning `NoContent()` (204). Change to `return Content("", "text/html")` to get a 200 response that htmx will process.

**Update Does Not Appear**

Check your `hx-target` selector. The element with that ID must exist on the page.

## Summary

This chapter covered advanced patterns for `hx-put`, `hx-patch`, and `hx-delete`:

- **PUT vs PATCH**: Use PUT for complete replacements, PATCH for partial updates
- **Anti-forgery tokens**: Required for all state-changing requests
- **Element removal**: Return empty content with 200 status, not NoContent()
- **hx-confirm**: Require user confirmation before destructive operations
- **Soft delete**: Mark records as deleted while preserving data for recovery
- **Optimistic updates**: Update UI immediately, correct on error
- **Conflict handling**: Use row versions to detect concurrent modifications
- **Batch operations**: Update or delete multiple items with one request
- **Error handling**: Return appropriate status codes and error messages

## Preview of Next Chapter

Chapter 7 explores `hx-target` and `hx-swap` in depth. You will learn advanced targeting with CSS selectors, multiple swap strategies, out-of-band updates for complex UI changes, and techniques for updating multiple page sections from a single response.
