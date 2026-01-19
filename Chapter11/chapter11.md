---
order: 20
icon: stack
label: Chap 11 - Building Blocks with HTML Components- Forms and Modal Forms
meta:
title: "Building Blocks with HTML Components: Forms and Modal Forms"
---

# Building Blocks with HTML Components: Forms and Modal Forms

Forms are where users interact most directly with your application. Registration, login, contact forms, settings pages, checkout flows. Every meaningful action requires a form. Traditional HTML forms reload the entire page on submit, destroying scroll position, clearing transient state, and making users wait while the browser re-renders everything. This experience feels dated.

htmx transforms forms into responsive, immediate interactions. Submit a form, get back a fragment of HTML, swap it into the page. No reload. No flicker. The user stays in context. Combined with modal dialogs, you can create focused workflows that collect input without navigating away from the current page.

This chapter covers both patterns with complete, working examples. You will build inline forms that submit and update in place, modal forms that appear on demand and close on success, and learn how to handle validation errors gracefully. Every example includes the server-side code required to make it work.

## Form Submission with `hx-post`

The `hx-post` attribute converts a traditional form into an htmx-powered form. Instead of reloading the page, the form submits asynchronously and htmx swaps the response into a target element.

### Basic Contact Form

Here's a complete contact form implementation:

**Pages/Contact.cshtml:**

```html
@page
@model ContactModel

<h1>Contact Us</h1>

<div id="form-container">
    <partial name="_ContactForm" model="Model.Input" />
</div>
```

**Pages/Contact.cshtml.cs:**

```csharp
public class ContactModel : PageModel
{
    private readonly IEmailService _emailService;

    public ContactModel(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [BindProperty]
    public ContactFormInput Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostSubmit()
    {
        if (!ModelState.IsValid)
        {
            // Return the form with validation errors
            return Partial("_ContactForm", Input);
        }

        // Process the form
        await _emailService.SendContactEmailAsync(Input.Name, Input.Email, Input.Message);

        // Return success message
        return Partial("_ContactSuccess", Input);
    }
}

public class ContactFormInput
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required")]
    [StringLength(2000, ErrorMessage = "Message cannot exceed 2000 characters")]
    public string Message { get; set; } = string.Empty;
}
```

**Pages/Shared/_ContactForm.cshtml:**

```html
@model ContactFormInput

<form hx-post="/Contact?handler=Submit" 
      hx-target="#form-container" 
      hx-swap="innerHTML"
      hx-indicator="#form-spinner">
    @Html.AntiForgeryToken()
    
    @if (!ViewData.ModelState.IsValid)
    {
        <div class="alert alert-danger">
            <ul class="mb-0">
                @foreach (var error in ViewData.ModelState.Values.SelectMany(v => v.Errors))
                {
                    <li>@error.ErrorMessage</li>
                }
            </ul>
        </div>
    }
    
    <div class="form-group mb-3">
        <label asp-for="Name" class="form-label"></label>
        <input asp-for="Name" class="form-control" />
        <span asp-validation-for="Name" class="text-danger"></span>
    </div>
    
    <div class="form-group mb-3">
        <label asp-for="Email" class="form-label"></label>
        <input asp-for="Email" class="form-control" />
        <span asp-validation-for="Email" class="text-danger"></span>
    </div>
    
    <div class="form-group mb-3">
        <label asp-for="Message" class="form-label"></label>
        <textarea asp-for="Message" class="form-control" rows="5"></textarea>
        <span asp-validation-for="Message" class="text-danger"></span>
    </div>
    
    <button type="submit" class="btn btn-primary">
        Send Message
        <span id="form-spinner" class="htmx-indicator spinner-border spinner-border-sm ms-2"></span>
    </button>
</form>
```

**Pages/Shared/_ContactSuccess.cshtml:**

```html
@model ContactFormInput

<div class="alert alert-success">
    <h4 class="alert-heading">Message Sent!</h4>
    <p>Thank you, @Model.Name. We've received your message and will respond to @Model.Email shortly.</p>
    <hr>
    <button hx-get="/Contact" 
            hx-target="#form-container" 
            hx-swap="innerHTML"
            class="btn btn-outline-success">
        Send Another Message
    </button>
</div>
```

**CSS for the spinner:**

```css
.htmx-indicator {
    display: none;
}
.htmx-request .htmx-indicator {
    display: inline-block;
}
.htmx-request button[type="submit"] {
    opacity: 0.7;
    pointer-events: none;
}
```

