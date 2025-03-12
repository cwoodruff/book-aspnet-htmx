---
order: 26
icon: stack
label: Chap 5 - Mastering hx-get and hx-post
meta:
title: "Mastering `hx-get` and `hx-post`"
visibility: hidden
---
# 5

# Mastering `hx-get` and `hx-post`

Interactivity is at the heart of every modern web application, and in this chapter, we'll explore two of the most essential tools in htmx: `hx-get` and `hx-post`. These attributes allow your HTML elements to fetch and submit data dynamically, eliminating full-page reloads and making your Razor Pages feel fluid and responsive. Whether retrieving new content, handling form submissions, or updating elements on the fly, mastering these fundamental htmx commands is key to building a seamless user experience.

Unlike traditional AJAX requests that require JavaScript, `hx-get` and `hx-post` integrate directly into your HTML, making server communication as easy as adding an attribute. With hx-get, you can fetch new content from your server with a simple button click, while `hx-post` lets you submit forms asynchronously without disrupting the page flow. These lightweight yet powerful capabilities enable you to enhance user interactions while keeping your Razor Pages project clean, maintainable, and free from unnecessary JavaScript complexity.

By the end of this chapter, you'll have a solid grasp of how to use `hx-get` and `hx-post` effectively within an ASP.NET Core Razor Pages application. You'll learn how to work with Razor Pages endpoints, manage server responses, and even handle security considerations such as CSRF (Cross-Site Request Forgery) protection. Once you've mastered these core concepts, you'll be well on your way to building dynamic, server-driven web applications with minimal effort.

## Fetching Dynamic Content with hx-get

Interactivity is the foundation of any modern web application, and hx-get plays a crucial role in making server-side data retrieval seamless and efficient. Unlike traditional JavaScript-based AJAX requests, hx-get allows you to fetch content from your server using simple HTML attributes, making your Razor Pages more dynamic without the overhead of a JavaScript framework. Whether you're updating sections of a page, retrieving user details, or handling bulk operations, `hx-get` provides a declarative way to make your UI more responsive.

Using `hx-get`, you can dynamically load content from Razor Pages without requiring full-page reloads. Imagine a scenario where you have a user list, and clicking on a user should display their profile details without navigating away from the page. Instead of embedding all user details upfront, you can use `hx-get` to fetch the relevant data only when needed. Here's a simple example where clicking a button loads user profile information into a designated section:

```HTML
<button hx-get="/Users?handler=Profile&id=1" hx-target="#profile-details" hx-swap="innerHTML">View Profile</button>
<div id="profile-details"></div>
```

On the server side, you define the Razor Pages handler to fetch and return the profile data:

```C#
public class UsersModel : PageModel
{
    public IActionResult OnGetProfile(int id)
    {
        var user = _context.Users.Find(id);
        if (user == null) return NotFound();
    
        return Content($"<h3>{user.Name}</h3><p>Email: {user.Email}</p>", "text/html");
    }
}
```

In this setup, when the button is clicked, an `hx-get` request is made to the server, fetching the profile details and inserting them into the #profile-details div. The `hx-swap="innerHTML"` ensures that only the selected element is updated with new content, preventing unnecessary re-renders of the entire page.

Dynamic URL construction is another powerful feature of `hx-get`, allowing you to pass query parameters and modify requests based on user input. For example, if you have a search input that filters countries dynamically as the user types, `hx-get` can handle this efficiently:

```HTML
<input type="text" name="query" hx-get="/Index?handler=Search" hx-target="#search-results" hx-trigger="keyup changed delay:300ms" />
<div id="search-results"></div>
```

On the backend, the `OnGetSearch` handler processes the query parameter and returns the filtered results:

```C#
public IActionResult OnGetSearch(string query)
{
    var users = _context.Users
        .Where(u => u.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
        .Select(u => $"<p>{u.Name} ({u.Email})</p>")
        .ToList();

    return Content(string.Join("", users), "text/html");
}
```

This allows real-time updates without unnecessary full-page reloads. The `hx-trigger="keyup changed delay:300ms"` ensures that requests are only sent after the user pauses typing for 300 milliseconds, reducing server load and improving efficiency.

Beyond simple retrieval of content, `hx-get` can be used for bulk operations such as activating or deactivating multiple table entries at once. Consider an admin dashboard where users can select multiple accounts and activate or deactivate them with a single click. You can structure the table with checkboxes and a bulk action button:

_Index.cshtml_

