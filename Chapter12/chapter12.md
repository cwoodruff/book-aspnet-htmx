---
order: 19
icon: stack
label: Chap 12 - Building Blocks with HTML Components- Tabs and Accordions
meta:
title: "Building Blocks with HTML Components: Tabs and Accordions"
---

# Building Blocks with HTML Components: Tabs and Accordions

Tabs and accordions appear everywhere: dashboards, settings pages, FAQs, product details, documentation. These components organize content into digestible sections that users reveal on demand. Traditional implementations require JavaScript to toggle visibility, manage state, and handle keyboard navigation. With htmx and Razor Pages, you can build these components with server-rendered content, lazy loading, and minimal client-side code.

This chapter covers both patterns with complete implementations. You will build tabbed interfaces with active state management, URL integration for bookmarking, and proper accessibility. You will create accordions that expand and collapse smoothly, load content on demand, and support both single-open and multi-open behaviors. Every example includes the server-side code and CSS required to make it work.

## Tabbed Interfaces

Tabs let users switch between content panels without page navigation. Each tab represents a view, and clicking it reveals the associated content while hiding others.

### Basic Tab Structure

Start with the HTML structure:

**Pages/Dashboard.cshtml:**

```html
@page
@model DashboardModel

<h1>Dashboard</h1>

<div class="tabs-container">
    <div class="tab-list" role="tablist">
        @foreach (var tab in Model.Tabs)
        {
            <button role="tab"
                    id="tab-@tab.Id"
                    aria-selected="@(Model.ActiveTab == tab.Id ? "true" : "false")"
                    aria-controls="tab-panel"
                    class="tab-button @(Model.ActiveTab == tab.Id ? "active" : "")"
                    hx-get="/Dashboard?handler=@tab.Handler"
                    hx-target="#tab-panel"
                    hx-swap="innerHTML"
                    hx-indicator="#tab-spinner"
                    _="on click remove .active from .tab-button then add .active to me">
                @tab.Label
            </button>
        }
        <span id="tab-spinner" class="htmx-indicator">Loading...</span>
    </div>

    <div id="tab-panel" 
         role="tabpanel" 
         aria-labelledby="tab-@Model.ActiveTab"
         class="tab-panel">
        @switch (Model.ActiveTab)
        {
            case "overview":
                <partial name="_OverviewTab" model="Model.OverviewData" />
                break;
            case "analytics":
                <partial name="_AnalyticsTab" model="Model.AnalyticsData" />
                break;
            case "settings":
                <partial name="_SettingsTab" model="Model.SettingsData" />
                break;
            default:
                <partial name="_OverviewTab" model="Model.OverviewData" />
                break;
        }
    </div>
</div>
```

**Pages/Dashboard.cshtml.cs:**

```csharp
public class DashboardModel : PageModel
{
    private readonly IDashboardService _dashboardService;

    public DashboardModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public string ActiveTab { get; set; } = "overview";
    public OverviewData? OverviewData { get; set; }
    public AnalyticsData? AnalyticsData { get; set; }
    public SettingsData? SettingsData { get; set; }

    public List<TabDefinition> Tabs { get; } = new()
    {
        new TabDefinition { Id = "overview", Label = "Overview", Handler = "Overview" },
        new TabDefinition { Id = "analytics", Label = "Analytics", Handler = "Analytics" },
        new TabDefinition { Id = "settings", Label = "Settings", Handler = "Settings" }
    };

    public IActionResult OnGet(string tab = "overview")
    {
        ActiveTab = tab;
        LoadTabData(tab);

        // htmx request returns partial only
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return GetPartialForTab(tab);
        }

        return Page();
    }

    public IActionResult OnGetOverview()
    {
        OverviewData = _dashboardService.GetOverview();
        return Partial("_OverviewTab", OverviewData);
    }

    public IActionResult OnGetAnalytics()
    {
        AnalyticsData = _dashboardService.GetAnalytics();
        return Partial("_AnalyticsTab", AnalyticsData);
    }

    public IActionResult OnGetSettings()
    {
        SettingsData = _dashboardService.GetSettings();
        return Partial("_SettingsTab", SettingsData);
    }

    private void LoadTabData(string tab)
    {
        switch (tab)
        {
            case "analytics":
                AnalyticsData = _dashboardService.GetAnalytics();
                break;
            case "settings":
                SettingsData = _dashboardService.GetSettings();
                break;
            default:
                OverviewData = _dashboardService.GetOverview();
                break;
        }
    }

    private IActionResult GetPartialForTab(string tab)
    {
        return tab switch
        {
            "analytics" => Partial("_AnalyticsTab", _dashboardService.GetAnalytics()),
            "settings" => Partial("_SettingsTab", _dashboardService.GetSettings()),
            _ => Partial("_OverviewTab", _dashboardService.GetOverview())
        };
    }
}

public class TabDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Handler { get; set; } = string.Empty;
}
```

