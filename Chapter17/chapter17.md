---
order: 14
icon: stack
label: Chap 17 - Form Enhancements with hx-params, hx-vals, and hx-validate
meta:
title: "Form Enhancements with hx-params, hx-vals, and hx-validate"
---

# Form Enhancements with `hx-params`, `hx-vals`, and `hx-validate`

Forms collect data. htmx submits that data without page reloads. But real-world forms need more control. You need to exclude fields from submission, inject values that aren't visible in the form, and validate input before it reaches the server. This chapter covers three htmx attributes that give you that control: `hx-params` filters which fields get sent, `hx-vals` injects additional data into requests, and `hx-validate` triggers HTML5 validation before submission.

These attributes work together with the form patterns from Chapter 11. Combined with server-side validation, they create forms that guide users through correct input while keeping your validation logic where it belongs: on the server.

## Anti-Forgery Token Setup

All POST form examples in this chapter require anti-forgery tokens. Configure htmx to include them automatically:

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

## Filtering Parameters with `hx-params`

The `hx-params` attribute controls which form fields are included in a request. By default, htmx sends all fields. With `hx-params`, you can include all, exclude specific fields, or send none.

### `hx-params` Values

| Value | Behavior |
|-------|----------|
| `*` | Send all parameters (default) |
| `none` | Send no parameters |
| `not fieldName` | Send all except the named field |
| `fieldName` | Send only the named field |
| `field1, field2` | Send only these fields |

### Multi-Step Form Example

A wizard-style form where each step submits only its own fields:

**Pages/Signup.cshtml:**

```html
@page
@model SignupModel

<h1>Create Account</h1>

<div id="step-container">
    <partial name="_SignupStep1" />
</div>
```

**Pages/Shared/_SignupStep1.cshtml:**

```html
<form hx-post="/Signup?handler=Step1"
      hx-target="#step-container"
      hx-swap="innerHTML"
      hx-disabled-elt="find button">
    @Html.AntiForgeryToken()
    
    <h2>Step 1: Personal Information</h2>
    
    <div class="form-group mb-3">
        <label for="firstName">First Name</label>
        <input type="text" id="firstName" name="FirstName" class="form-control" required />
    </div>
    
    <div class="form-group mb-3">
        <label for="lastName">Last Name</label>
        <input type="text" id="lastName" name="LastName" class="form-control" required />
    </div>
    
    <button type="submit" class="btn btn-primary">
        Next Step
    </button>
</form>
```

**Pages/Shared/_SignupStep2.cshtml:**

```html
@model SignupStep2Model

<form hx-post="/Signup?handler=Step2"
      hx-target="#step-container"
      hx-swap="innerHTML"
      hx-disabled-elt="find button">
    @Html.AntiForgeryToken()
    
    <h2>Step 2: Account Details</h2>
    
    <!-- Hidden fields preserve previous step data -->
    <input type="hidden" name="FirstName" value="@Model.FirstName" />
    <input type="hidden" name="LastName" value="@Model.LastName" />
    
    <div class="form-group mb-3">
        <label for="email">Email</label>
        <input type="email" id="email" name="Email" class="form-control" required />
    </div>
    
    <div class="form-group mb-3">
        <label for="password">Password</label>
        <input type="password" id="password" name="Password" class="form-control" required minlength="8" />
    </div>
    
    <div class="d-flex gap-2">
        <button type="button" 
                hx-get="/Signup?handler=BackToStep1&firstName=@Model.FirstName&lastName=@Model.LastName"
                hx-target="#step-container"
                class="btn btn-secondary">
            Back
        </button>
        <button type="submit" class="btn btn-primary">
            Create Account
        </button>
    </div>
</form>
```

**Pages/Signup.cshtml.cs:**

```csharp
public class SignupModel : PageModel
{
    private readonly IUserService _userService;

    public SignupModel(IUserService userService)
    {
        _userService = userService;
    }

    public void OnGet() { }

    public IActionResult OnPostStep1(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            ModelState.AddModelError("", "First and last name are required");
            return Partial("_SignupStep1");
        }

        return Partial("_SignupStep2", new SignupStep2Model
        {
            FirstName = firstName,
            LastName = lastName
        });
    }

    public IActionResult OnGetBackToStep1(string firstName, string lastName)
    {
        return Partial("_SignupStep1WithValues", new SignupStep1Model
        {
            FirstName = firstName,
            LastName = lastName
        });
    }

    public IActionResult OnPostStep2(SignupInput input)
    {
        if (!ModelState.IsValid)
        {
            return Partial("_SignupStep2", new SignupStep2Model
            {
                FirstName = input.FirstName,
                LastName = input.LastName
            });
        }

        _userService.CreateAccount(input);
        return Partial("_SignupSuccess", input);
    }
}

public class SignupStep2Model
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}

public class SignupInput
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}
```

