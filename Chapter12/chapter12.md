---
order: 19
icon: stack
label: Chap 12 - Building Blocks with HTML Components- Tabs and Accordions
meta:
title: "Building Blocks with HTML Components: Tabs and Accordions"
visibility: hidden
---

# Building Blocks with HTML Components- Tabs and Accordions

If you've followed along so far, you've seen how htmx can inject interactivity into your ASP.NET Core Razor Pages apps with minimal effort—and even less JavaScript. We've tackled forms, modals, and lists, showing how htmx makes everyday UI patterns feel lighter and more dynamic. Now it's time to bring that same approachability to a couple of common, yet surprisingly fiddly, UI elements: tabs and accordions.

Tabs and accordions are everywhere—from dashboards and documentation pages to mobile menus and feature comparisons. But implementing them usually means reaching for JavaScript-heavy solutions, custom components, or client-side frameworks. Not here. In this chapter, we’ll show you how to build flexible, fully server-driven tabs and accordions using nothing more than HTML, Razor Pages, and htmx. No complex frontend logic, just smart, dynamic updates from your server.

You’ll learn how to dynamically swap content, manage user-triggered state, and even enhance the experience further with a little Hyperscript magic—all while keeping your markup clean and your code easy to reason about. By the end, you’ll have practical, reusable components that fit right into your Razor Pages project, delivering a polished user experience without the usual overhead. Let's get into it.

## Building Tabbed Content with Razor Pages and htmx

Tabbed interfaces are a staple of web applications. They let users quickly switch between different content panels without navigating away from the current page. Traditionally, tabs are powered by JavaScript: click handlers toggle classes, show and hide elements, and sometimes fetch data using fetch or jQuery’s AJAX. While this works, it often leads to bloated client-side logic and tight coupling between presentation and behavior. With htmx and Razor Pages, you can skip the JavaScript and let the server handle the logic—cleanly and declaratively.

Let’s say you’re building a product details page in an e-commerce application. You want to show product information, specifications, and reviews as separate tabs. Instead of preloading everything or writing a JavaScript tab manager, you can load each section on demand using hx-get, and target a shared container using hx-target.

Start with your base Razor Page. Here's the layout of the Product.cshtml page:

```html
@page
@model ProductModel

<h2>@Model.ProductName</h2>

<div class="tabs">
    <button hx-get="/Product?handler=Info&productId=@Model.ProductId" hx-target="#tab-content" hx-swap="innerHTML">Info</button>
    <button hx-get="/Product?handler=Specs&productId=@Model.ProductId" hx-target="#tab-content" hx-swap="innerHTML">Specifications</button>
    <button hx-get="/Product?handler=Reviews&productId=@Model.ProductId" hx-target="#tab-content" hx-swap="innerHTML">Reviews</button>
</div>

<div id="tab-content" class="tab-content">
    <p>Select a tab to view details.</p>
</div>
```

Each tab is a button that triggers an hx-get request. The target is the #tab-content div, which means the content returned by the server will be swapped into that element. We're using hx-swap="innerHTML" to ensure we’re only replacing the content inside the container, not the element itself—so any layout or CSS styles applied to #tab-content remain intact.

Now let’s look at the Product.cshtml.cs page model. We’ll handle each tab request with a separate handler:

```csharp
public class ProductModel : PageModel
{
    public string ProductName { get; set; } = "Contoso Headphones";
    public int ProductId { get; set; } = 42;

    public IActionResult OnGetInfo(int productId)
    {
        var html = "<p>This is a great pair of wireless headphones with noise cancellation and 20-hour battery life.</p>";
        return Content(html, "text/html");
    }

    public IActionResult OnGetSpecs(int productId)
    {
        var html = "<ul><li>Bluetooth 5.2</li><li>Noise Cancellation</li><li>USB-C Charging</li></ul>";
        return Content(html, "text/html");
    }

    public IActionResult OnGetReviews(int productId)
    {
        var html = "<p>⭐️⭐️⭐️⭐️☆ - Sounds amazing and battery lasts all day.</p>";
        return Content(html, "text/html");
    }
}
```

Each tab triggers a unique handler based on the ?handler= query string. Razor Pages routes the request appropriately, and each handler returns a snippet of HTML. Because htmx expects just a fragment, there’s no need to wrap this in a full layout or view—just return the markup you want injected into the tab container.

This approach is refreshingly simple. You don’t preload any unnecessary content, you don’t maintain tab state on the client, and you don’t worry about client-side lifecycle events. The tabs remain clickable HTML buttons, and your Razor Pages backend handles the logic of what to render and when.

You can make it even more elegant by moving the HTML fragments into partial views, but the core idea remains the same: using hx-get to fetch content, hx-target to decide where it goes, and hx-swap to control how it’s inserted. Combined, they give you a robust tab interface with almost no JavaScript and all the power of Razor Pages behind the scenes.

## Crafting Lazy-Loaded FAQ Accordions with Razor Pages and htmx