**Tab partial example - Pages/Shared/_OverviewTab.cshtml:**

```html
@model OverviewData

<div class="overview-content">
    <h2>Welcome back!</h2>
    
    <div class="stats-grid">
        <div class="stat-card">
            <h3>Total Users</h3>
            <p class="stat-value">@Model.TotalUsers.ToString("N0")</p>
        </div>
        <div class="stat-card">
            <h3>Active Sessions</h3>
            <p class="stat-value">@Model.ActiveSessions.ToString("N0")</p>
        </div>
        <div class="stat-card">
            <h3>Revenue</h3>
            <p class="stat-value">@Model.Revenue.ToString("C")</p>
        </div>
    </div>
    
    <h3>Recent Activity</h3>
    <ul>
        @foreach (var activity in Model.RecentActivity)
        {
            <li>@activity.Description - @activity.Timestamp.ToString("g")</li>
        }
    </ul>
</div>
```

### Tab CSS

```css
.tabs-container {
    margin: 1rem 0;
}

.tab-list {
    display: flex;
    gap: 0.25rem;
    border-bottom: 2px solid #dee2e6;
    margin-bottom: 0;
}

.tab-button {
    padding: 0.75rem 1.5rem;
    border: none;
    background: transparent;
    cursor: pointer;
    border-bottom: 2px solid transparent;
    margin-bottom: -2px;
    font-size: 1rem;
    color: #6c757d;
    transition: color 0.15s, border-color 0.15s;
}

.tab-button:hover {
    color: #495057;
}

.tab-button.active {
    color: #0d6efd;
    border-bottom-color: #0d6efd;
    font-weight: 500;
}

.tab-button:focus {
    outline: 2px solid #0d6efd;
    outline-offset: -2px;
}

.tab-panel {
    padding: 1.5rem 0;
}

.htmx-indicator {
    display: none;
    margin-left: 0.5rem;
}

.htmx-request .htmx-indicator {
    display: inline-block;
}

.htmx-request .tab-button {
    pointer-events: none;
    opacity: 0.7;
}
```

### Tabs with URL Integration

Add `hx-push-url` to make tabs bookmarkable and support browser back/forward:

```html
<button role="tab"
        class="tab-button @(Model.ActiveTab == "overview" ? "active" : "")"
        hx-get="/Dashboard?handler=Overview"
        hx-target="#tab-panel"
        hx-push-url="/Dashboard?tab=overview"
        hx-indicator="#tab-spinner"
        _="on click remove .active from .tab-button then add .active to me">
    Overview
</button>
```

Add `hx-history-elt` to the tab panel for proper history restoration:

```html
<div id="tab-panel" role="tabpanel" hx-history-elt>
    <!-- content -->
</div>
```

Now users can:
- Bookmark specific tabs
- Share links to specific tabs
- Use back/forward buttons to navigate between tabs
- Refresh the page and stay on the current tab

### Product Detail Tabs

A common pattern for e-commerce or content sites:

**Pages/Product.cshtml:**

