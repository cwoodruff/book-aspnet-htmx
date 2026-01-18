---
order: 12
icon: stack
label: Chap 19 - Optimizing Requests with hx-disabled-elt, hx-request, and hx-sync
meta:
title: "Optimizing Requests with hx-disabled-elt, hx-request, and hx-sync"
---

# Optimizing Requests with `hx-disabled-elt`, `hx-request`, and `hx-sync`

Every click, every keystroke, every interaction in your application translates to a potential server request. Most of the time, that is exactly what you want. But what happens when users click a submit button five times because they are impatient? What happens when two requests race each other and the slower one overwrites fresher data? What happens when your API expects specific headers or timeout settings that htmx does not send by default?

These problems will find you eventually. The question is: do you have the tools to handle them?

This chapter shifts focus from visual interactivity to request behavior. You have already learned how to cache history, preserve state, and build responsive interfaces. Now you will learn how to control the requests themselves. Three htmx attributes give you that control: `hx-disabled-elt` prevents duplicate submissions by disabling elements during requests, `hx-request` configures timeout and credential settings, and `hx-sync` coordinates multiple requests to prevent race conditions.

By the end of this chapter, you will know how to build forms that cannot be double-submitted, configure requests for cross-origin authentication, and synchronize competing interactions so they do not corrupt your data. These are the details that separate amateur implementations from production-ready applications.

## Preventing Double Submissions with `hx-disabled-elt`

Users click buttons more than once. They do it when the network is slow. They do it when nothing appears to happen. They do it out of habit. Each extra click can trigger another request to your server, creating duplicate database entries, charging credit cards twice, or sending the same email multiple times.

The `hx-disabled-elt` attribute solves this problem by disabling specified elements while a request is in flight. When the request completes, htmx automatically re-enables the elements. The user sees immediate feedback that their action registered, and your server handles exactly one request.

Here is a feedback form that uses `hx-disabled-elt` to prevent duplicate submissions:

```html
<form hx-post="/Feedback?handler=Submit"
      hx-target="#result"
      hx-disabled-elt="find button[type='submit']">
    <textarea name="Message" placeholder="Your feedback..."></textarea>
    <button type="submit">Send Feedback</button>
</form>

<div id="result"></div>
```

The `find button[type='submit']` expression tells htmx to locate the submit button within the form and disable it during the request. You can also use simpler selectors:

```html
<button type="submit" hx-disabled-elt="this">Send Feedback</button>
```

Using `this` as the selector disables the element that triggered the request. This works when the button itself carries the htmx attributes.

The server-side handler stays simple:

```csharp
public class FeedbackModel : PageModel
{
    public IActionResult OnPostSubmit([FromForm] string Message)
    {
        if (string.IsNullOrWhiteSpace(Message))
        {
            return Content("<p class=\"error\">Please enter your feedback.</p>", "text/html");
        }

        // Save to database or queue for processing
        _feedbackService.Save(Message);
        
        return Content("<p class=\"success\">Thanks for your feedback!</p>", "text/html");
    }
}
```

Notice that the parameter name `Message` matches the form field name exactly. ASP.NET Core model binding is case-insensitive by default, but matching cases makes your code clearer.

### Disabling Multiple Elements

Sometimes you need to disable more than just the submit button. A form might have multiple action buttons, or you might want to disable the entire input area to prevent edits during submission:

```html
<form hx-post="/Order?handler=Place"
      hx-target="#order-result"
      hx-disabled-elt="#order-form-controls input, #order-form-controls button">
    <div id="order-form-controls">
        <input type="text" name="PromoCode" placeholder="Promo code (optional)" />
        <button type="submit" name="action" value="place">Place Order</button>
        <button type="submit" name="action" value="save">Save for Later</button>
    </div>
</form>

<div id="order-result"></div>
```

The comma-separated selector disables all inputs and buttons within the controls container. Users cannot modify the promo code or click either button while the order processes.

### Combining with Visual Feedback

Disabling elements prevents duplicate submissions, but users also need visual confirmation that something is happening. Combine `hx-disabled-elt` with `hx-indicator` for a complete solution:

```html
<form hx-post="/Payment?handler=Process"
      hx-target="#payment-result"
      hx-disabled-elt="this"
      hx-indicator="#payment-spinner">
    <input type="text" name="CardNumber" placeholder="Card number" />
    <button type="submit">
        Pay Now
        <span id="payment-spinner" class="htmx-indicator">Processing...</span>
    </button>
</form>

<div id="payment-result"></div>
```

The `htmx-indicator` class hides the spinner by default. When a request starts, htmx adds the `htmx-request` class to the element, and your CSS can reveal the spinner:

```css
.htmx-indicator {
    display: none;
}

.htmx-request .htmx-indicator {
    display: inline;
}

button:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}
```

