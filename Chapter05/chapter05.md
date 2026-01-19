---
order: 26
icon: stack
label: Chap 5 - Mastering hx-get and hx-post
meta:
title: "Mastering hx-get and hx-post"
---

# Mastering `hx-get` and `hx-post`

The previous chapters introduced htmx commands and showed you how to build basic interactions. Now it is time to master the two commands you will use most often: `hx-get` and `hx-post`. These attributes handle the majority of web application interactions, from loading dynamic content to submitting forms and processing user input.

This chapter goes beyond the basics. You will learn how to construct dynamic URLs, include additional parameters with requests, handle file uploads, and manage server responses effectively. You will build real patterns: live search with debouncing, inline editing, bulk operations, and proper error handling. By the end, you will have the skills to build sophisticated server-driven interactions without writing JavaScript.

## Fetching Dynamic Content with `hx-get`

The `hx-get` attribute sends HTTP GET requests to your server and updates the page with the response. GET requests are ideal for retrieving data because they are cacheable, bookmarkable, and do not modify server state.

### Basic Content Loading

The simplest use of `hx-get` loads content when a user clicks a button:

```html
<button hx-get="/Users?handler=Profile&id=1" 
        hx-target="#profile-details">
    View Profile
</button>
<div id="profile-details"></div>
```

The server handler returns HTML:

```csharp
public class UsersModel : PageModel
{
    private readonly UserService _userService;

    public UsersModel(UserService userService)
    {
        _userService = userService;
    }

    public IActionResult OnGetProfile(int id)
    {
        var user = _userService.GetById(id);
        if (user == null)
        {
            return Content("<p class=\"error\">User not found</p>", "text/html");
        }

        return Partial("_UserProfile", user);
    }
}
```

The partial view renders the user data:

**Pages/Shared/_UserProfile.cshtml**

```html
@model User

<div class="user-profile">
    <h3>@Model.Name</h3>
    <p>Email: @Model.Email</p>
    <p>Member since: @Model.CreatedAt.ToString("MMMM yyyy")</p>
</div>
```

### Live Search with Debouncing

Search boxes that update as users type need careful handling. Without debouncing, every keystroke triggers a server request. The `hx-trigger` attribute with a delay prevents this:

```html
<div class="search-container">
    <input type="text" 
           name="query"
           placeholder="Search users..."
           hx-get="/Users?handler=Search" 
           hx-target="#search-results" 
           hx-trigger="keyup changed delay:300ms"
           hx-indicator="#search-spinner" />
    <span id="search-spinner" class="htmx-indicator">Searching...</span>
</div>
<div id="search-results"></div>
```

The trigger expression breaks down as:
- `keyup`: Fire on keyup events
- `changed`: Only if the value actually changed
- `delay:300ms`: Wait 300ms after the last keystroke

The server handler filters and returns results:

```csharp
public IActionResult OnGetSearch(string query)
{
    if (string.IsNullOrWhiteSpace(query))
    {
        return Content("", "text/html");
    }

    var users = _userService.Search(query);
    return Partial("_UserSearchResults", users);
}
```

**Pages/Shared/_UserSearchResults.cshtml**

```html
@model IEnumerable<User>

@if (!Model.Any())
{
    <p class="no-results">No users found</p>
}
else
{
    <ul class="search-results">
        @foreach (var user in Model)
        {
            <li>
                <a hx-get="/Users?handler=Profile&amp;id=@user.Id"
                   hx-target="#profile-details">
                    @user.Name
                </a>
                <span class="email">@user.Email</span>
            </li>
        }
    </ul>
}
```

### Passing Parameters with `hx-vals`

Sometimes you need to send additional data with a GET request beyond what is in the URL. The `hx-vals` attribute adds JSON-formatted values:

```html
<button hx-get="/Products?handler=Filter"
        hx-target="#product-list"
        hx-vals='{"category": "electronics", "inStock": true}'>
    Show Electronics
</button>
```

The handler receives these as parameters:

```csharp
public IActionResult OnGetFilter(string category, bool inStock)
{
    var products = _productService.Filter(category, inStock);
    return Partial("_ProductList", products);
}
```

### Including Form Fields with `hx-include`

When you need to include values from form fields outside the triggering element, use `hx-include`:

```html
<div class="filter-controls">
    <select id="category-select" name="category">
        <option value="">All Categories</option>
        <option value="electronics">Electronics</option>
        <option value="clothing">Clothing</option>
    </select>
    
    <input type="number" id="max-price" name="maxPrice" placeholder="Max price" />
    
    <button hx-get="/Products?handler=Filter"
            hx-target="#product-list"
            hx-include="#category-select, #max-price">
        Apply Filters
    </button>
</div>

<div id="product-list"></div>
```

The `hx-include` selector can be:
- An ID: `#category-select`
- Multiple selectors: `#category-select, #max-price`
- A CSS selector: `.filter-input`
- `closest form`: Include all fields in the nearest ancestor form
- `this`: Include the triggering element itself

### Caching GET Responses

For data that does not change frequently, server-side caching reduces load:

```csharp
public IActionResult OnGetCategories()
{
    Response.Headers.Append("Cache-Control", "public, max-age=300");
    
    var categories = _categoryService.GetAll();
    return Partial("_CategoryList", categories);
}
```

The browser caches this response for 5 minutes, avoiding redundant server requests.

## Submitting Data with `hx-post`

The `hx-post` attribute sends HTTP POST requests, typically for creating resources or submitting forms. POST requests can modify server state and require anti-forgery protection in ASP.NET Core.

### Setting Up Anti-Forgery Token Handling

Before any POST examples will work, configure htmx to include anti-forgery tokens. Add this to your `_Layout.cshtml`:

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

### Basic Form Submission

With token handling configured, forms submit without page reloads:

```html
<form hx-post="/Contact?handler=Submit" hx-target="#response">
    @Html.AntiForgeryToken()
    <div class="form-group">
        <label for="name">Name</label>
        <input type="text" id="name" name="Name" required />
    </div>
    <div class="form-group">
        <label for="email">Email</label>
        <input type="email" id="email" name="Email" required />
    </div>
    <div class="form-group">
        <label for="message">Message</label>
        <textarea id="message" name="Message" required></textarea>
    </div>
    <button type="submit">Send Message</button>
</form>
<div id="response"></div>
```

The handler processes the submission:

```csharp
public class ContactModel : PageModel
{
    private readonly IEmailService _emailService;

    public ContactModel(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostSubmit(ContactForm form)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("<br>",
                ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
            return Content($"<div class=\"error\">{errors}</div>", "text/html");
        }

        await _emailService.SendContactEmailAsync(form);
        
        return Content("<div class=\"success\">Thank you for your message. We will respond shortly.</div>", "text/html");
    }
}

public class ContactForm
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;
}
```

### Controlling Which Parameters Are Sent

The `hx-params` attribute controls which form fields are included in the request:

```html
<!-- Include only specific fields -->
<form hx-post="/Users?handler=UpdateEmail" hx-params="email">
    @Html.AntiForgeryToken()
    <input type="text" name="name" />
    <input type="email" name="email" />
    <input type="text" name="phone" />
    <button type="submit">Update Email Only</button>
</form>

<!-- Include all fields (default for forms) -->
<form hx-post="/Users?handler=UpdateAll" hx-params="*">
    @Html.AntiForgeryToken()
    <!-- fields -->
</form>

<!-- Exclude specific fields -->
<form hx-post="/Users?handler=UpdateProfile" hx-params="not password,confirmPassword">
    @Html.AntiForgeryToken()
    <!-- fields -->
</form>
```

### File Uploads

File uploads require the `hx-encoding` attribute to set the correct content type:

```html
<form hx-post="/Documents?handler=Upload"
      hx-encoding="multipart/form-data"
      hx-target="#upload-result">
    @Html.AntiForgeryToken()
    <div class="form-group">
        <label for="file">Select File</label>
        <input type="file" id="file" name="file" required />
    </div>
    <div class="form-group">
        <label for="description">Description</label>
        <input type="text" id="description" name="description" />
    </div>
    <button type="submit">Upload</button>
</form>
<div id="upload-result"></div>
```

The handler receives the file:

```csharp
public async Task<IActionResult> OnPostUpload(IFormFile file, string description)
{
    if (file == null || file.Length == 0)
    {
        return Content("<div class=\"error\">Please select a file</div>", "text/html");
    }

    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt" };
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    
    if (!allowedExtensions.Contains(extension))
    {
        return Content("<div class=\"error\">Invalid file type</div>", "text/html");
    }

    var fileName = $"{Guid.NewGuid()}{extension}";
    var filePath = Path.Combine(_environment.WebRootPath, "uploads", fileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    var document = new Document
    {
        FileName = fileName,
        OriginalName = file.FileName,
        Description = description,
        UploadedAt = DateTime.UtcNow
    };
    
    _documentService.Add(document);

    return Content($"<div class=\"success\">Uploaded: {file.FileName}</div>", "text/html");
}
```

### Buttons Outside Forms

Sometimes you need a button to POST data without being inside a form. Use `hx-vals` to send the data:

```html
@Html.AntiForgeryToken()

<button hx-post="/Cart?handler=Add"
        hx-vals='{"productId": @product.Id, "quantity": 1}'
        hx-target="#cart-count"
        hx-swap="innerHTML">
    Add to Cart
</button>

<span id="cart-count">@Model.CartItemCount</span>
```

```csharp
public IActionResult OnPostAdd(int productId, int quantity)
{
    _cartService.AddItem(productId, quantity);
    var count = _cartService.GetItemCount();
    return Content(count.ToString(), "text/html");
}
```

## Bulk Operations

Admin interfaces often need to operate on multiple items at once. This pattern uses checkboxes with `hx-include` to send selected IDs:

```html
@page
@model BulkOperationsModel

<h1>User Management</h1>

<div class="bulk-actions" hx-include="#user-table" hx-target="#user-list">
    @Html.AntiForgeryToken()
    <button hx-put="/Admin/Users?handler=Activate" class="btn btn-success">
        Activate Selected
    </button>
    <button hx-put="/Admin/Users?handler=Deactivate" class="btn btn-warning">
        Deactivate Selected
    </button>
    <button hx-delete="/Admin/Users?handler=Delete" 
            hx-confirm="Delete selected users?"
            class="btn btn-danger">
        Delete Selected
    </button>
</div>

<form id="user-table">
    <table class="table">
        <thead>
            <tr>
                <th><input type="checkbox" id="select-all" /></th>
                <th>Name</th>
                <th>Email</th>
                <th>Status</th>
            </tr>
        </thead>
        <tbody id="user-list">
            @foreach (var user in Model.Users)
            {
                <partial name="_UserRow" model="user" />
            }
        </tbody>
    </table>
</form>

<script>
document.getElementById('select-all').addEventListener('change', function() {
    var checkboxes = document.querySelectorAll('input[name="ids"]');
    checkboxes.forEach(cb => cb.checked = this.checked);
});
</script>
```

**Pages/Shared/_UserRow.cshtml**

```html
@model User

<tr id="user-@Model.Id" class="@(Model.IsActive ? "" : "inactive")">
    <td><input type="checkbox" name="ids" value="@Model.Id" /></td>
    <td>@Model.Name</td>
    <td>@Model.Email</td>
    <td>
        <span class="badge @(Model.IsActive ? "badge-success" : "badge-secondary")">
            @(Model.IsActive ? "Active" : "Inactive")
        </span>
    </td>
</tr>
```

The handlers process the selected IDs:

```csharp
public class UsersModel : PageModel
{
    private readonly IUserService _userService;

    public UsersModel(IUserService userService)
    {
        _userService = userService;
    }

    public List<User> Users { get; set; } = new();

    public void OnGet()
    {
        Users = _userService.GetAll();
    }

    public IActionResult OnPutActivate(int[] ids)
    {
        if (ids == null || ids.Length == 0)
        {
            return Content("<tr><td colspan=\"4\">No users selected</td></tr>", "text/html");
        }

        _userService.SetActiveStatus(ids, true);
        var users = _userService.GetByIds(ids);
        
        return Partial("_UserRows", users);
    }

    public IActionResult OnPutDeactivate(int[] ids)
    {
        if (ids == null || ids.Length == 0)
        {
            return Content("<tr><td colspan=\"4\">No users selected</td></tr>", "text/html");
        }

        _userService.SetActiveStatus(ids, false);
        var users = _userService.GetByIds(ids);
        
        return Partial("_UserRows", users);
    }

    public IActionResult OnDeleteDelete(int[] ids)
    {
        if (ids == null || ids.Length == 0)
        {
            return Content("<tr><td colspan=\"4\">No users selected</td></tr>", "text/html");
        }

        _userService.Delete(ids);
        
        // Return remaining users
        var remainingUsers = _userService.GetAll();
        return Partial("_UserRows", remainingUsers);
    }
}
```

**Pages/Shared/_UserRows.cshtml**

```html
@model IEnumerable<User>

@foreach (var user in Model)
{
    <partial name="_UserRow" model="user" />
}
```

## Inline Editing

Inline editing lets users modify data directly in a table without navigating to a separate page:

```html
<table class="table">
    <thead>
        <tr>
            <th>Product</th>
            <th>Price</th>
            <th>Stock</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody id="product-list">
        @foreach (var product in Model.Products)
        {
            <partial name="_ProductRow" model="product" />
        }
    </tbody>
</table>
```

**Pages/Shared/_ProductRow.cshtml**

```html
@model Product

<tr id="product-@Model.Id">
    <td>@Model.Name</td>
    <td>@Model.Price.ToString("C")</td>
    <td>@Model.Stock</td>
    <td>
        <button hx-get="/Products?handler=Edit&amp;id=@Model.Id"
                hx-target="#product-@Model.Id"
                hx-swap="outerHTML"
                class="btn btn-sm btn-primary">
            Edit
        </button>
    </td>
</tr>
```

**Pages/Shared/_ProductEditRow.cshtml**

```html
@model Product

<tr id="product-@Model.Id" class="editing">
    <td>
        <input type="text" name="name" value="@Model.Name" class="form-control" />
    </td>
    <td>
        <input type="number" name="price" value="@Model.Price" step="0.01" class="form-control" />
    </td>
    <td>
        <input type="number" name="stock" value="@Model.Stock" class="form-control" />
    </td>
    <td>
        <button hx-put="/Products?handler=Update&amp;id=@Model.Id"
                hx-target="#product-@Model.Id"
                hx-swap="outerHTML"
                hx-include="closest tr"
                class="btn btn-sm btn-success">
            Save
        </button>
        <button hx-get="/Products?handler=Cancel&amp;id=@Model.Id"
                hx-target="#product-@Model.Id"
                hx-swap="outerHTML"
                class="btn btn-sm btn-secondary">
            Cancel
        </button>
    </td>
</tr>
```

The handlers switch between view and edit modes:

```csharp
public IActionResult OnGetEdit(int id)
{
    var product = _productService.GetById(id);
    if (product == null) return NotFound();

    return Partial("_ProductEditRow", product);
}

public IActionResult OnGetCancel(int id)
{
    var product = _productService.GetById(id);
    if (product == null) return NotFound();

    return Partial("_ProductRow", product);
}

public IActionResult OnPutUpdate(int id, string name, decimal price, int stock)
{
    var product = _productService.GetById(id);
    if (product == null) return NotFound();

    product.Name = name;
    product.Price = price;
    product.Stock = stock;
    
    _productService.Update(product);

    return Partial("_ProductRow", product);
}
```

The `hx-include="closest tr"` on the Save button includes all input fields from the table row in the PUT request.