While tabs present side-by-side content views that users toggle between, accordions follow a more vertical, stacked pattern. They’re often used when you want to let users drill into specific content without overwhelming the page visually—think FAQs, documentation, or expandable panels in a dashboard. The challenge with traditional accordion implementations is that they either load all content up front (leading to bloated pages) or require JavaScript to manage show/hide logic. With htmx, we can ditch the JavaScript and avoid the preload penalty by lazily loading content as users expand each section.

Let’s build an FAQ section where clicking a question expands the answer—but the answer isn’t fetched until the user wants to see it. This is where hx-get and hx-target really shine. You send a request when the accordion header is clicked and inject the response into a specific area under that question.

Start with a Razor Page called Faq.cshtml. Here’s what the markup might look like:

```html
@page
@model FaqModel

<h2>Frequently Asked Questions</h2>

<div class="accordion">
    <div class="question">
        <h3>
            <button hx-get="/Faq?handler=Answer&id=1" hx-target="#answer-1" hx-swap="innerHTML">
                What is your return policy?
            </button>
        </h3>
        <div id="answer-1" class="answer"></div>
    </div>

    <div class="question">
        <h3>
            <button hx-get="/Faq?handler=Answer&id=2" hx-target="#answer-2" hx-swap="innerHTML">
                Do you ship internationally?
            </button>
        </h3>
        <div id="answer-2" class="answer"></div>
    </div>

    <div class="question">
        <h3>
            <button hx-get="/Faq?handler=Answer&id=3" hx-target="#answer-3" hx-swap="innerHTML">
                How do I request a refund?
            </button>
        </h3>
        <div id="answer-3" class="answer"></div>
    </div>
</div>
```

Each question has a button that makes a lazy request to load the answer. When clicked, htmx sends an HTTP GET request to the appropriate handler and injects the returned answer into the div just beneath the question. The rest of the page remains untouched—no full reloads, no DOM juggling.

The Faq.cshtml.cs page model handles the responses. Here's how you could return answers based on an ID:

```csharp
public class FaqModel : PageModel
{
    private readonly Dictionary<int, string> _answers = new()
    {
        { 1, "You can return any item within 30 days of delivery for a full refund." },
        { 2, "Yes, we ship to over 50 countries worldwide. Shipping costs vary by region." },
        { 3, "To request a refund, visit your order history and click 'Request Refund' next to the item." }
    };

    public IActionResult OnGetAnswer(int id)
    {
        if (_answers.TryGetValue(id, out var answer))
        {
            return Content($"<p>{answer}</p>", "text/html");
        }

        return NotFound();
    }
}
```
This model keeps things simple. Each answer is returned as a raw HTML snippet, injected directly into the #answer-{id} element targeted by the button. You avoid bloating the initial page load, and each section only loads when needed—making it perfect for mobile or bandwidth-sensitive users.

This lazy loading pattern also scales well. You could pull FAQ content from a database or CMS and still only query what's necessary. And since the interaction is server-driven, you don’t have to manage state in the browser. It’s all declarative and minimal.

By leaning on htmx’s ability to surgically update the DOM, you create a snappy, interactive accordion interface that feels modern without leaning on a frontend framework. No JavaScript toggling, no preloading unnecessary data—just a clean, efficient FAQ that works out of the box.

## Polishing Tab Interactions with Hyperscript and Razor Pages

htmx makes it easy to fetch and swap content for tab interfaces, but it doesn’t manage visual state—like highlighting which tab is active or animating content transitions. That’s where Hyperscript comes in. Hyperscript is a small scripting language that plays beautifully with htmx and HTML, allowing you to add behavior directly to elements without reaching for full JavaScript. In a tabbed interface, it helps tie the whole experience together with just a few readable attributes.

Let’s take a simple tabbed dashboard and level it up. We’ll show active tab highlighting, apply a smooth fade-in animation when switching content, and even enable keyboard navigation for accessibility. Start with a basic layout for Dashboard.cshtml:

```html
@page
@model DashboardModel

<h2>My Dashboard</h2>

<div class="tabs">
    <button _="on click
                remove .active from .tabs button
                add .active to me
                get my @hx-get then
                put it into #panel with a fade transition"
            hx-get="/Dashboard?handler=Overview">Overview</button>

    <button _="on click
                remove .active from .tabs button
                add .active to me
                get my @hx-get then
                put it into #panel with a fade transition"
            hx-get="/Dashboard?handler=Reports">Reports</button>

    <button _="on click
                remove .active from .tabs button
                add .active to me
                get my @hx-get then
                put it into #panel with a fade transition"
            hx-get="/Dashboard?handler=Settings">Settings</button>
</div>

<div id="panel" class="tab-panel fade">
    <p>Select a tab to view content.</p>
</div>
```

Notice the _= attribute. That’s Hyperscript. When a button is clicked, Hyperscript removes the .active class from all buttons, adds it to the clicked one, performs the fetch using the @hx-get attribute, and injects the result into the #panel element—with a fade effect for smoothness. You get both state management and animation in a single, declarative snippet.

