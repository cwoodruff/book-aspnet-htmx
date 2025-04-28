---
order: 18
icon: stack
label: Chap 13 - Building Blocks with HTML Components- Lists and Tables
meta:
title: "Building Blocks with HTML Components: Lists and Tables"
---

# Building Blocks with HTML Components: Lists and Tables

Now that you've seen how forms, modals, tabs, and accordions can come to life with just a dash of htmx magic, it’s time to explore two of the most foundational elements of any web application: lists and tables. Whether you’re presenting product catalogs, user directories, or transaction histories, these components are at the heart of turning raw data into something structured, visual, and usable.

In the world of server-rendered applications, updating tables or filtering lists used to mean one thing—full page reloads. That’s no longer the case. With htmx integrated into Razor Pages, we can provide highly responsive, seamless list and table interactions without introducing heavy frontend frameworks or duplicating business logic on the client. The result? Less code, better performance, and happier developers.

This chapter isn’t just about displaying data—it’s about making it dynamic. We'll look at how to load tables on demand, paginate large datasets, and even sort or filter content—all while staying within the comfort of your existing Razor Page models and partials. htmx handles the interactions, your server handles the logic, and your users get a smooth, modern experience.

You won’t need to re-learn everything from earlier chapters, but you will be building on concepts like hx-get, hx-trigger, and hx-target. If forms were your first taste of dynamic server interaction, lists and tables are where those techniques really start to shine. Let’s dive into how to make your data-driven components both powerful and elegant.

## Making Tables Talk: Dynamic Data with `hx-get` and `hx-trigger`

When you’re working with large datasets—think product catalogs, order histories, or game leaderboards—the performance and user experience of your table views become critical. If every change in state requires a full page reload, you’re not just wasting bandwidth; you’re also breaking the user’s rhythm. That’s where htmx steps in. With just a couple of attributes—`hx-get` and `hx-trigger`—you can dynamically fetch and refresh table data on demand or even in real time, all while keeping your backend logic clean and server-driven.

Server-driven updates are not only simpler to maintain but also more scalable. When the server owns the rendering logic, you're free from the burden of duplicating state and behavior in JavaScript. It also means you can return precisely the markup you want, already formatted, sorted, and styled, without needing the client to piece things together. This pays off especially when you’re dealing with large amounts of data: paging, filtering, and updating become server responsibilities, reducing load on the client and giving you a more secure, maintainable architecture.

Let’s see this in action with a leaderboard scenario. Imagine you’re building a competitive trivia game, and you want to show the top 10 players as they race for first place. You don’t want to refresh the entire page every few seconds, but you do want the table to stay current as new scores come in.

First, let’s create the Razor Page endpoint that will serve the leaderboard data. This can be a partial view that returns just the table rows:

```csharp
// Pages/Leaderboard.cshtml.cs
public class LeaderboardModel : PageModel
{
    public List<PlayerScore> TopPlayers { get; set; } = new();

    public PartialViewResult OnGetTopPlayers()
    {
        TopPlayers = LeaderboardService.GetTopPlayers(10); // Fetch top 10 players from your service or DB
        return Partial("_LeaderboardRows", TopPlayers);
    }
}
```

Now for the partial view that renders the rows:

```html
@model List<PlayerScore>

@foreach (var player in Model)
{
    <tr>
        <td>@player.Rank</td>
        <td>@player.Name</td>
        <td>@player.Score</td>
    </tr>
}
```

Next, let’s plug this into the main page. We’ll set up a table with a `tbody` element that will receive the updated rows. We'll use `hx-get` to define the endpoint and `hx-trigger` to refresh it automatically every few seconds:

```html
<table class="table">
    <thead>
        <tr>
            <th>Rank</th>
            <th>Name</th>
            <th>Score</th>
        </tr>
    </thead>
    <tbody
        hx-get="/Leaderboard?handler=TopPlayers"
        hx-trigger="every 5s"
        hx-swap="outerHTML"
        id="leaderboard-body">
        @await Html.PartialAsync("_LeaderboardRows", Model.TopPlayers)
    </tbody>
</table>
```

What’s happening here is pretty slick. Every five seconds, htmx sends a GET request to `/Leaderboard?handler=TopPlayers`, fetches the updated rows from the server, and swaps them into the `tbody`. The `hx-swap="outerHTML"` makes sure the entire tbody gets replaced, which is a clean way to reset the table without breaking the structure.

You can also set this up to update on demand—for example, when a user clicks a "Refresh" button. Just add a button with an `hx-get` pointing to the same handler, and set `hx-target="#leaderboard-body"` to direct the response to the correct place.

This approach gives you real-time-ish updates with almost no JavaScript and full control on the server. Whether you're building leaderboards, admin dashboards, or dynamic reports, `hx-get` and `hx-trigger` help you stay responsive without overcomplicating your architecture.

Up next, we’ll explore how to handle pagination and sorting, still powered by server-rendered content and minimal markup.

## Scroll On: Lazy-Loading Lists with Infinite htmx