## Error Handling

Users need clear feedback when something goes wrong. Return appropriate HTTP status codes and error messages:

```csharp
public IActionResult OnPostRegister(string name, string email)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        Response.StatusCode = 400;
        return Content("<div class=\"error\">Name is required</div>", "text/html");
    }

    if (string.IsNullOrWhiteSpace(email))
    {
        Response.StatusCode = 400;
        return Content("<div class=\"error\">Email is required</div>", "text/html");
    }

    if (_userService.EmailExists(email))
    {
        Response.StatusCode = 409;
        return Content("<div class=\"error\">This email is already registered</div>", "text/html");
    }

    try
    {
        _userService.Register(name, email);
        return Content("<div class=\"success\">Registration complete! Check your email.</div>", "text/html");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Registration failed for {Email}", email);
        Response.StatusCode = 500;
        return Content("<div class=\"error\">An unexpected error occurred. Please try again.</div>", "text/html");
    }
}
```

On the client side, you can handle errors with htmx events:

```html
<form hx-post="/Users?handler=Register" 
      hx-target="#response"
      hx-target-error="#error-container">
    @Html.AntiForgeryToken()
    <input type="text" name="name" required />
    <input type="email" name="email" required />
    <button type="submit">Register</button>
</form>

<div id="response"></div>
<div id="error-container"></div>

<script>
document.body.addEventListener('htmx:responseError', function(event) {
    console.error('Request failed:', event.detail.xhr.status);
});
</script>
```

## Loading Indicators

Users need visual feedback during requests. The `hx-indicator` attribute shows an element while the request is in progress:

```html
<button hx-post="/Reports?handler=Generate"
        hx-target="#report-content"
        hx-indicator="#report-spinner">
    Generate Report
</button>

<span id="report-spinner" class="htmx-indicator">
    <img src="/images/spinner.gif" alt="Loading..." />
    Generating report...
</span>

<div id="report-content"></div>
```

Add CSS to hide indicators by default:

```css
.htmx-indicator {
    display: none;
}

.htmx-request .htmx-indicator {
    display: inline;
}

.htmx-request.htmx-indicator {
    display: inline;
}
```

## Debugging htmx Requests

When requests do not work as expected, use these debugging techniques:

### Browser Network Tab

Open Developer Tools (F12) and go to the Network tab. Filter by XHR/Fetch to see htmx requests. Check:

- Request URL and method
- Request headers (look for RequestVerificationToken)
- Request body (form data)
- Response status code
- Response body

### htmx Logging

Enable verbose logging during development:

```html
<script>
htmx.logAll();
</script>
```

### Common Issues

**400 Bad Request**

Usually a missing anti-forgery token. Ensure `@Html.AntiForgeryToken()` is present and the token-forwarding script is in your layout.

**404 Not Found**

The handler name does not match your method. `handler=Submit` requires `OnPostSubmit()` for POST requests.

**Empty Response**

The handler might be returning `null` or the wrong content type. Verify the handler returns `Content(..., "text/html")` or `Partial(...)`.

**Response Not Updating Target**

Check that the `hx-target` selector matches an element that exists. Verify the element has the correct `id`.

## Summary

This chapter covered `hx-get` and `hx-post` in depth:

- `hx-get` retrieves data with GET requests, ideal for search, filtering, and loading content
- `hx-post` submits data with POST requests, requiring anti-forgery token handling
- `hx-vals` adds JSON data to requests
- `hx-include` includes form fields from outside the triggering element
- `hx-params` controls which form fields are sent
- `hx-encoding` enables file uploads with multipart form data
- `hx-indicator` shows loading feedback during requests

You built patterns for live search, bulk operations, inline editing, file uploads, and error handling. These patterns form the foundation for most web application interactions.

## Preview of Next Chapter

Chapter 6 explores `hx-put`, `hx-patch`, and `hx-delete` for RESTful update and delete operations. You will learn when to use each method, how to handle partial updates, and patterns for optimistic UI updates that make your application feel responsive.