This form:
1. Submits via htmx without page reload
2. Shows validation errors inline if submission fails
3. Displays a spinner during submission
4. Disables the button to prevent double-submission
5. Shows a success message with option to send another

### Comment Form with List Update

For forms that add items to a list, use `hx-swap="beforeend"` to append new items:

**Pages/Comments.cshtml:**

```html
@page
@model CommentsModel

<h1>Comments</h1>

<div id="comments-list">
    @foreach (var comment in Model.Comments)
    {
        <partial name="_Comment" model="comment" />
    }
</div>

<div id="comment-form-container">
    <partial name="_CommentForm" />
</div>
```

**Pages/Shared/_CommentForm.cshtml:**

```html
<form hx-post="/Comments?handler=Add" 
      hx-target="#comments-list" 
      hx-swap="beforeend"
      hx-on::after-request="if(event.detail.successful) this.reset()">
    @Html.AntiForgeryToken()
    
    <div class="form-group mb-3">
        <label for="author" class="form-label">Name</label>
        <input type="text" id="author" name="author" class="form-control" required />
    </div>
    
    <div class="form-group mb-3">
        <label for="message" class="form-label">Comment</label>
        <textarea id="message" name="message" class="form-control" rows="3" required></textarea>
    </div>
    
    <button type="submit" class="btn btn-primary">Post Comment</button>
</form>
```

**Pages/Comments.cshtml.cs:**

```csharp
public class CommentsModel : PageModel
{
    private readonly ICommentService _commentService;

    public CommentsModel(ICommentService commentService)
    {
        _commentService = commentService;
    }

    public List<Comment> Comments { get; set; } = new();

    public void OnGet()
    {
        Comments = _commentService.GetAll();
    }

    public IActionResult OnPostAdd(string author, string message)
    {
        if (string.IsNullOrWhiteSpace(author) || string.IsNullOrWhiteSpace(message))
        {
            Response.StatusCode = 400;
            return Content("<div class=\"alert alert-danger\">Name and comment are required.</div>", "text/html");
        }

        var comment = new Comment
        {
            Author = author,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };
        
        _commentService.Add(comment);
        
        return Partial("_Comment", comment);
    }
}
```

**Pages/Shared/_Comment.cshtml:**

```html
@model Comment

<div class="card mb-3" id="comment-@Model.Id">
    <div class="card-body">
        <h6 class="card-subtitle mb-2 text-muted">
            @Model.Author - @Model.CreatedAt.ToString("MMM d, yyyy h:mm tt")
        </h6>
        <p class="card-text">@Model.Message</p>
    </div>
</div>
```

The `hx-on::after-request` attribute clears the form after successful submission so users can immediately post another comment.

## Inline Editing

Allow users to edit content in place without navigating to a separate page:

**Pages/Shared/_UserRow.cshtml:**

```html
@model User

<tr id="user-@Model.Id">
    <td>@Model.Name</td>
    <td>@Model.Email</td>
    <td>
        <button hx-get="/Users?handler=EditForm&amp;id=@Model.Id"
                hx-target="#user-@Model.Id"
                hx-swap="outerHTML"
                class="btn btn-sm btn-outline-primary">
            Edit
        </button>
        <button hx-delete="/Users?handler=Delete&amp;id=@Model.Id"
                hx-target="#user-@Model.Id"
                hx-swap="outerHTML"
                hx-confirm="Delete @Model.Name?"
                class="btn btn-sm btn-outline-danger">
            Delete
        </button>
    </td>
</tr>
```

**Pages/Shared/_UserEditRow.cshtml:**

```html
@model User

<tr id="user-@Model.Id" class="table-active">
    <td>
        <input type="text" name="name" value="@Model.Name" class="form-control form-control-sm" required />
    </td>
    <td>
        <input type="email" name="email" value="@Model.Email" class="form-control form-control-sm" required />
    </td>
    <td>
        <button hx-put="/Users?handler=Update&amp;id=@Model.Id"
                hx-target="#user-@Model.Id"
                hx-swap="outerHTML"
                hx-include="closest tr"
                class="btn btn-sm btn-success">
            Save
        </button>
        <button hx-get="/Users?handler=CancelEdit&amp;id=@Model.Id"
                hx-target="#user-@Model.Id"
                hx-swap="outerHTML"
                class="btn btn-sm btn-secondary">
            Cancel
        </button>
    </td>
</tr>
```

**Pages/Users.cshtml.cs (handlers):**