### Excluding Specific Fields

Use `hx-params="not fieldName"` to exclude fields:

```html
<form hx-post="/Search?handler=Execute" hx-target="#results">
    @Html.AntiForgeryToken()
    
    <input type="text" name="Query" />
    <input type="text" name="InternalTrackingId" value="abc123" />
    
    <!-- Don't send the tracking ID with this button -->
    <button type="submit" hx-params="not InternalTrackingId">
        Search
    </button>
    
    <!-- Send everything with this button -->
    <button type="submit" hx-params="*" name="action" value="search-and-track">
        Search with Tracking
    </button>
</form>
```

### Sending No Parameters

Use `hx-params="none"` for actions that don't need form data:

```html
<form hx-post="/Editor?handler=Save" hx-target="#save-status">
    @Html.AntiForgeryToken()
    
    <textarea name="Content" rows="10"></textarea>
    
    <button type="submit">Save Draft</button>
    
    <!-- Reset button doesn't need to send form data -->
    <button type="button"
            hx-post="/Editor?handler=Reset"
            hx-target="#editor-container"
            hx-params="none"
            hx-confirm="Discard all changes?">
        Reset
    </button>
</form>
```

## Injecting Values with `hx-vals`

The `hx-vals` attribute adds extra name-value pairs to requests. These values don't appear in the form but get sent to the server alongside form fields.

### Static Values

For fixed values, use JSON syntax:

```html
<button hx-post="/Feedback?handler=Submit"
        hx-target="#result"
        hx-vals='{"source": "homepage", "category": "general"}'>
    Submit Feedback
</button>
```

The server receives `source=homepage` and `category=general` as if they were form fields.

### Values from Razor

Inject server-side values:

```html
<button hx-post="/Order?handler=Reorder"
        hx-target="#order-status"
        hx-vals='{"orderId": "@Model.Order.Id", "userId": "@Model.UserId"}'>
    Reorder
</button>
```

**Rendered HTML:**

```html
<button hx-post="/Order?handler=Reorder"
        hx-target="#order-status"
        hx-vals='{"orderId": "12345", "userId": "user-abc"}'>
    Reorder
</button>
```

### Dynamic JavaScript Values

For values computed at request time, use the `js:` prefix:

```html
<button hx-post="/Analytics?handler=Track"
        hx-vals='js:{"timestamp": new Date().toISOString(), "screenWidth": window.innerWidth, "timezone": Intl.DateTimeFormat().resolvedOptions().timeZone}'>
    Track Event
</button>
```

The `js:` prefix tells htmx to evaluate the object as JavaScript when the request fires.

### Complete Feedback Form Example

**Pages/Feedback.cshtml:**

```html
@page
@model FeedbackModel

<h1>Send Feedback</h1>

<form hx-post="/Feedback?handler=Submit"
      hx-target="#feedback-result"
      hx-swap="outerHTML"
      hx-disabled-elt="find button">
    @Html.AntiForgeryToken()
    
    <div class="form-group mb-3">
        <label for="message">Your Feedback</label>
        <textarea id="message" name="Message" class="form-control" rows="5" required></textarea>
    </div>
    
    <div class="form-group mb-3">
        <label for="rating">Rating</label>
        <select id="rating" name="Rating" class="form-select">
            <option value="5">Excellent</option>
            <option value="4">Good</option>
            <option value="3">Average</option>
            <option value="2">Poor</option>
            <option value="1">Terrible</option>
        </select>
    </div>
    
    <button type="submit"
            hx-vals='js:{"submittedAt": new Date().toISOString(), "pageUrl": window.location.href, "userAgent": navigator.userAgent}'
            class="btn btn-primary">
        <span class="btn-text">Send Feedback</span>
        <span class="htmx-indicator">Sending...</span>
    </button>
</form>

<div id="feedback-result"></div>
```

**Pages/Feedback.cshtml.cs:**

```csharp
public class FeedbackModel : PageModel
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackModel(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    public void OnGet() { }

    public IActionResult OnPostSubmit(FeedbackInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Message))
        {
            return Content("<div class=\"alert alert-danger\">Message is required</div>", "text/html");
        }

        _feedbackService.Save(new Feedback
        {
            Message = input.Message,
            Rating = input.Rating,
            SubmittedAt = input.SubmittedAt,
            PageUrl = input.PageUrl,
            UserAgent = input.UserAgent
        });

        return Content("<div class=\"alert alert-success\">Thank you for your feedback!</div>", "text/html");
    }
}

public class FeedbackInput
{
    public string Message { get; set; } = "";
    public int Rating { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string PageUrl { get; set; } = "";
    public string UserAgent { get; set; } = "";
}
```