Now users see the button dim and display "Processing..." while the payment request completes. They cannot click again, and they know the system received their action.

## Configuring Request Behavior with `hx-request`

The `hx-request` attribute lets you configure how htmx sends requests. Unlike what you might assume, this attribute focuses on three specific settings: timeout duration, credential handling, and whether to include htmx-specific headers.

### Setting Request Timeouts

By default, htmx does not enforce a timeout on requests. If your server hangs, the user waits indefinitely. The `timeout` option lets you set a maximum wait time in milliseconds:

```html
<button hx-get="/Reports?handler=Generate"
        hx-target="#report-content"
        hx-request='{"timeout": 30000}'>
    Generate Report
</button>

<div id="report-content"></div>
```

This request will abort after 30 seconds if the server has not responded. You can handle the timeout on the client with htmx events:

```html
<div id="report-container"
     hx-get="/Reports?handler=Generate"
     hx-target="#report-content"
     hx-request='{"timeout": 30000}'
     hx-trigger="click from:#generate-btn"
     _="on htmx:timeout put 'Request timed out. Please try again.' into #report-content">
    <button id="generate-btn">Generate Report</button>
    <div id="report-content"></div>
</div>
```

The Hyperscript listens for the `htmx:timeout` event and displays a friendly message when the request exceeds the time limit.

### Handling Credentials for Cross-Origin Requests

When your Razor Pages application calls APIs on different domains, browsers apply strict rules about cookies and authentication headers. The `credentials` option controls whether htmx includes credentials with cross-origin requests:

```html
<div hx-get="https://api.example.com/user/profile"
     hx-target="#profile-data"
     hx-request='{"credentials": true}'>
    Loading profile...
</div>

<div id="profile-data"></div>
```

Setting `credentials` to `true` is equivalent to setting `credentials: 'include'` on a fetch request. The browser will send cookies and HTTP authentication headers to the cross-origin server.