```csharp
public IActionResult OnGetEditForm(int id)
{
    var user = _userService.GetById(id);
    if (user == null) return NotFound();
    return Partial("_UserEditRow", user);
}

public IActionResult OnGetCancelEdit(int id)
{
    var user = _userService.GetById(id);
    if (user == null) return NotFound();
    return Partial("_UserRow", user);
}

public IActionResult OnPutUpdate(int id, string name, string email)
{
    var user = _userService.GetById(id);
    if (user == null) return NotFound();

    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
    {
        Response.StatusCode = 400;
        return Partial("_UserEditRow", user);
    }

    user.Name = name;
    user.Email = email;
    _userService.Update(user);

    return Partial("_UserRow", user);
}

public IActionResult OnDeleteDelete(int id)
{
    var user = _userService.GetById(id);
    if (user == null) return NotFound();

    _userService.Delete(id);
    return Content("", "text/html");
}
```

## Modal Forms

Modal forms appear on demand without navigating away from the current page. They focus user attention and work well for quick actions like adding items or confirming operations.

### Modal Structure

First, add a modal container to your layout or page:

```html
<!-- Modal container - add to _Layout.cshtml or individual pages -->
<div id="modal" class="modal" _="on click if event.target is #modal remove .open from #modal">
    <div class="modal-backdrop"></div>
    <div class="modal-dialog">
        <div class="modal-header">
            <h5 id="modal-title">Modal</h5>
            <button type="button" class="btn-close" _="on click remove .open from #modal"></button>
        </div>
        <div id="modal-content" class="modal-body">
            <!-- Content loads here -->
        </div>
    </div>
</div>
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
    max-height: 90vh;
    overflow: auto;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
}

.modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem;
    border-bottom: 1px solid #dee2e6;
}

.modal-body {
    padding: 1rem;
}

.btn-close {
    background: none;
    border: none;
    font-size: 1.5rem;
    cursor: pointer;
    opacity: 0.5;
}

.btn-close:hover {
    opacity: 1;
}
```

### Opening the Modal

Trigger buttons load content into the modal and open it:

```html
<button hx-get="/Users?handler=CreateForm"
        hx-target="#modal-content"
        hx-swap="innerHTML"
        _="on htmx:afterSwap add .open to #modal
           on htmx:afterSwap set #modal-title.textContent to 'Add New User'">
    Add User
</button>
```

### Modal Form Content

**Pages/Shared/_UserCreateForm.cshtml:**

```html
@model UserCreateInput

<form hx-post="/Users?handler=Create"
      hx-target="#user-list"
      hx-swap="beforeend"
      _="on htmx:afterRequest if event.detail.successful remove .open from #modal">
    @Html.AntiForgeryToken()
    
    <div id="form-errors"></div>
    
    <div class="form-group mb-3">
        <label asp-for="Name" class="form-label"></label>
        <input asp-for="Name" class="form-control" />
        <span asp-validation-for="Name" class="text-danger"></span>
    </div>
    
    <div class="form-group mb-3">
        <label asp-for="Email" class="form-label"></label>
        <input asp-for="Email" class="form-control" />
        <span asp-validation-for="Email" class="text-danger"></span>
    </div>
    
    <div class="form-group mb-3">
        <label asp-for="Role" class="form-label"></label>
        <select asp-for="Role" class="form-select">
            <option value="">Select a role...</option>
            <option value="User">User</option>
            <option value="Admin">Admin</option>
        </select>
        <span asp-validation-for="Role" class="text-danger"></span>
    </div>
    
    <div class="d-flex justify-content-end gap-2">
        <button type="button" class="btn btn-secondary" _="on click remove .open from #modal">
            Cancel
        </button>
        <button type="submit" class="btn btn-primary">
            Create User
            <span class="htmx-indicator spinner-border spinner-border-sm ms-2"></span>
        </button>
    </div>
</form>
```

### Server-Side Modal Handlers

**Pages/Users.cshtml.cs:**

```csharp
public class UsersModel : PageModel
{
    private readonly IUserService _userService;

    public UsersModel(IUserService userService)
    {
        _userService = userService;
    }

    public List<User> Users { get; set; } = new();

    [BindProperty]
    public UserCreateInput CreateInput { get; set; } = new();

    public void OnGet()
    {
        Users = _userService.GetAll();
    }

    public IActionResult OnGetCreateForm()
    {
        return Partial("_UserCreateForm", new UserCreateInput());
    }

    public IActionResult OnPostCreate()
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;
            return Partial("_UserCreateForm", CreateInput);
        }

        var user = new User
        {
            Name = CreateInput.Name,
            Email = CreateInput.Email,
            Role = CreateInput.Role
        };
        
        _userService.Add(user);

        // Return the new user row to append to the list
        return Partial("_UserRow", user);
    }
}

public class UserCreateInput
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = string.Empty;
}
```