```html
<div id="content">
    <!-- Begin Page Content -->
    <div class="container-fluid">
        <!-- Page Heading -->
        <div class="card shadow mb-4">
            <div class="card-header py-3">
                <h4 class="m-0 font-weight-bold text-primary">Bulk Update</h4>
            </div>
            <div class="card-body">

                <div hx-include="#checked-contacts" hx-target="#tbody">
                    <button class="btn btn-primary btn-icon-split" hx-put="/BulkUpdate/Index?handler=activate">Activate</button>
                    <button class="btn btn-primary btn-icon-split" hx-put="/BulkUpdate/Index?handler=deactivate">Deactivate</button>
                </div>

                @if (Model.ContactTableRows != null && Model.ContactTableRows.Count() > 0)
                {
                    <form id="checked-contacts">
                        @Html.AntiForgeryToken()
                        <table>
                            <thead>
                            <tr>
                                <th></th>
                                <th>Name</th>
                                <th>Email</th>
                                <th>Status</th>
                            </tr>
                            </thead>

                            <tbody id="tbody">
                            <partial name="BulkUpdate/_tbody" model="@Model.ContactTableRows"/>
                            </tbody>
                        </table>
                    </form>
                }

            </div>
        </div>

    </div>
    <!-- /.container-fluid -->
</div>
```

On the server side, the handler processes the selected user IDs and updates their status:

_Index.cshtml.cs_

```C#
public class Index : PageModel
{
    private readonly IContactService service;

    public Index(IContactService service)
    {
        this.service = service;
    }

    public List<Contact>? ContactTableRows { get; set; }

    public void OnGet()
    {
        ContactTableRows = service.Get().ToList();
    }


    public PartialViewResult OnPutActivate(int[] Ids)
    {
        foreach (var Id in Ids)
            service.Update(Id, true);
        var models = service.Get();
        foreach (var m in models)
            if (Ids.Contains(m.Id))
                m.Updated = true;
            else m.Updated = false;
        return Partial("_tbody", models.ToList());
    }

    public PartialViewResult OnPutDeactivate(int[] Ids)
    {
        foreach (var Id in Ids)
            service.Update(Id, false);
        var models = service.Get();
        foreach (var m in models)
            if (Ids.Contains(m.Id))
                m.Updated = true;
            else m.Updated = false;

        return Partial("_tbody", models.ToList());
    }
}
```

_IContactService.cs_

```C#
public interface IContactService
{
    IEnumerable<Contact> Get();
    void Update(int Id, bool Status);
}
```

_ContactService.cs_

```C#
{
    private Dictionary<int, Contact> contacts;

    public ContactService()
    {
        int key = 0;
        // Initialize the static contact member.
        contacts = new();
        contacts.Add(++key, new(key, "Bobby Jones", "bobby@jones.org"));
        contacts.Add(++key, new(key, "Sally Ride", "sally@apace.org"));
        contacts.Add(++key, new(key, "Brian Woodruff", "dr.brian@doctor.org"));
        contacts.Add(++key, new(key, "Spencer Woodruff", "spencer@woodruff.org") { Status = false });
    }

    public IEnumerable<Contact> Get()
    {
        return contacts.Select(c => c.Value);
    }

    public void Update(int Id, bool status)
    {
        contacts[Id].Status = status;
        contacts[Id].Updated = false;
    }
}
```

_Program.cs_

```C#
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddSingleton<htmx_examples.Pages.BulkUpdate.IContactService, htmx_examples.Pages.BulkUpdate.ContactService>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
```

This approach allows admins to perform bulk updates efficiently without refreshing the page.

With `hx-get`, you can build highly dynamic and interactive Razor Pages that fetch data only when needed, reducing unnecessary server load and improving performance. Whether you’re loading user profiles, implementing real-time search, or handling bulk updates, `hx-get` provides a powerful way to make your application more responsive while keeping your frontend code clean and maintainable.

## Seamless Form Submissions with `hx-post`

Handling form submissions is a fundamental aspect of any web application. The `hx-post` feature provides a clean and efficient way to submit data asynchronously without needing JavaScript. By integrating `hx-post` into your Razor Pages application, you can create forms that dynamically send data to the server, process responses, and update the user interface—all without requiring a full page reload. This approach keeps your frontend lightweight while delivering a modern and responsive user experience.

Consider a basic form that collects a user's name and email address. Instead of using traditional form submissions that reload the page, `hx-post` allows us to manage everything in the background. Here’s how you can implement it in Razor Pages:

```HTML
<form hx-post="/Users?handler=Register" hx-target="#response" hx-indicator="#loading">
    <label for="name">Name:</label>
    <input type="text" id="name" name="name" required />

    <label for="email">Email:</label>
    <input type="email" id="email" name="email" required />

    <button type="submit">Register</button>
</form>

<div id="loading" style="display: none;">Submitting...</div>
<div id="response"></div>
```