To make the transitions feel polished, we define the .fade class in your CSS:

```css
.tab-panel.fade {
opacity: 0;
transition: opacity 0.3s ease-in-out;
}

.tab-panel.fade:load {
opacity: 1;
}
```

This subtle transition improves the user experience by gently fading in the new content. No jarring swaps, no sudden layout shifts—just a smoother feel that modern users expect.

But let’s not forget accessibility. Tabs should be keyboard-navigable. Hyperscript can help here too. You can use the on keydown event to support left/right navigation between tab buttons:

```html
<button tabindex="0"
        _="on keydown[key is 'ArrowRight'] then
            call nextElementSibling.click()
            on keydown[key is 'ArrowLeft'] then
            call previousElementSibling.click()"
        hx-get="/Dashboard?handler=Overview">
    Overview
</button>
```

Each tab button listens for arrow key presses and programmatically triggers the next or previous button’s click event. With just a few more lines, all your tabs are now keyboard-friendly without needing a full ARIA role setup.

Finally, here’s how the backend handles the content. In Dashboard.cshtml.cs:

```csharp
public class DashboardModel : PageModel
{
public IActionResult OnGetOverview() =>
Content("<p>Welcome to your personalized overview.</p>", "text/html");

    public IActionResult OnGetReports() =>
        Content("<p>Here are your latest reports and analytics.</p>", "text/html");

    public IActionResult OnGetSettings() =>
        Content("<p>Update your preferences and configurations here.</p>", "text/html");
}
```

Combining htmx, Razor Pages, and Hyperscript creates a powerful pattern: the server controls what content is shown, htmx controls how it's fetched and inserted, and Hyperscript enhances the UX with visual polish and behavior—all without reaching for a heavyweight framework. The result is a dashboard that feels dynamic and professional, with minimal code and maximum clarity.

## Refining Tab and Accordion UX with State, Caching, and Reusability

As you start introducing more interactivity into your Razor Pages app with tabs and accordions, user experience becomes more than just the visuals—it’s about performance, state awareness, and reusability. A polished UI doesn’t just look nice; it behaves intuitively. That’s what we’ll focus on in this final section: how to optimize your tab and accordion components for a smooth, stateful, and maintainable user experience.

One of the most underrated improvements you can make to tabbed interfaces is handling browser history. With htmx, you can easily enable deep-linking into specific tabs by using hx-push-url. When a user clicks a tab, the URL updates with a query or fragment that reflects the active section. This allows bookmarking and sharing of specific views. For example:

```html
<button hx-get="/Product?handler=Specs&productId=42"
        hx-target="#tab-content"
        hx-swap="innerHTML"
        hx-push-url="true">
    Specifications
</button>
```

In your page model, you can detect the query string on initial load and preselect the right tab server-side. If someone shares a link to the Specs tab, it will load directly into that state, giving a seamless deep-link experience.

Another important UX consideration is performance. If users frequently switch between tabs or accordion panels, hitting the server each time can feel sluggish and unnecessary. You can cache previously loaded content on the client using a little Hyperscript or htmx’s hx-boost and hx-history-elt to prevent re-fetching. An easy pattern is to store the loaded HTML in a local element’s dataset, and reuse it if it’s already there, like this:

```html
<button _="on click
            if my.dataset.loaded is not 'true'
            get my @hx-get then put it into #tab-content
            set my.dataset.loaded to 'true'
        else
            put my.nextElementSibling.innerHTML into #tab-content">
    Overview
</button>
<div hidden>
    <div>Cached content goes here after first load</div>
</div>
```

By caching the response in a sibling element or data attribute, you can prevent repeat network calls while still delivering dynamic behavior. You can also make use of htmx’s hx-trigger="load once" or hx-request="once" attributes depending on your specific needs.

To ensure maintainability as your app grows, it’s worth breaking out tabs and accordions into partials. Keep the layout shell in your main page and render the dynamic content from partial views using Razor. Not only does this reduce code duplication, but it also lets you reuse patterns across the app—especially helpful for admin dashboards or product views that share common UI structures.

For example, a reusable tab section might look like this:

```html
@await Html.PartialAsync("_ProductTabs", new ProductTabModel { ProductId = Model.ProductId })
```

And each tab’s content is fetched from dedicated partials returned by their respective page handlers. This structure scales well and keeps concerns separated.

With these refinements—history awareness, lazy-loading with caching, and component reusability—you’re not just building interactive UI, you’re building thoughtful, production-ready experiences. Tabs and accordions should feel fast, remember user context, and integrate naturally into your Razor Pages application.

In the next chapter, we’ll take this same mindset into dynamic lists and tables. You’ll learn how to build interfaces that support real-time updates, client-driven filtering, and pagination—all with the same htmx fundamentals we’ve covered here. Lists and tables are the backbone of many business apps, and now that you’ve got a solid foundation with tabs and accordions, you’ll be ready to level up the complexity.