### Combining `hx-vals` with Form Data

Values from `hx-vals` merge with form fields:

```html
<form hx-post="/Products?handler=AddToCart" hx-target="#cart-feedback">
    @Html.AntiForgeryToken()
    
    <input type="number" name="Quantity" value="1" min="1" />
    
    <!-- Button adds productId and source to the form data -->
    <button type="submit"
            hx-vals='{"productId": "@product.Id", "source": "product-page"}'>
        Add to Cart
    </button>
</form>
```

The server receives: `Quantity` (from form) + `productId` + `source` (from hx-vals).

## HTML5 Validation with `hx-validate`

The `hx-validate` attribute triggers HTML5 form validation before htmx sends a request. If validation fails, the request is cancelled and the browser shows validation messages.

### Basic Usage

```html
<form hx-post="/Contact?handler=Submit"
      hx-target="#contact-result"
      hx-validate>
    @Html.AntiForgeryToken()
    
    <input type="text" name="Name" required />
    <input type="email" name="Email" required />
    <textarea name="Message" required minlength="10"></textarea>
    
    <button type="submit">Send</button>
</form>
```

Without `hx-validate`, htmx would send the request even if fields are empty. With `hx-validate`, the browser prevents submission and shows "Please fill out this field" messages.

### Validation Attributes

Common HTML5 validation attributes work with `hx-validate`:

```html
<input type="text" name="Username" 
       required 
       minlength="3" 
       maxlength="20" 
       pattern="[a-zA-Z0-9_]+" 
       title="Username must be 3-20 characters, letters, numbers, and underscores only" />

<input type="email" name="Email" required />

<input type="url" name="Website" />

<input type="number" name="Age" min="18" max="120" />

<input type="tel" name="Phone" pattern="[0-9]{3}-[0-9]{3}-[0-9]{4}" title="Format: 123-456-7890" />
```

### When to Use `hx-validate`

Use `hx-validate` on form submissions to catch obvious errors before hitting the server:

```html
<form hx-post="/Register?handler=Submit"
      hx-target="#form-container"
      hx-swap="outerHTML"
      hx-validate>
```

Don't use `hx-validate` on real-time validation triggers (like `keyup`). It would prevent the request from firing at all if the field is incomplete:

```html
<!-- DON'T do this - hx-validate blocks the request -->
<input hx-get="/Register?handler=CheckUsername"
       hx-trigger="keyup changed delay:500ms"
       hx-validate />  <!-- Remove this -->

<!-- DO this instead -->
<input hx-get="/Register?handler=CheckUsername"
       hx-trigger="keyup changed delay:500ms"
       hx-target="#username-feedback" />
```

## Real-Time Server Validation

For validation that requires server checks (username availability, email uniqueness), use `hx-get` with debounced triggers.

### Username Availability Check

**Pages/Register.cshtml:**

```html
@page
@model RegisterModel

<h1>Register</h1>

<form id="register-form"
      hx-post="/Register?handler=Submit"
      hx-target="#form-messages"
      hx-swap="innerHTML"
      hx-validate
      hx-disabled-elt="find button[type='submit']">
    @Html.AntiForgeryToken()
    
    <div class="form-group mb-3">
        <label for="username">Username</label>
        <div class="input-with-feedback">
            <input type="text" 
                   id="username" 
                   name="Username" 
                   class="form-control"
                   required
                   minlength="3"
                   maxlength="20"
                   pattern="[a-zA-Z0-9_]+"
                   hx-get="/Register?handler=CheckUsername"
                   hx-trigger="keyup changed delay:500ms"
                   hx-target="#username-feedback"
                   hx-swap="innerHTML"
                   hx-sync="this:replace"
                   hx-indicator="#username-spinner" />
            <span id="username-spinner" class="htmx-indicator">
                <span class="spinner-border spinner-border-sm"></span>
            </span>
        </div>
        <div id="username-feedback" class="form-feedback"></div>
    </div>
    
    <div class="form-group mb-3">
        <label for="email">Email</label>
        <div class="input-with-feedback">
            <input type="email" 
                   id="email" 
                   name="Email" 
                   class="form-control"
                   required
                   hx-get="/Register?handler=CheckEmail"
                   hx-trigger="keyup changed delay:500ms"
                   hx-target="#email-feedback"
                   hx-swap="innerHTML"
                   hx-sync="this:replace"
                   hx-indicator="#email-spinner" />
            <span id="email-spinner" class="htmx-indicator">
                <span class="spinner-border spinner-border-sm"></span>
            </span>
        </div>
        <div id="email-feedback" class="form-feedback"></div>
    </div>
    
    <div class="form-group mb-3">
        <label for="password">Password</label>
        <input type="password" 
               id="password" 
               name="Password" 
               class="form-control"
               required
               minlength="8" />
        <small class="form-text text-muted">At least 8 characters</small>
    </div>
    
    <div id="form-messages"></div>
    
    <button type="submit" class="btn btn-primary">
        <span class="btn-text">Create Account</span>
        <span class="htmx-indicator">Creating...</span>
    </button>
</form>
```