### Closing Modal with HX-Trigger Header

An alternative to Hyperscript is using the `HX-Trigger` response header:

```csharp
public IActionResult OnPostCreate()
{
    if (!ModelState.IsValid)
    {
        Response.StatusCode = 400;
        return Partial("_UserCreateForm", CreateInput);
    }

    var user = new User
    {
        Name = CreateInput.Name,
        Email = CreateInput.Email,
        Role = CreateInput.Role
    };
    
    _userService.Add(user);

    // Trigger modal close event
    Response.Headers.Append("HX-Trigger", "closeModal");

    return Partial("_UserRow", user);
}
```

Add a listener in your layout:

```html
<script>
document.body.addEventListener('closeModal', function() {
    document.getElementById('modal').classList.remove('open');
});
</script>
```

And simplify the form:

```html
<form hx-post="/Users?handler=Create"
      hx-target="#user-list"
      hx-swap="beforeend">
    @Html.AntiForgeryToken()
    <!-- fields -->
</form>
```

### Edit Modal

The same pattern works for editing existing items:

```html
<button hx-get="/Users?handler=EditForm&amp;id=@user.Id"
        hx-target="#modal-content"
        hx-swap="innerHTML"
        _="on htmx:afterSwap add .open to #modal
           on htmx:afterSwap set #modal-title.textContent to 'Edit User'">
    Edit
</button>
```

**Pages/Shared/_UserEditForm.cshtml:**

```html
@model User

<form hx-put="/Users?handler=ModalUpdate&amp;id=@Model.Id"
      hx-target="#user-@Model.Id"
      hx-swap="outerHTML"
      _="on htmx:afterRequest if event.detail.successful remove .open from #modal">
    @Html.AntiForgeryToken()
    
    <div class="form-group mb-3">
        <label for="name" class="form-label">Name</label>
        <input type="text" id="name" name="name" value="@Model.Name" class="form-control" required />
    </div>
    
    <div class="form-group mb-3">
        <label for="email" class="form-label">Email</label>
        <input type="email" id="email" name="email" value="@Model.Email" class="form-control" required />
    </div>
    
    <div class="d-flex justify-content-end gap-2">
        <button type="button" class="btn btn-secondary" _="on click remove .open from #modal">
            Cancel
        </button>
        <button type="submit" class="btn btn-primary">Save Changes</button>
    </div>
</form>
```

```csharp
public IActionResult OnGetEditForm(int id)
{
    var user = _userService.GetById(id);
    if (user == null) return NotFound();
    return Partial("_UserEditForm", user);
}

public IActionResult OnPutModalUpdate(int id, string name, string email)
{
    var user = _userService.GetById(id);
    if (user == null) return NotFound();

    user.Name = name;
    user.Email = email;
    _userService.Update(user);

    Response.Headers.Append("HX-Trigger", "closeModal");
    return Partial("_UserRow", user);
}
```

## Confirmation Dialogs

For dangerous actions, use `hx-confirm` for simple confirmations:

```html
<button hx-delete="/Items?handler=Delete&amp;id=@item.Id"
        hx-target="#item-@item.Id"
        hx-swap="outerHTML"
        hx-confirm="Delete '@item.Name'? This cannot be undone."
        class="btn btn-danger">
    Delete
</button>
```

For custom confirmation modals with more detail:

```html
<button hx-get="/Items?handler=ConfirmDelete&amp;id=@item.Id"
        hx-target="#modal-content"
        hx-swap="innerHTML"
        _="on htmx:afterSwap add .open to #modal">
    Delete
</button>
```

**Pages/Shared/_ConfirmDelete.cshtml:**

```html
@model Item

<div class="text-center">
    <h5>Delete Item?</h5>
    <p class="text-muted">You are about to delete "@Model.Name". This action cannot be undone.</p>
    
    <div class="d-flex justify-content-center gap-2 mt-4">
        <button type="button" class="btn btn-secondary" _="on click remove .open from #modal">
            Cancel
        </button>
        <button hx-delete="/Items?handler=Delete&amp;id=@Model.Id"
                hx-target="#item-@Model.Id"
                hx-swap="outerHTML"
                _="on htmx:afterRequest remove .open from #modal"
                class="btn btn-danger">
            Delete
        </button>
    </div>
</div>
```