```C#
public class UsersModel : PageModel
{
    [BindProperty] public string Name { get; set; }
    [BindProperty] public string Email { get; set; }

    public IActionResult OnPostRegister()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email))
            return BadRequest("Both fields are required.");

        return Content($"<p>Thank you, {Name}. Your email {Email} has been registered!</p>", "text/html");
    }
}
```

In this setup, when the user submits the form, the `hx-post` attribute sends the data to the `OnPostRegister` handler, which processes the input and returns a response. The `hx-target="#response"` attribute ensures that only the relevant part of the page is updated, providing a smoother user experience. At the same time, the `hx-indicator="#loading"` attribute offers feedback to the user while the request is in progress.

For more advanced scenarios, you may want to send additional data or modify the behavior of the form dynamically. For example, if you're implementing a commenting system where users submit comments and see them appear instantly, you can use the `hx-swap="beforeend"` attribute to append new comments directly without reloading the page.

```HTML
<form hx-post="/Comments?handler=Add" hx-target="#comments" hx-swap="beforeend">
    <textarea name="comment" required></textarea>
    <button type="submit">Post Comment</button>
</form>

<div id="comments"></div>
```

```C#
public class CommentsModel : PageModel
{
    [BindProperty] public string Comment { get; set; }

    public IActionResult OnPostAdd()
    {
        if (string.IsNullOrWhiteSpace(Comment))
            return BadRequest("Comment cannot be empty.");

        return Content($"<p>{Comment}</p>", "text/html");
    }
}
```

Here, `hx-swap="beforeend"` ensures that each new comment is added to the existing list instead of replacing it. This small addition makes a big difference in the user experience, allowing for seamless interaction without requiring JavaScript.

Another powerful use case for `hx-post` is handling complex validation and dynamic error messages. Instead of performing client-side validation, you can let the server handle everything and return validation messages in real time. Here’s how you can modify the earlier registration form to display validation errors without reloading the page:

```HTML
<form hx-post="/Users?handler=Register" hx-target="#response" hx-indicator="#loading">
    <input type="text" name="name" placeholder="Name" required />
    <input type="email" name="email" placeholder="Email" required />
    <button type="submit">Register</button>
</form>

<div id="loading" style="display: none;">Submitting...</div>
<div id="response" class="error-message"></div>
```

```C#
public class UsersModel : PageModel
{
    [BindProperty] public string Name { get; set; }
    [BindProperty] public string Email { get; set; }

    public IActionResult OnPostRegister()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email))
            return BadRequest("<p class='error'>Both fields are required.</p>");

        if (!Email.Contains("@"))
            return BadRequest("<p class='error'>Please enter a valid email.</p>");

        return Content($"<p class='success'>Thank you, {Name}. You are now registered.</p>", "text/html");
    }
}
```

This setup ensures that validation feedback is displayed dynamically without requiring JavaScript. If the server detects an issue, it returns an error message, which is inserted into `#response`. Users receive immediate feedback without the disruption of a full-page reload.

For even greater flexibility, `hx-post` can be combined with hx-vals to send additional parameters programmatically. For instance, if you need to submit hidden values alongside user input, you can do it like this:

```HTML
<form hx-post="/Orders?handler=Submit" hx-target="#confirmation" hx-vals='{"productId": 1234, "quantity": 2}'>
    <button type="submit">Place Order</button>
</form>

<div id="confirmation"></div>
```

```C#
public class OrdersModel : PageModel
{
    public IActionResult OnPostSubmit(int productId, int quantity)
    {
        return Content($"<p>Order placed: Product {productId}, Quantity {quantity}.</p>", "text/html");
    }
}
```

This approach simplifies the process of passing additional data without the need for hidden form fields or JavaScript event handlers.

By using `hx-post`, you can create highly interactive, server-driven applications that effectively manage form submissions. Whether you are processing simple forms, dynamically adding content, or implementing real-time validation, `hx-post` enables you to accomplish all of this with minimal effort. Its seamless integration with Razor Pages ensures that your application remains clean, maintainable, and free from unnecessary JavaScript complexity.

## Securing `hx-post` Requests with Anti-Forgery Tokens

Security is crucial for any web application, particularly when it comes to handling form submissions. In ASP.NET Core, CSRF attacks are prevented through the use of Anti-Forgery Tokens. This measure ensures that malicious websites cannot trick users into submitting unauthorized requests. While traditional Razor Pages forms automatically incorporate this protection, htmx-based requests need explicit handling to effectively integrate `hx-post` with ASP.NET Core’s CSRF defenses.