**Pages/Register.cshtml.cs:**

```csharp
public class RegisterModel : PageModel
{
    private readonly IUserService _userService;

    public RegisterModel(IUserService userService)
    {
        _userService = userService;
    }

    public void OnGet() { }

    public IActionResult OnGetCheckUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Content("", "text/html");
        }

        if (username.Length < 3)
        {
            return Content("<span class=\"text-warning\">Username must be at least 3 characters</span>", "text/html");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
        {
            return Content("<span class=\"text-danger\">Only letters, numbers, and underscores allowed</span>", "text/html");
        }

        if (_userService.UsernameExists(username))
        {
            return Content("<span class=\"text-danger\">Username is already taken</span>", "text/html");
        }

        return Content("<span class=\"text-success\">Username is available</span>", "text/html");
    }

    public IActionResult OnGetCheckEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Content("", "text/html");
        }

        if (!IsValidEmail(email))
        {
            return Content("<span class=\"text-danger\">Please enter a valid email address</span>", "text/html");
        }

        if (_userService.EmailExists(email))
        {
            return Content("<span class=\"text-danger\">An account with this email already exists</span>", "text/html");
        }

        return Content("<span class=\"text-success\">Email is available</span>", "text/html");
    }

    public IActionResult OnPostSubmit(RegisterInput input)
    {
        // Server-side validation (always validate again on submit)
        var errors = new List<string>();

        if (_userService.UsernameExists(input.Username))
        {
            errors.Add("Username is already taken");
        }

        if (_userService.EmailExists(input.Email))
        {
            errors.Add("Email is already registered");
        }

        if (input.Password.Length < 8)
        {
            errors.Add("Password must be at least 8 characters");
        }

        if (errors.Any())
        {
            return Partial("_RegisterErrors", errors);
        }

        _userService.Register(input);
        
        Response.Headers.Append("HX-Redirect", "/Register/Success");
        return Content("", "text/html");
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

public class RegisterInput
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}
```

**Pages/Shared/_RegisterErrors.cshtml:**

```html
@model List<string>

<div class="alert alert-danger">
    <ul class="mb-0">
        @foreach (var error in Model)
        {
            <li>@error</li>
        }
    </ul>
</div>
```

### Key Patterns for Real-Time Validation

**1. Debounce with delay:**

```html
hx-trigger="keyup changed delay:500ms"
```

Waits 500ms after typing stops before sending the request.

**2. Cancel in-flight requests:**

```html
hx-sync="this:replace"
```

If the user keeps typing, cancel the previous request and send a new one.

**3. Show loading state:**

```html
hx-indicator="#username-spinner"
```

Display a spinner while checking.

**4. Target feedback area:**

```html
hx-target="#username-feedback"
```

Put validation messages in a dedicated container.

### Validation CSS

```css
.input-with-feedback {
    position: relative;
    display: flex;
    align-items: center;
}

.input-with-feedback input {
    flex: 1;
}

.input-with-feedback .htmx-indicator {
    position: absolute;
    right: 10px;
    display: none;
}

.input-with-feedback .htmx-request ~ .htmx-indicator {
    display: inline-block;
}

.form-feedback {
    min-height: 1.5rem;
    font-size: 0.875rem;
    margin-top: 0.25rem;
}

.form-feedback .text-success {
    color: #198754;
}

.form-feedback .text-danger {
    color: #dc3545;
}

.form-feedback .text-warning {
    color: #ffc107;
}
```

## Combining All Three Attributes

A profile editor that uses all three attributes:

**Pages/Profile.cshtml:**