```html
@page "{id:int}"
@model ProductModel

<div class="product-header">
    <h1>@Model.Product.Name</h1>
    <p class="price">@Model.Product.Price.ToString("C")</p>
</div>

<div class="product-tabs">
    <div class="tab-list" role="tablist">
        <button role="tab"
                class="tab-button active"
                hx-get="/Product/@Model.Product.Id?handler=Description"
                hx-target="#product-tab-content"
                hx-push-url="/Product/@Model.Product.Id?tab=description"
                _="on click remove .active from .tab-button then add .active to me">
            Description
        </button>
        <button role="tab"
                class="tab-button"
                hx-get="/Product/@Model.Product.Id?handler=Specifications"
                hx-target="#product-tab-content"
                hx-push-url="/Product/@Model.Product.Id?tab=specifications"
                _="on click remove .active from .tab-button then add .active to me">
            Specifications
        </button>
        <button role="tab"
                class="tab-button"
                hx-get="/Product/@Model.Product.Id?handler=Reviews"
                hx-target="#product-tab-content"
                hx-push-url="/Product/@Model.Product.Id?tab=reviews"
                _="on click remove .active from .tab-button then add .active to me">
            Reviews (@Model.Product.ReviewCount)
        </button>
    </div>

    <div id="product-tab-content" role="tabpanel" hx-history-elt>
        <partial name="_ProductDescription" model="Model.Product" />
    </div>
</div>
```

**Pages/Product.cshtml.cs:**

```csharp
public class ProductModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IReviewService _reviewService;

    public ProductModel(IProductService productService, IReviewService reviewService)
    {
        _productService = productService;
        _reviewService = reviewService;
    }

    public Product Product { get; set; } = null!;
    public string ActiveTab { get; set; } = "description";

    public IActionResult OnGet(int id, string tab = "description")
    {
        Product = _productService.GetById(id);
        if (Product == null) return NotFound();

        ActiveTab = tab;

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return tab switch
            {
                "specifications" => Partial("_ProductSpecifications", Product),
                "reviews" => Partial("_ProductReviews", _reviewService.GetForProduct(id)),
                _ => Partial("_ProductDescription", Product)
            };
        }

        return Page();
    }

    public IActionResult OnGetDescription(int id)
    {
        var product = _productService.GetById(id);
        if (product == null) return NotFound();
        return Partial("_ProductDescription", product);
    }

    public IActionResult OnGetSpecifications(int id)
    {
        var product = _productService.GetById(id);
        if (product == null) return NotFound();
        return Partial("_ProductSpecifications", product);
    }

    public IActionResult OnGetReviews(int id)
    {
        var reviews = _reviewService.GetForProduct(id);
        return Partial("_ProductReviews", reviews);
    }
}
```

**Pages/Shared/_ProductSpecifications.cshtml:**

```html
@model Product

<table class="specs-table">
    <tbody>
        @foreach (var spec in Model.Specifications)
        {
            <tr>
                <th>@spec.Name</th>
                <td>@spec.Value</td>
            </tr>
        }
    </tbody>
</table>
```

**Pages/Shared/_ProductReviews.cshtml:**

```html
@model List<Review>

<div class="reviews-list">
    @if (Model.Any())
    {
        @foreach (var review in Model)
        {
            <div class="review-card">
                <div class="review-header">
                    <span class="reviewer">@review.AuthorName</span>
                    <span class="rating">
                        @for (int i = 0; i < review.Rating; i++)
                        {
                            <span class="star filled">★</span>
                        }
                        @for (int i = review.Rating; i < 5; i++)
                        {
                            <span class="star">☆</span>
                        }
                    </span>
                    <span class="date">@review.CreatedAt.ToString("MMM d, yyyy")</span>
                </div>
                <p class="review-text">@review.Text</p>
            </div>
        }
    }
    else
    {
        <p>No reviews yet. Be the first to review this product!</p>
    }
</div>
```

## Accordion Components

Accordions stack content sections vertically, allowing users to expand and collapse individual sections. They work well for FAQs, settings panels, and any content where users need access to specific sections without seeing everything at once.

### Basic Accordion

**Pages/FAQ.cshtml:**