ASP.NET Core applies anti-forgery validation to all POST, PUT, PATCH, and DELETE requests by default. When using `hx-post`, you must include the anti-forgery token to ensure successful validation. While this token is typically embedded in forms using the `@Html.AntiForgeryToken()` helper, htmx does not automatically send it. Therefore, you need to manually include the token with each `hx-post` request.

Here is an example of a secure form using hx-post with CSRF protection:

```HTML
<form hx-post="/Users?handler=Register" hx-target="#response">
    <input type="hidden" name="__RequestVerificationToken" value="@Html.AntiForgeryToken()" />

    <label for="name">Name:</label>
    <input type="text" id="name" name="name" required />

    <label for="email">Email:</label>
    <input type="email" id="email" name="email" required />

    <button type="submit">Register</button>
</form>

<div id="response"></div>
```

This form includes the anti-forgery token as a hidden input field, ensuring it is sent along with the request. However, this approach requires a manual update to every form. A more flexible solution is to dynamically inject the token into all htmx requests using JavaScript.

To automatically append the CSRF token to every `hx-post` request, you can use the `htmx:configRequest` event in JavaScript:

```HTML
<script>
document.body.addEventListener('htmx:configRequest', (event) => {
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    event.detail.headers['RequestVerificationToken'] = token;
});
</script>
```

This script monitors all htmx requests and automatically adds the CSRF token to the request headers. As a result, all hx-post submissions will include the necessary token without requiring any changes to individual forms.

On the server side, you must ensure that the request validation is enforced. ASP.NET Core provides the [ValidateAntiForgeryToken] attribute, which should be applied to the OnPost handler:

```C#
[ValidateAntiForgeryToken]
public class UsersModel : PageModel
{
    [BindProperty] public string Name { get; set; }
    [BindProperty] public string Email { get; set; }

    public IActionResult OnPostRegister()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email))
            return BadRequest("<p class='error'>Both fields are required.</p>");

        return Content($"<p class='success'>Thank you, {Name}. You are now registered.</p>", "text/html");
    }
}
```

When an hx-post request is sent without a valid anti-forgery token, the server responds with a **400 Bad Request** message. This mechanism ensures that only legitimate requests originating from your site are processed, effectively preventing CSRF attacks.

While disabling CSRF protection is not recommended in production, there are cases where you might want to bypass it temporarily, such as during local development or API testing. ASP.NET Core allows you to override token validation by applying the [IgnoreAntiforgeryToken] attribute to your handler. This should only be done when security is not a concern, such as testing isolated features.

```C#
[IgnoreAntiforgeryToken]
public IActionResult OnPostSubmit()
{
    return Content("<p class='success'>CSRF protection is disabled for testing purposes.</p>", "text/html");
}
```

By correctly implementing anti-forgery tokens, you can ensure that your htmx-enhanced Razor Pages applications are secure while still benefiting from the simplicity of `hx-post`. You can protect user data from malicious attacks by employing hidden fields or by dynamically appending tokens through JavaScript, all without compromising the seamless user experience that htmx offers.

## Laying the Groundwork for Complex Data Interactions

As you start working with more advanced data operations in your Razor Pages application, it's essential to establish a solid foundation for managing dynamic updates, state management, and real-time changes. While using `hx-get` and `hx-post` addresses the basics of data retrieval and submission, more complex scenarios necessitate a structured approach to modifying existing records, handling partial updates, and ensuring smooth user interactions. By setting up a flexible data layer and efficiently organizing your htmx requests, you'll find it much easier to implement future operations with `hx-put`, `hx-patch`, and `hx-delete`.

A well-designed API for advanced operations begins with a robust entity model. For example, if you’re managing a list of products in an inventory system, each product must support full updates (replacing all fields), partial updates (modifying only specific fields), and deletions. Here’s a simple model for managing products:

```C#
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
```

Instead of reloading the entire page when updating product information, htmx allows you to send targeted updates that modify only the necessary parts of the UI. A list of products can be rendered as follows:

```HTML
<table>
    <thead>
        <tr><th>Name</th><th>Price</th><th>Stock</th><th>Actions</th></tr>
    </thead>
    <tbody id="product-list">
        @foreach (var product in Model.Products)
        {
            <tr id="product-@product.Id">
                <td>@product.Name</td>
                <td>@product.Price</td>
                <td>@product.Stock</td>
                <td>
                    <button hx-get="/Products?handler=Edit&id=@product.Id" hx-target="#product-@product.Id">Edit</button>
                    <button hx-delete="/Products?handler=Delete&id=@product.Id" hx-target="#product-@product.Id" hx-swap="outerHTML">Delete</button>
                </td>
            </tr>
        }
    </tbody>
</table>
```