```html
@page
@model ProfileModel

<h1>Edit Profile</h1>

<form hx-post="/Profile?handler=Save"
      hx-target="#profile-messages"
      hx-swap="innerHTML"
      hx-validate
      hx-disabled-elt="find button[type='submit']">
    @Html.AntiForgeryToken()
    
    <div class="form-group mb-3">
        <label for="displayName">Display Name</label>
        <input type="text" 
               id="displayName"
               name="DisplayName" 
               value="@Model.Profile.DisplayName"
               class="form-control"
               required
               maxlength="50"
               hx-get="/Profile?handler=CheckDisplayName"
               hx-trigger="keyup changed delay:500ms"
               hx-target="#displayname-feedback"
               hx-sync="this:replace" />
        <div id="displayname-feedback" class="form-feedback"></div>
    </div>
    
    <div class="form-group mb-3">
        <label for="bio">Bio</label>
        <textarea id="bio"
                  name="Bio"
                  class="form-control"
                  rows="4"
                  maxlength="500">@Model.Profile.Bio</textarea>
        <small class="form-text text-muted">
            <span id="bio-count">@Model.Profile.Bio?.Length ?? 0</span>/500 characters
        </small>
    </div>
    
    <div class="form-group mb-3">
        <label for="website">Website</label>
        <input type="url" 
               id="website"
               name="Website" 
               value="@Model.Profile.Website"
               class="form-control"
               placeholder="https://example.com" />
    </div>
    
    <div id="profile-messages"></div>
    
    <!-- hx-vals injects metadata, hx-params excludes debug field -->
    <button type="submit"
            hx-vals='js:{"updatedAt": new Date().toISOString()}'
            hx-params="not _debugInfo"
            class="btn btn-primary">
        <span class="btn-text">Save Profile</span>
        <span class="htmx-indicator">Saving...</span>
    </button>
    
    <input type="hidden" name="_debugInfo" value="client-debug-data" />
</form>

<script>
// Character counter for bio
document.getElementById('bio').addEventListener('input', function() {
    document.getElementById('bio-count').textContent = this.value.length;
});
</script>
```

**Pages/Profile.cshtml.cs:**

```csharp
public class ProfileModel : PageModel
{
    private readonly IProfileService _profileService;

    public ProfileModel(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public UserProfile Profile { get; set; } = null!;

    public void OnGet()
    {
        Profile = _profileService.GetCurrentUserProfile();
    }

    public IActionResult OnGetCheckDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Content("", "text/html");
        }

        var currentProfile = _profileService.GetCurrentUserProfile();
        
        // Allow keeping current name
        if (displayName.Equals(currentProfile.DisplayName, StringComparison.OrdinalIgnoreCase))
        {
            return Content("<span class=\"text-success\">Current name</span>", "text/html");
        }

        if (_profileService.DisplayNameExists(displayName))
        {
            return Content("<span class=\"text-danger\">This display name is taken</span>", "text/html");
        }

        return Content("<span class=\"text-success\">Available</span>", "text/html");
    }

    public IActionResult OnPostSave(ProfileInput input)
    {
        if (!ModelState.IsValid)
        {
            return Content("<div class=\"alert alert-danger\">Please correct the errors above</div>", "text/html");
        }

        var currentProfile = _profileService.GetCurrentUserProfile();

        // Check display name uniqueness (skip if unchanged)
        if (!input.DisplayName.Equals(currentProfile.DisplayName, StringComparison.OrdinalIgnoreCase)
            && _profileService.DisplayNameExists(input.DisplayName))
        {
            return Content("<div class=\"alert alert-danger\">Display name is already taken</div>", "text/html");
        }

        currentProfile.DisplayName = input.DisplayName;
        currentProfile.Bio = input.Bio;
        currentProfile.Website = input.Website;
        currentProfile.UpdatedAt = input.UpdatedAt;

        _profileService.Save(currentProfile);

        return Content("<div class=\"alert alert-success\">Profile saved!</div>", "text/html");
    }
}

public class ProfileInput
{
    public string DisplayName { get; set; } = "";
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

## Summary

This chapter covered form enhancement attributes:

- **`hx-params`** controls which fields are sent: `*` (all), `none`, `not fieldName`, or specific field names
- **`hx-vals`** injects additional data using JSON syntax or `js:` prefix for dynamic values
- **`hx-validate`** triggers HTML5 validation before htmx sends the request
- **Real-time validation** uses `hx-get` with `delay:` trigger and `hx-sync="this:replace"`
- **Server validation** should always run on submit, even if client-side checks passed

These attributes give you fine control over form data while keeping validation logic on the server where it can access databases, enforce business rules, and stay consistent across all clients.

## Preview of Next Chapter

Chapter 18 covers drag-and-drop functionality with htmx and Hyperscript. You will learn to build sortable lists, kanban boards, and file upload zones that let users rearrange content with natural mouse and touch interactions.