Your server must be configured to accept credentialed requests. In ASP.NET Core, this means setting up CORS properly:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCredentials", policy =>
    {
        policy.WithOrigins("https://yourapp.com")
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

Without the `AllowCredentials()` call and a specific origin (not wildcard), the browser will block the credentialed request.

### Suppressing htmx Headers

Every htmx request includes several custom headers: `HX-Request`, `HX-Trigger`, `HX-Target`, and others. These headers help your server understand the request context. In some situations, you might want to suppress them:

```html
<button hx-get="/external-api/data"
        hx-target="#external-data"
        hx-request='{"noHeaders": true}'>
    Fetch External Data
</button>
```

Setting `noHeaders` to `true` prevents htmx from adding its custom headers. This can be useful when calling third-party APIs that might reject requests with unfamiliar headers, or when you want requests to appear as standard browser requests rather than htmx-initiated ones.

### Combining Options

You can combine multiple options in a single `hx-request` attribute:

```html
<div hx-get="https://api.partner.com/inventory"
     hx-target="#inventory-list"
     hx-request='{"timeout": 10000, "credentials": true, "noHeaders": true}'
     hx-trigger="load">
    Loading inventory...
</div>
```

This request will timeout after 10 seconds, include credentials, and omit htmx headers.

## Adding Custom Headers with `hx-headers`

When you need to send custom headers with your requests, use `hx-headers` rather than trying to configure them through `hx-request`. This attribute accepts a JSON object of header names and values:

```html
<button hx-post="/api/Admin?handler=Action"
        hx-target="#admin-result"
        hx-headers='{"X-Admin-Token": "secure-token-value"}'>
    Execute Admin Action
</button>

<div id="admin-result"></div>
```

The server receives the custom header and can validate it:

```csharp
public class AdminModel : PageModel
{
    private readonly IConfiguration _configuration;

    public AdminModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult OnPostAction()
    {
        var providedToken = Request.Headers["X-Admin-Token"].ToString();
        var expectedToken = _configuration["AdminToken"];

        if (string.IsNullOrEmpty(providedToken) || 
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedToken),
                Encoding.UTF8.GetBytes(expectedToken)))
        {
            return Unauthorized();
        }

        // Perform admin action
        return Content("<p>Action completed successfully.</p>", "text/html");
    }
}
```

The `CryptographicOperations.FixedTimeEquals` method prevents timing attacks by comparing strings in constant time. Never use simple string equality for security tokens.

### Dynamic Headers

For headers that change based on user state or page context, you can set them dynamically using JavaScript:

```html
<button hx-post="/api/Data?handler=Fetch"
        hx-target="#data-result"
        id="fetch-btn">
    Fetch Data
</button>

<script>
document.getElementById('fetch-btn').addEventListener('htmx:configRequest', function(event) {
    event.detail.headers['X-Request-Time'] = new Date().toISOString();
    event.detail.headers['X-User-Timezone'] = Intl.DateTimeFormat().resolvedOptions().timeZone;
});
</script>
```

The `htmx:configRequest` event fires before each request, allowing you to modify headers, parameters, or other request details.

### Security Considerations

Placing tokens directly in HTML attributes means anyone can view them by inspecting the page source. For truly sensitive operations:

1. Use server-side sessions and cookies rather than client-visible tokens
2. Implement proper authentication middleware
3. Consider using `hx-vals` to send data that gets validated server-side against the user's session

```html
<form hx-post="/SecureAction?handler=Execute"
      hx-target="#result"
      hx-headers='{"X-CSRF-Token": "@Html.AntiForgeryToken()"}'>
    <button type="submit">Execute</button>
</form>
```

This approach embeds the anti-forgery token in a header rather than a hidden field, which some security configurations require.

## Coordinating Competing Requests with `hx-sync`

Race conditions happen when multiple requests compete and the results arrive out of order. Picture a search box that sends a request on every keystroke. The user types "cat", generating requests for "c", "ca", and "cat". If the "ca" response arrives after the "cat" response, the user sees results for "ca" instead of their complete query.

The `hx-sync` attribute prevents these problems by coordinating requests from related elements. It specifies a synchronization strategy that determines how competing requests should be handled.

### Basic Synchronization Strategies

The `hx-sync` attribute takes two parts: a CSS selector identifying the synchronization scope, and a strategy keyword.

**Drop Strategy** - Ignore new requests while one is in flight:

```html
<input type="text" 
       name="query"
       hx-get="/Search?handler=Results"
       hx-target="#search-results"
       hx-trigger="keyup changed delay:300ms"
       hx-sync="this:drop">

<div id="search-results"></div>
```

With `drop`, if a request is already running when a new one would start, the new request is simply ignored. This prevents request pileup but might miss the user's final input.

**Abort Strategy** - Cancel the in-flight request and start the new one:

```html
<input type="text" 
       name="query"
       hx-get="/Search?handler=Results"
       hx-target="#search-results"
       hx-trigger="keyup changed delay:300ms"
       hx-sync="this:abort">

<div id="search-results"></div>
```

With `abort`, the previous request is cancelled when a new one starts. This ensures users always see results for their most recent input. For search boxes, this is usually the right choice.

**Replace Strategy** - Same as abort, the default behavior:

```html
<input type="text" 
       name="query"
       hx-get="/Search?handler=Results"
       hx-target="#search-results"
       hx-trigger="keyup changed delay:300ms"
       hx-sync="this:replace">
```

The `replace` strategy is the default and behaves identically to `abort`. Explicitly stating it can make your intent clearer.

**Queue Strategies** - Process requests in order:

```html
<button hx-post="/Queue?handler=AddItem"
        hx-target="#queue-status"
        hx-sync="closest form:queue first">
    Add to Queue
</button>
```

Queue strategies process requests sequentially. Options include:
- `queue first` - Queue the first request, drop subsequent ones
- `queue last` - Queue only the most recent request
- `queue all` - Queue every request (use with caution)

### Synchronizing Multiple Elements

The CSS selector in `hx-sync` determines what scope to synchronize against. This lets you coordinate requests across related elements:

```html
<form id="product-filters">
    <select name="category"
            hx-get="/Products?handler=Filter"
            hx-target="#product-list"
            hx-trigger="change"
            hx-sync="#product-filters:abort">
        <option value="">All Categories</option>
        <option value="electronics">Electronics</option>
        <option value="clothing">Clothing</option>
    </select>

    <select name="sort"
            hx-get="/Products?handler=Filter"
            hx-target="#product-list"
            hx-trigger="change"
            hx-sync="#product-filters:abort">
        <option value="name">Name</option>
        <option value="price">Price</option>
        <option value="rating">Rating</option>
    </select>

    <input type="text"
           name="search"
           placeholder="Search..."
           hx-get="/Products?handler=Filter"
           hx-target="#product-list"
           hx-trigger="keyup changed delay:300ms"
           hx-sync="#product-filters:abort">
</form>

<div id="product-list"></div>
```

All three filter controls synchronize against the same form. If a user changes the category while a search request is running, the search request aborts and the category request proceeds. This ensures the product list always reflects the most recent filter state.

### Server-Side Handler for Synchronized Requests

The handler receives all filter parameters and returns the filtered results:

```csharp
public class ProductsModel : PageModel
{
    private readonly IProductRepository _products;

    public ProductsModel(IProductRepository products)
    {
        _products = products;
    }

    public IActionResult OnGetFilter(string category, string sort, string search)
    {
        var query = _products.GetAll();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category == category);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        query = sort switch
        {
            "price" => query.OrderBy(p => p.Price),
            "rating" => query.OrderByDescending(p => p.Rating),
            _ => query.OrderBy(p => p.Name)
        };

        return Partial("_ProductList", query.ToList());
    }
}
```

Because requests are synchronized on the client, you do not need to worry about race conditions corrupting the displayed data. The UI always reflects the most recent user selection.

## Combining Attributes for Production-Ready Forms

Real forms often need multiple optimizations working together. Here is a complete example that combines `hx-disabled-elt`, `hx-sync`, `hx-indicator`, and proper validation:

```html
<form id="registration-form"
      hx-post="/Register?handler=Submit"
      hx-target="#registration-result"
      hx-disabled-elt="#registration-form input, #registration-form button"
      hx-sync="this:abort"
      hx-indicator="#registration-spinner">
    
    <div class="form-group">
        <label for="email">Email</label>
        <input type="email" id="email" name="Email" required />
    </div>
    
    <div class="form-group">
        <label for="password">Password</label>
        <input type="password" id="password" name="Password" required minlength="8" />
    </div>
    
    <div class="form-group">
        <label for="confirm">Confirm Password</label>
        <input type="password" id="confirm" name="ConfirmPassword" required />
    </div>
    
    <button type="submit">
        Create Account
        <span id="registration-spinner" class="htmx-indicator">Creating...</span>
    </button>
</form>

<div id="registration-result"></div>
```

This form:
- Disables all inputs and the button during submission
- Aborts any in-flight request if the user somehow triggers another
- Shows a loading indicator in the button
- Uses HTML5 validation attributes for client-side checks

The server handler:

```csharp
public class RegisterModel : PageModel
{
    private readonly IUserService _userService;

    public RegisterModel(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IActionResult> OnPostSubmit([FromForm] RegistrationInput input)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("<br>", 
                ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
            return Content($"<div class=\"error\">{errors}</div>", "text/html");
        }

        if (input.Password != input.ConfirmPassword)
        {
            return Content("<div class=\"error\">Passwords do not match.</div>", "text/html");
        }

        var result = await _userService.CreateUserAsync(input.Email, input.Password);
        
        if (!result.Succeeded)
        {
            return Content($"<div class=\"error\">{result.ErrorMessage}</div>", "text/html");
        }

        return Content("<div class=\"success\">Account created! Check your email to verify.</div>", "text/html");
    }
}

public class RegistrationInput
{
    [Required, EmailAddress]
    public string Email { get; set; }
    
    [Required, MinLength(8)]
    public string Password { get; set; }
    
    [Required]
    public string ConfirmPassword { get; set; }
}
```

## Debugging Request Issues

When requests behave unexpectedly, htmx provides tools to understand what is happening.

### Browser Developer Tools

Open the Network tab in your browser's developer tools. Filter by XHR/Fetch to see only AJAX requests. For each request, examine:

- **Headers**: Verify custom headers are present and correct
- **Payload**: Confirm form data is being sent as expected
- **Timing**: Identify slow requests that might need timeouts
- **Response**: Check that the server returns valid HTML

### htmx Logging

Enable htmx logging to see events in the console:

```javascript
htmx.logAll();
```

This outputs every htmx event, which can be noisy but thorough. For targeted debugging, listen for specific events:

```javascript
document.body.addEventListener('htmx:beforeRequest', function(event) {
    console.log('Request starting:', event.detail);
});

document.body.addEventListener('htmx:afterRequest', function(event) {
    console.log('Request complete:', event.detail);
});

document.body.addEventListener('htmx:sendError', function(event) {
    console.error('Request failed:', event.detail);
});
```

### Common Issues and Solutions

**Problem**: Button does not re-enable after request completes.
**Solution**: Verify the request actually completes. Check for JavaScript errors. Ensure `hx-disabled-elt` selector matches the element.

**Problem**: Requests still race despite `hx-sync`.
**Solution**: Confirm all related elements use the same sync scope. The CSS selector must resolve to the same element for coordination to work.

**Problem**: Custom headers not appearing in requests.
**Solution**: Check for typos in `hx-headers` JSON. Verify the JSON is valid. Some proxies strip non-standard headers.

**Problem**: Credentials not sent to cross-origin API.
**Solution**: Ensure both `hx-request='{"credentials": true}'` on the client and `AllowCredentials()` in server CORS configuration.

## Summary

Request optimization separates functional applications from professional ones. Users expect buttons to work exactly once. They expect the most recent action to win. They expect the application to handle edge cases gracefully.

The three attributes covered in this chapter give you that control:

- `hx-disabled-elt` prevents duplicate submissions by disabling elements during requests
- `hx-request` configures timeout, credentials, and header behavior
- `hx-sync` coordinates competing requests to prevent race conditions

Use `hx-headers` when you need custom headers. Combine these attributes with `hx-indicator` for visual feedback. Test thoroughly with browser developer tools.

In the next chapter, we will explore advanced performance techniques including lazy loading, request batching, and strategies for minimizing latency in complex Razor Pages applications.