```html
@page
@model FAQModel

<h1>Frequently Asked Questions</h1>

<div class="accordion">
    @foreach (var item in Model.Questions)
    {
        <div class="accordion-item" id="faq-@item.Id">
            <button class="accordion-header"
                    aria-expanded="false"
                    aria-controls="faq-content-@item.Id"
                    hx-get="/FAQ?handler=Answer&amp;id=@item.Id"
                    hx-target="#faq-content-@item.Id"
                    hx-swap="innerHTML"
                    _="on click 
                       if I match @aria-expanded='true'
                           set @aria-expanded to 'false'
                           remove .open from next .accordion-content
                       else
                           set @aria-expanded to 'true'
                           add .open to next .accordion-content
                       end">
                <span class="accordion-title">@item.Question</span>
                <span class="accordion-icon">▼</span>
            </button>
            <div id="faq-content-@item.Id" 
                 class="accordion-content"
                 role="region"
                 aria-labelledby="faq-@item.Id">
                <!-- Answer loads here -->
            </div>
        </div>
    }
</div>
```

**Pages/FAQ.cshtml.cs:**

```csharp
public class FAQModel : PageModel
{
    private readonly IFAQService _faqService;

    public FAQModel(IFAQService faqService)
    {
        _faqService = faqService;
    }

    public List<FAQQuestion> Questions { get; set; } = new();

    public void OnGet()
    {
        Questions = _faqService.GetAllQuestions();
    }

    public IActionResult OnGetAnswer(int id)
    {
        var answer = _faqService.GetAnswer(id);
        if (answer == null) return NotFound();
        return Partial("_FAQAnswer", answer);
    }
}
```

**Pages/Shared/_FAQAnswer.cshtml:**

```html
@model FAQAnswer

<div class="answer-content">
    @Html.Raw(Model.HtmlContent)
    
    @if (Model.RelatedLinks.Any())
    {
        <div class="related-links">
            <h4>Related Articles</h4>
            <ul>
                @foreach (var link in Model.RelatedLinks)
                {
                    <li><a href="@link.Url">@link.Title</a></li>
                }
            </ul>
        </div>
    }
</div>
```

### Accordion CSS

```css
.accordion {
    border: 1px solid #dee2e6;
    border-radius: 8px;
    overflow: hidden;
}

.accordion-item {
    border-bottom: 1px solid #dee2e6;
}

.accordion-item:last-child {
    border-bottom: none;
}

.accordion-header {
    width: 100%;
    padding: 1rem 1.25rem;
    background: #f8f9fa;
    border: none;
    cursor: pointer;
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 1rem;
    font-weight: 500;
    text-align: left;
    transition: background-color 0.15s;
}

.accordion-header:hover {
    background: #e9ecef;
}

.accordion-header:focus {
    outline: 2px solid #0d6efd;
    outline-offset: -2px;
}

.accordion-icon {
    transition: transform 0.3s ease;
    font-size: 0.75rem;
}

.accordion-header[aria-expanded="true"] .accordion-icon {
    transform: rotate(180deg);
}

.accordion-content {
    max-height: 0;
    overflow: hidden;
    transition: max-height 0.3s ease, padding 0.3s ease;
    background: white;
}

.accordion-content.open {
    max-height: 1000px;
    padding: 1rem 1.25rem;
}

.answer-content {
    line-height: 1.6;
}

.answer-content p {
    margin-bottom: 1rem;
}

.related-links {
    margin-top: 1.5rem;
    padding-top: 1rem;
    border-top: 1px solid #dee2e6;
}

.related-links h4 {
    font-size: 0.875rem;
    color: #6c757d;
    margin-bottom: 0.5rem;
}
```

### Single-Open Accordion

For accordions where only one section should be open at a time:

```html
<div class="accordion" id="settings-accordion">
    @foreach (var section in Model.Sections)
    {
        <div class="accordion-item">
            <button class="accordion-header"
                    aria-expanded="false"
                    hx-get="/Settings?handler=Section&amp;id=@section.Id"
                    hx-target="#section-content-@section.Id"
                    hx-swap="innerHTML"
                    _="on click
                       -- Close all other sections first
                       remove .open from .accordion-content in #settings-accordion
                       set @aria-expanded to 'false' on .accordion-header in #settings-accordion
                       -- Then open this one
                       set @aria-expanded to 'true' on me
                       add .open to next .accordion-content">
                @section.Title
                <span class="accordion-icon">▼</span>
            </button>
            <div id="section-content-@section.Id" class="accordion-content">
                <!-- Content loads here -->
            </div>
        </div>
    }
</div>
```

### Accordion with Preloaded Content

If content is small and you want instant expansion without server requests:

```html
@foreach (var item in Model.FAQItems)
{
    <div class="accordion-item">
        <button class="accordion-header"
                aria-expanded="false"
                _="on click
                   if I match @aria-expanded='true'
                       set @aria-expanded to 'false'
                       remove .open from next .accordion-content
                   else
                       set @aria-expanded to 'true'
                       add .open to next .accordion-content
                   end">
            @item.Question
            <span class="accordion-icon">▼</span>
        </button>
        <div class="accordion-content">
            <p>@item.Answer</p>
        </div>
    </div>
}
```

No htmx needed here since content is already in the page. Hyperscript handles the expand/collapse.

### Accordion with Cached Content

Load content once, then use cached version on subsequent opens:

```html
<button class="accordion-header"
        aria-expanded="false"
        hx-get="/FAQ?handler=Answer&amp;id=@item.Id"
        hx-target="#faq-content-@item.Id"
        hx-swap="innerHTML"
        hx-trigger="click once"
        _="on click
           if I match @aria-expanded='true'
               set @aria-expanded to 'false'
               remove .open from next .accordion-content
           else
               set @aria-expanded to 'true'
               add .open to next .accordion-content
           end">
    @item.Question
</button>
```

The `hx-trigger="click once"` ensures htmx only fetches content on the first click. Subsequent clicks just toggle visibility using Hyperscript.

## Accessibility

Tabs and accordions have specific accessibility requirements.

### Tab Accessibility

```html
<div class="tab-list" role="tablist" aria-label="Dashboard sections">
    <button role="tab"
            id="tab-overview"
            aria-selected="true"
            aria-controls="panel-overview"
            tabindex="0"
            class="tab-button active">
        Overview
    </button>
    <button role="tab"
            id="tab-analytics"
            aria-selected="false"
            aria-controls="panel-analytics"
            tabindex="-1"
            class="tab-button">
        Analytics
    </button>
</div>

<div role="tabpanel"
     id="panel-overview"
     aria-labelledby="tab-overview"
     tabindex="0">
    <!-- content -->
</div>
```

Key requirements:
- Container has `role="tablist"`
- Tabs have `role="tab"` and `aria-selected`
- Active tab has `tabindex="0"`, others have `tabindex="-1"`
- Panels have `role="tabpanel"` and `aria-labelledby`

### Keyboard Navigation for Tabs

Add arrow key navigation:

```html
<div class="tab-list" 
     role="tablist"
     _="on keydown[key is 'ArrowRight'] from .tab-button
            get the next .tab-button from event.target
            if it exists trigger click on it then it.focus()
        on keydown[key is 'ArrowLeft'] from .tab-button
            get the previous .tab-button from event.target
            if it exists trigger click on it then it.focus()
        on keydown[key is 'Home'] from .tab-button
            get the first .tab-button
            trigger click on it then it.focus()
        on keydown[key is 'End'] from .tab-button
            get the last .tab-button
            trigger click on it then it.focus()">
    <!-- tabs -->
</div>
```

### Accordion Accessibility

```html
<div class="accordion-item">
    <h3>
        <button class="accordion-header"
                id="accordion-header-1"
                aria-expanded="false"
                aria-controls="accordion-panel-1">
            Section Title
        </button>
    </h3>
    <div id="accordion-panel-1"
         role="region"
         aria-labelledby="accordion-header-1"
         class="accordion-content"
         hidden>
        <!-- content -->
    </div>
</div>
```

Use the `hidden` attribute alongside CSS for proper accessibility:

```css
.accordion-content[hidden] {
    display: block; /* Override hidden for CSS animation */
    max-height: 0;
    overflow: hidden;
}

.accordion-content:not([hidden]) {
    max-height: 1000px;
}
```

```html
<button _="on click
           if next .accordion-content matches [hidden]
               remove @hidden from next .accordion-content
               set @aria-expanded to 'true'
           else
               add @hidden to next .accordion-content
               set @aria-expanded to 'false'
           end">
```

## Error Handling

Handle failed requests gracefully:

```html
<div id="tab-panel"
     hx-on::response-error="showTabError(event)"
     hx-on::send-error="showTabError(event)">
</div>

<script>
function showTabError(event) {
    var panel = document.getElementById('tab-panel');
    panel.innerHTML = `
        <div class="alert alert-danger">
            <h4>Failed to Load Content</h4>
            <p>There was a problem loading this section. Please try again.</p>
            <button onclick="location.reload()" class="btn btn-outline-danger">
                Reload Page
            </button>
        </div>
    `;
}
</script>
```

## Loading States

Show loading feedback during content fetch:

```html
<div class="tab-list">
    <button hx-get="/Dashboard?handler=Analytics"
            hx-target="#tab-panel"
            hx-indicator="#tab-panel"
            class="tab-button">
        Analytics
    </button>
</div>

<div id="tab-panel" class="tab-panel">
    <!-- content -->
</div>
```

```css
#tab-panel.htmx-request {
    opacity: 0.5;
    pointer-events: none;
    position: relative;
}

#tab-panel.htmx-request::after {
    content: "Loading...";
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    background: white;
    padding: 1rem 2rem;
    border-radius: 4px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}
```

## Reusable Tab Component

Create a partial for consistent tab styling across your application:

**Pages/Shared/_TabContainer.cshtml:**

```html
@model TabContainerModel

<div class="tabs-container">
    <div class="tab-list" role="tablist" aria-label="@Model.AriaLabel">
        @foreach (var tab in Model.Tabs)
        {
            <button role="tab"
                    id="tab-@tab.Id"
                    aria-selected="@(Model.ActiveTabId == tab.Id ? "true" : "false")"
                    aria-controls="@Model.PanelId"
                    class="tab-button @(Model.ActiveTabId == tab.Id ? "active" : "")"
                    hx-get="@tab.Url"
                    hx-target="#@Model.PanelId"
                    hx-swap="innerHTML"
                    hx-push-url="@(Model.EnableHistory ? tab.PushUrl : null)"
                    hx-indicator="#@(Model.PanelId)-spinner"
                    _="on click remove .active from .tab-button in closest .tabs-container then add .active to me">
                @tab.Label
            </button>
        }
        <span id="@(Model.PanelId)-spinner" class="htmx-indicator">Loading...</span>
    </div>

    <div id="@Model.PanelId" 
         role="tabpanel"
         class="tab-panel"
         @(Model.EnableHistory ? "hx-history-elt" : "")>
        @Model.InitialContent
    </div>
</div>
```

Use it:

```html
<partial name="_TabContainer" model="new TabContainerModel
{
    AriaLabel = "Product information",
    PanelId = "product-tabs",
    ActiveTabId = Model.ActiveTab,
    EnableHistory = true,
    Tabs = new List<TabModel>
    {
        new() { Id = "desc", Label = "Description", Url = "/Product/" + Model.Id + "?handler=Description", PushUrl = "/Product/" + Model.Id + "?tab=desc" },
        new() { Id = "specs", Label = "Specifications", Url = "/Product/" + Model.Id + "?handler=Specs", PushUrl = "/Product/" + Model.Id + "?tab=specs" },
        new() { Id = "reviews", Label = "Reviews", Url = "/Product/" + Model.Id + "?handler=Reviews", PushUrl = "/Product/" + Model.Id + "?tab=reviews" }
    },
    InitialContent = await Html.PartialAsync("_ProductDescription", Model.Product)
}" />
```

## Summary

This chapter covered tabs and accordions with htmx and Razor Pages:

- **Tabs** switch between content panels with lazy loading via `hx-get`
- **Active state** managed with Hyperscript class toggling
- **URL integration** uses `hx-push-url` and `hx-history-elt` for bookmarkable tabs
- **Accordions** expand/collapse sections with `aria-expanded` and CSS transitions
- **Single-open accordions** close other sections when opening a new one
- **Cached content** uses `hx-trigger="click once"` to load content only once
- **Accessibility** requires proper ARIA roles, states, and keyboard navigation
- **Error handling** shows user-friendly messages when requests fail
- **Loading states** provide feedback during content fetch

These patterns give you interactive, accessible navigation components without JavaScript frameworks.

## Preview of Next Chapter

Chapter 13 covers dynamic lists and tables. You will learn to build sortable, filterable tables with server-side data, pagination that works with htmx, inline row editing, and real-time updates for live data displays.