## Form Validation Patterns

### Client-Side Validation

HTML5 validation attributes work with htmx. The form will not submit until validation passes:

```html
<input type="email" name="email" required pattern="[^@]+@[^@]+\.[^@]+" />
```

### Server-Side Validation with Error Display

Return the form partial with ModelState errors:

```csharp
public IActionResult OnPostSubmit()
{
    if (!ModelState.IsValid)
    {
        // Return form with validation errors highlighted
        return Partial("_ContactForm", Input);
    }
    
    // Process and return success
}
```

The partial uses tag helpers to display errors:

```html
<span asp-validation-for="Email" class="text-danger"></span>
```

### Validation Summary

For a summary of all errors:

```html
@if (!ViewData.ModelState.IsValid)
{
    <div class="alert alert-danger">
        <h6>Please fix the following errors:</h6>
        <ul class="mb-0">
            @foreach (var state in ViewData.ModelState.Values)
            {
                @foreach (var error in state.Errors)
                {
                    <li>@error.ErrorMessage</li>
                }
            }
        </ul>
    </div>
}
```

## Accessibility Considerations

Make your forms accessible:

### Labels and Inputs

Always associate labels with inputs:

```html
<label for="email" class="form-label">Email Address</label>
<input type="email" id="email" name="email" class="form-control" required aria-describedby="email-help" />
<div id="email-help" class="form-text">We'll never share your email.</div>
```

### Error Announcements

Use `aria-live` for dynamic error messages:

```html
<div id="form-errors" aria-live="polite">
    @if (!ViewData.ModelState.IsValid)
    {
        <div class="alert alert-danger" role="alert">
            Please correct the errors below.
        </div>
    }
</div>
```

### Focus Management in Modals

When a modal opens, focus should move to the first interactive element:

```html
<button hx-get="/Users?handler=CreateForm"
        hx-target="#modal-content"
        _="on htmx:afterSwap 
           add .open to #modal
           then wait 50ms
           then focus() on the first <input/> in #modal-content">
    Add User
</button>
```

## Reusable Form Components

Create partial views for common form patterns:

**Pages/Shared/_FormGroup.cshtml:**

```html
@model FormGroupModel

<div class="form-group mb-3">
    <label for="@Model.Id" class="form-label">@Model.Label</label>
    
    @if (Model.Type == "textarea")
    {
        <textarea id="@Model.Id" 
                  name="@Model.Name" 
                  class="form-control @(Model.HasError ? "is-invalid" : "")"
                  rows="@Model.Rows"
                  required="@Model.Required">@Model.Value</textarea>
    }
    else
    {
        <input type="@Model.Type" 
               id="@Model.Id" 
               name="@Model.Name" 
               value="@Model.Value"
               class="form-control @(Model.HasError ? "is-invalid" : "")"
               required="@Model.Required" />
    }
    
    @if (!string.IsNullOrEmpty(Model.HelpText))
    {
        <div class="form-text">@Model.HelpText</div>
    }
    
    @if (Model.HasError)
    {
        <div class="invalid-feedback">@Model.ErrorMessage</div>
    }
</div>
```

Use it across forms:

```html
<partial name="_FormGroup" model="new FormGroupModel { 
    Id = "email", 
    Name = "Email", 
    Label = "Email Address", 
    Type = "email", 
    Required = true 
}" />
```

## Summary

This chapter covered forms and modal forms with htmx:

- **Basic forms** submit with `hx-post` and swap responses without page reload
- **Anti-forgery tokens** are required in all forms using `@Html.AntiForgeryToken()`
- **Validation errors** display by returning the form partial with ModelState
- **Loading indicators** use `hx-indicator` to show progress
- **Comment/list patterns** use `hx-swap="beforeend"` to append items
- **Inline editing** swaps table rows between view and edit states
- **Modal forms** load content on demand and close on success
- **`HX-Trigger` header** provides server-controlled modal closing
- **`hx-confirm`** adds simple confirmation dialogs
- **Accessibility** requires proper labels, aria attributes, and focus management

These patterns combine into sophisticated form workflows while keeping your Razor Pages clean and maintainable.

## Preview of Next Chapter

Chapter 12 covers tabs and accordions, two classic UI patterns that organize content into collapsible or switchable sections. You will learn to build tab interfaces that load content on demand, accordions that expand and collapse smoothly, and navigation patterns that combine with the URL management techniques from Chapter 10.