This setup allows each product row to include buttons for dynamically editing or deleting an item. When the "Edit" button is clicked, an `hx-get` request loads an editable form directly into the same row, enabling inline editing. In contrast, the `hx-delete` request completely removes the row after a successful deletion. To handle updates efficiently on the server, it's essential to define specific endpoints for retrieving and modifying product details.

The handler for the "Edit" button retrieves the selected product and returns a partial HTML response that contains the update form.

```C#
public IActionResult OnGetEdit(int id)
{
    var product = _context.Products.Find(id);
    if (product == null) return NotFound();

    return Partial("_ProductEdit", product);
}
```

```HTML
<!-- _ProductEdit.cshtml -->
<tr id="product-@Model.Id">
    <td><input type="text" name="name" value="@Model.Name" /></td>
    <td><input type="number" name="price" value="@Model.Price" step="0.01" /></td>
    <td><input type="number" name="stock" value="@Model.Stock" /></td>
    <td>
        <button hx-put="/Products?handler=Update&id=@Model.Id" hx-target="#product-@Model.Id" hx-include="closest tr">Save</button>
        <button hx-get="/Products?handler=Cancel&id=@Model.Id" hx-target="#product-@Model.Id">Cancel</button>
    </td>
</tr>
```

With `hx-include="closest tr"`, the form fields are automatically included in the `hx-put` request. The server-side update handler then processes the request and returns the updated row:

```C#
public IActionResult OnPutUpdate(int id, string name, decimal price, int stock)
{
    var product = _context.Products.Find(id);
    if (product == null) return NotFound();

    product.Name = name;
    product.Price = price;
    product.Stock = stock;
    _context.SaveChanges();

    return Partial("_ProductRow", product);
}
```

This method ensures that only the modified row is updated on the user interface, which prevents unnecessary page refreshes. By organizing your Razor Pages and htmx requests in this manner, you create a scalable foundation for effectively managing RESTful updates.

When working with `hx-post`, a successful operation typically results in an HTML snippet being returned to update the page. However, when an error occurs—such as missing form fields or a server issue—you need to provide meaningful feedback to the user. Consider a scenario where a user submits a form with required fields. If validation fails, the server should return an error message while keeping the form visible:

```C#
public IActionResult OnPostRegister(string name, string email)
{
    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        return BadRequest("<p class='error'>All fields are required.</p>");

    return Content($"<p class='success'>Thank you, {name}. Your registration is complete.</p>", "text/html");
}
```

The BadRequest response ensures that the form remains on the screen while displaying the error message dynamically. On the client side, `hx-target` can be used to direct this response into a dedicated error container, ensuring the UI remains responsive:

```HTML
<form hx-post="/Users?handler=Register" hx-target="#response">
    <input type="text" name="name" placeholder="Name" required />
    <input type="email" name="email" placeholder="Email" required />
    <button type="submit">Register</button>
</form>

<div id="response"></div>
```
Debugging network requests is an essential skill when working with htmx. Since requests are made asynchronously, errors may not always be immediately visible. Using browser developer tools, you can inspect network traffic, check request payloads, and view server responses. In Chrome or Edge, opening DevTools (F12) → Network tab allows you to filter requests by XHR and see the details of every hx-get or hx-post call. Common issues include missing anti-forgery tokens, incorrect content types, or unexpected 400/500 status codes from the server.

Performance considerations become critical when making repeated requests. If a page element triggers frequent `hx-get` or `hx-post` calls—such as a live search feature or an auto-refreshing dashboard—it’s important to avoid overwhelming the server. Using `hx-trigger` with a debounce delay ensures that requests are sent only when necessary. A search bar that updates results as the user types should use a short delay to prevent excessive queries:

```HTML
<input type="text" name="query" hx-get="/Users?handler=Search" hx-target="#results" hx-trigger="keyup changed delay:300ms" />
<div id="results"></div>
```

For scenarios where the same data is fetched multiple times, caching responses at the server or leveraging hx-history can improve performance. If a dashboard refreshes frequently, returning a `304 Not Modified` response when data hasn't changed can significantly reduce server load.

In the next chapter, we will delve into `hx-put`, `hx-patch`, and `hx-delete`, focusing on their roles in implementing full and partial updates within a RESTful architecture. These methods are essential for making your applications more dynamic, responsive, and in line with modern best practices.