Infinite scrolling is one of those UX patterns that feels magical when done right. Instead of breaking content across clunky pagination links, you just scroll—and more content appears like it’s always been there. This kind of interaction is especially useful for things like product listings, activity feeds, or image galleries, where users expect to browse fluidly and uninterrupted. With htmx, implementing this kind of experience is easier than you might think, thanks to the `hx-get` and `hx-trigger="revealed"` combo.

While traditional pagination requires users to stop, find the next link, click it, and wait for the page to reload (or at least update), infinite scrolling keeps users in the flow. It's a smoother experience and keeps people engaged longer. For e-commerce applications in particular, that can translate directly into better conversion rates. Of course, this needs to be balanced with performance—but htmx gives you a lightweight way to lazy-load new content without overloading the browser.

Let’s walk through building a product catalog with infinite scrolling. The server will return batches of products, and the client will request the next batch when a hidden trigger element comes into view. First, we’ll need a Razor Page that can return a set of products based on a page number:

```csharp
// Pages/Products.cshtml.cs
public class ProductsModel : PageModel
{
    public List<Product> Products { get; set; } = new();
    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;
    private const int PageSize = 20;

    public PartialViewResult OnGetMore()
    {
        Products = ProductService.GetPage(Page, PageSize);
        return Partial("_ProductCards", Products);
    }
}
```

Now we create the partial view that renders a grid of product cards:

```html
@model List<Product>

@foreach (var product in Model)
{
    <div class="product-card">
        <img src="@product.ImageUrl" alt="@product.Name" />
        <h3>@product.Name</h3>
        <p>$@product.Price</p>
    </div>
}
```

On the main page, we render the initial product list and add a special `div` at the bottom that will serve as our scroll-triggered loader. This div will contain an `hx-get` pointing to the next page of products and will activate when it’s revealed on screen:

```html
<div id="product-grid">
    @await Html.PartialAsync("_ProductCards", Model.Products)
</div>

<div 
    hx-get="/Products?handler=More&page=2" 
    hx-trigger="revealed" 
    hx-swap="beforeend" 
    hx-target="#product-grid"
    class="load-more-trigger">
</div>
```

The trick here is that `hx-trigger="revealed"` fires when the element becomes visible in the viewport. As the user scrolls, the trigger appears, htmx sends the request, and the response is inserted into the product grid using `hx-swap="beforeend"`. This means new products are appended instead of replacing the whole grid. On the server, we only send a fixed number of items per request (20 in this case), so performance stays predictable and snappy.

To keep the process going, the server’s response should include the next trigger element with an incremented page number, like so:

```html
@await Html.PartialAsync("_ProductCards", Model)

<div 
    hx-get="/Products?handler=More&page=@(Model.Page + 1)" 
    hx-trigger="revealed" 
    hx-swap="beforeend" 
    hx-target="#product-grid"
    class="load-more-trigger">
</div>
```

This creates a chain of revealed triggers, each pulling the next page when needed. You don’t need timers, IntersectionObserver, or any JavaScript to wire it up. And since the content is streamed from the server, you maintain control over layout, data shaping, and rendering.

Infinite scrolling with htmx offers the best of both worlds: a smooth, JavaScript-light frontend and full server-side control. Whether you're listing sneakers, blog posts, or customer transactions, this pattern keeps users moving and your app responsive. Next, we’ll look at how to add filters and sorting on top of this without breaking the lazy-load experience.

## Smarter Tables: Paging and Sorting with Razor Pages and htmx

While infinite scrolling is great for certain interfaces, sometimes your users just want control. Tables full of data—like admin dashboards, reports, or transaction histories—benefit from familiar pagination and sortable columns. htmx makes these features possible with very little overhead. You can implement both server-side pagination and sorting without any client-side JavaScript libraries. And best of all, it feels snappy and modern, thanks to dynamic updates using `hx-get`, `hx-target`, and `hx-swap`.

Let’s build out a common use case: a transaction history table. We’ll assume your data lives on the server, and you want to present it in pages of 10 or 20 items, sortable by columns like Date, Amount, and Description. The server is responsible for slicing and sorting the data, and htmx just fetches and injects the updated HTML when the user interacts with the UI.

We’ll begin with a Razor Page that accepts query parameters for pagination and sorting. Here's the model code:

```csharp
// Pages/Transactions.cshtml.cs
public class TransactionsModel : PageModel
{
    public List<Transaction> Transactions { get; set; } = new();
    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;
    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "Date";
    [BindProperty(SupportsGet = true)]
    public bool Desc { get; set; } = true;

    public PartialViewResult OnGetPage()
    {
        Transactions = TransactionService.GetPage(Page, 10, SortBy, Desc);
        return Partial("_TransactionRows", Transactions);
    }
}
```

We’re binding query parameters like `page`, `sortBy`, and `desc`, which will control the data slice returned by the server. The `_TransactionRows` partial will render just the rows of the table. On the main page, the table might look like this:

```html
<table class="table">
    <thead>
        <tr>
            <th>
                <a 
                    hx-get="/Transactions?handler=Page&sortBy=Date&desc=false" 
                    hx-target="#transactions-body" 
                    hx-swap="outerHTML"
                    hx-trigger="click">Date</a>
            </th>
            <th>
                <a 
                    hx-get="/Transactions?handler=Page&sortBy=Amount&desc=true" 
                    hx-target="#transactions-body" 
                    hx-swap="outerHTML"
                    hx-trigger="click">Amount</a>
            </th>
            <th>Description</th>
        </tr>
    </thead>
    <tbody id="transactions-body">
        @await Html.PartialAsync("_TransactionRows", Model.Transactions)
    </tbody>
</table>
```

Clicking a column header sends an `hx-get` to the page handler, specifying which field to sort by and whether it should be ascending or descending. The response is injected into the tbody, thanks to `hx-target="#transactions-body"` and `hx-swap="outerHTML"`. You can add logic in your handler to toggle the sort direction if you want that sort of UX.

For pagination, you can generate a basic pager with links or buttons at the bottom of the table. Here’s a simple example of numbered links that fetch pages dynamically:

```html
<div class="pagination">
    @for (int i = 1; i <= 5; i++)
    {
        <a 
            hx-get="/Transactions?handler=Page&page=@i&sortBy=@Model.SortBy&desc=@Model.Desc"
            hx-target="#transactions-body"
            hx-swap="outerHTML"
            class="page-link">@i</a>
    }
</div>
```

These links work just like the column headers. The only thing changing is the page parameter, and again, only the table body gets updated—no flickering, no full-page reloads.

This kind of setup gives your users a familiar experience with just the right amount of interactivity. And because all the logic lives server-side, it’s secure and easy to maintain. You’re not rebuilding table sorting or pagination logic in JavaScript. You’re simply re-rendering Razor partials in response to lightweight htmx requests.

Now that we’ve added paging and sorting, we’re ready to explore richer interactivity. In the next section, we’ll take a look at how to use buttons and links to drive inline actions—like approving transactions or deleting records—without leaving the page.

## Fast, Friendly, and Reusable: Optimizing Lists and Tables

By now, we’ve covered dynamic updates, sorting, paging, and lazy loading—core features that make lists and tables truly interactive. But good performance and UX aren’t just about fancy updates; they’re also about minimizing what you don’t need to do. When it comes to high-traffic or data-heavy applications, optimizing your interactions with smart caching, graceful loading states, and reusable component structure can have a huge impact on how fast and friendly your app feels.

One of the easiest wins is caching. Razor Pages with htmx can take advantage of HTTP caching headers or output caching to avoid regenerating the same markup repeatedly. For example, if your leaderboard or product listing doesn’t change every second, you can cache the result of your partial with [OutputCache] or even a custom memory cache. Here’s a simple way to add output caching to a Razor Page handler:

```csharp
[OutputCache(Duration = 10)]
public PartialViewResult OnGetTopPlayers()
{
    var players = LeaderboardService.GetTopPlayers(10);
    return Partial("_LeaderboardRows", players);`
}
```
This tells ASP.NET Core to cache the result of that partial for 10 seconds. Combined with htmx polling or on-demand updates, you can avoid redundant server calls and still keep things responsive. It’s especially useful for dashboards or public views where the data changes predictably.

From a UX perspective, never leave your users guessing. When data is loading, show a visual cue—an animated spinner, a “loading…” message, or a placeholder row. You can use hx-indicator to automatically tie a loading indicator to your htmx request:

```html
<div id="loading-spinner" class="spinner" style="display:none;"></div>

<table hx-get="/Transactions?handler=Page" 
       hx-target="#transaction-rows"
       hx-indicator="#loading-spinner">
    <!-- Table markup -->
</table>
```

htmx will automatically show the indicator when the request begins and hide it when the response comes back. And when there’s no data at all? That’s your cue to handle the empty state. Instead of a blank table, return a message that communicates clearly:

```html
@if (!Model.Transactions.Any())
{
    <tr>
        <td colspan="3">No transactions found.</td>
    </tr>
}
else
{
    foreach (var transaction in Model.Transactions)
    {
        <tr>
            <td>@transaction.Date</td>
            <td>@transaction.Amount</td>
            <td>@transaction.Description</td>
        </tr>
    }
}
```

Reusable components will also help keep your codebase clean and manageable. Use partial views for your table rows, extract shared filters or toolbars into dedicated Razor components, and follow consistent naming conventions. For example, if you have multiple list pages that all use a filter bar, extract that bar into a partial like _FilterToolbar.cshtml and pass any required parameters through the view model. This approach helps ensure that performance tweaks and UI changes stay DRY and centralized.

When you combine smart caching, responsive feedback, and reusable structure, you not only get better performance—you get maintainable code and happier users. It’s the kind of polish that separates an app that just works from one that feels effortless.

In the next chapter, we’ll take these interactive building blocks a step further by focusing on Buttons and Action Links. We'll explore how to enhance user interactions with one-click actions, confirmations, and inline updates—all powered by htmx and Razor Pages, of course.