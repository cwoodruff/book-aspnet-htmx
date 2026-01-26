---
order: 1
icon: stack
label: Appendix D - Valuable Tools and Resources for htmx and Razor Pages Development
meta:
title: "Valuable Tools and Resources for htmx and Razor Pages Development"
---
# Appendix D: Tools and Resources

This appendix provides a curated collection of tools, libraries, and resources for htmx and ASP.NET Core Razor Pages development.

---

## D.1 Development Tools

### Browser Extensions

| Tool | Browser | Description |
|------|---------|-------------|
| **htmx Debugger** | Chrome | Inspect htmx requests, view headers, debug swaps |
| **htmx Devtools** | Firefox | Similar debugging for Firefox |
| **JSON Viewer** | Chrome/Firefox | Format JSON responses for inspection |

**htmx Debugger Chrome Extension:**
- View all htmx requests in dedicated panel
- Inspect HX-* request/response headers
- Monitor swap operations
- Available in Chrome Web Store

### Visual Studio Code Extensions

| Extension | Publisher | Description |
|-----------|-----------|-------------|
| **htmx-tags** | otovo-oss | Syntax highlighting and snippets for htmx |
| **Hyperscript** | dz4k | Hyperscript syntax highlighting |
| **C# Dev Kit** | Microsoft | C# language support |
| **ASP.NET Core Snippets** | rahulsahay19 | Razor Pages snippets |
| **Tailwind CSS IntelliSense** | Tailwind Labs | Tailwind autocomplete |
| **REST Client** | Huachao Mao | Test HTTP endpoints directly |
| **SQLite Viewer** | alexcvzz | View SQLite databases |
| **Live Server** | Ritwick Dey | Local development server |

**Recommended VS Code Settings for htmx:**

```json
{
  "emmet.includeLanguages": {
    "razor": "html",
    "aspnetcorerazor": "html"
  },
  "files.associations": {
    "*.cshtml": "aspnetcorerazor"
  },
  "[html]": {
    "editor.defaultFormatter": "vscode.html-language-features"
  }
}
```

### Visual Studio Extensions

| Extension | Description |
|-----------|-------------|
| **Web Essentials** | HTML/CSS/JS tooling |
| **Bundler & Minifier** | Bundle and minify static assets |
| **Image Optimizer** | Optimize images for web |
| **File Nesting** | Organize related files |
| **Add New File** | Quick file creation with templates |

### JetBrains Rider

| Feature | Description |
|---------|-------------|
| Built-in Razor support | Full IntelliSense for .cshtml files |
| HTTP Client | Test API endpoints |
| Database Tools | SQLite/SQL Server browsing |
| NuGet Browser | Package management |

### Command Line Tools

| Tool | Install | Purpose |
|------|---------|---------|
| **dotnet-ef** | `dotnet tool install -g dotnet-ef` | EF Core migrations |
| **dotnet-watch** | Built-in | Hot reload during development |
| **libman** | `dotnet tool install -g Microsoft.Web.LibraryManager.Cli` | Client library management |
| **httprepl** | `dotnet tool install -g Microsoft.dotnet-httprepl` | REST API testing |

---

## D.2 NuGet Packages

### htmx Integration

| Package | Description | Install |
|---------|-------------|---------|
| **Htmx** | Tag helpers and response extensions | `dotnet add package Htmx` |
| **Htmx.TagHelpers** | htmx-specific tag helpers | `dotnet add package Htmx.TagHelpers` |
| **Rizzy** | Razor component library with htmx | `dotnet add package Rizzy` |

**Htmx Package Usage:**

```csharp
// Check if htmx request
if (Request.IsHtmx())
{
    return Partial("_Content");
}

// Response extensions
Response.Htmx(h => {
    h.Retarget("#error");
    h.Reswap("innerHTML");
});
```

### Entity Framework Core

| Package | Purpose |
|---------|---------|
| **Microsoft.EntityFrameworkCore.Sqlite** | SQLite provider |
| **Microsoft.EntityFrameworkCore.SqlServer** | SQL Server provider |
| **Microsoft.EntityFrameworkCore.Design** | EF tooling |
| **Microsoft.EntityFrameworkCore.Tools** | PMC commands |

### Testing

| Package | Purpose |
|---------|---------|
| **Microsoft.AspNetCore.Mvc.Testing** | Integration testing |
| **Microsoft.Playwright** | Browser automation |
| **Moq** | Mocking framework |
| **FluentAssertions** | Assertion library |
| **Bogus** | Fake data generation |
| **AngleSharp** | HTML parsing for tests |
| **Verify.Http** | Snapshot testing for HTTP |

### Complementary Packages

| Package | Purpose |
|---------|---------|
| **Serilog.AspNetCore** | Structured logging |
| **FluentValidation** | Model validation |
| **AutoMapper** | Object mapping |
| **MediatR** | Mediator pattern |
| **Polly** | Resilience and transient fault handling |
| **HealthChecks.UI** | Health check dashboard |

---

## D.3 Client-Side Libraries

### htmx and Extensions

| Library | CDN URL |
|---------|---------|
| **htmx** | `https://unpkg.com/htmx.org@1.9.10` |
| **htmx (minified)** | `https://unpkg.com/htmx.org@1.9.10/dist/htmx.min.js` |
| **Hyperscript** | `https://unpkg.com/hyperscript.org@0.9.12` |

**htmx Extensions:**

```
https://unpkg.com/htmx.org@1.9.10/dist/ext/json-enc.js
https://unpkg.com/htmx.org@1.9.10/dist/ext/loading-states.js
https://unpkg.com/htmx.org@1.9.10/dist/ext/response-targets.js
https://unpkg.com/htmx.org@1.9.10/dist/ext/preload.js
https://unpkg.com/htmx.org@1.9.10/dist/ext/sse.js
https://unpkg.com/htmx.org@1.9.10/dist/ext/ws.js
```

### CSS Frameworks

| Framework | Notes | CDN |
|-----------|-------|-----|
| **Tailwind CSS** | Utility-first, pairs excellently with htmx | Play CDN or build |
| **Bootstrap** | Component library | `https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css` |
| **Pico CSS** | Minimal, classless | `https://unpkg.com/@picocss/pico@latest/css/pico.min.css` |
| **Simple.css** | Classless styling | `https://cdn.simplecss.org/simple.min.css` |
| **Water.css** | Classless, dark mode | `https://cdn.jsdelivr.net/npm/water.css@2/out/water.css` |

### Icon Libraries

| Library | CDN |
|---------|-----|
| **Heroicons** | `https://unpkg.com/heroicons@2.0.0/` |
| **Lucide** | `https://unpkg.com/lucide@latest` |
| **Feather Icons** | `https://unpkg.com/feather-icons@4.29.0/dist/feather.min.js` |
| **Bootstrap Icons** | `https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css` |

### Utility Libraries

| Library | Purpose | CDN |
|---------|---------|-----|
| **Alpine.js** | Lightweight reactivity | `https://unpkg.com/alpinejs@3.x.x/dist/cdn.min.js` |
| **morphdom** | DOM diffing | `https://unpkg.com/morphdom@2.7.0/dist/morphdom-umd.min.js` |
| **Sortable.js** | Drag and drop | `https://unpkg.com/sortablejs@1.15.0/Sortable.min.js` |

---

## D.4 Testing Tools

### Browser Testing

| Tool | Description | Website |
|------|-------------|---------|
| **Playwright** | Cross-browser automation | playwright.dev |
| **Selenium** | Browser automation | selenium.dev |
| **Cypress** | E2E testing | cypress.io |
| **Puppeteer** | Chrome automation | pptr.dev |

**Playwright with .NET:**

```bash
dotnet add package Microsoft.Playwright
pwsh bin/Debug/net8.0/playwright.ps1 install
```

### API Testing

| Tool | Type | Website |
|------|------|---------|
| **Postman** | GUI | postman.com |
| **Insomnia** | GUI | insomnia.rest |
| **Bruno** | GUI, Git-friendly | usebruno.com |
| **httpie** | CLI | httpie.io |
| **curl** | CLI | Built into most systems |

### Load Testing

| Tool | Description |
|------|-------------|
| **k6** | Modern load testing |
| **NBomber** | .NET load testing |
| **Apache JMeter** | Java-based load testing |
| **Artillery** | Node.js load testing |

---

## D.5 Online Resources

### Official Documentation

| Resource | URL |
|----------|-----|
| **htmx Documentation** | htmx.org/docs |
| **htmx Reference** | htmx.org/reference |
| **htmx Examples** | htmx.org/examples |
| **Hyperscript Documentation** | hyperscript.org/docs |
| **ASP.NET Core Documentation** | learn.microsoft.com/aspnet/core |
| **Razor Pages Documentation** | learn.microsoft.com/aspnet/core/razor-pages |
| **Entity Framework Core** | learn.microsoft.com/ef/core |

### Tutorials and Learning

| Resource | Description | URL |
|----------|-------------|-----|
| **htmx Essays** | Philosophy and patterns | htmx.org/essays |
| **Hypermedia Systems** | Free online book | hypermedia.systems |
| **.NET Documentation** | Official tutorials | learn.microsoft.com/dotnet |
| **Microsoft Learn** | Interactive modules | learn.microsoft.com |

### Community

| Resource | Description | URL |
|----------|-------------|-----|
| **htmx Discord** | Active community chat | htmx.org/discord |
| **htmx GitHub** | Source and issues | github.com/bigskysoftware/htmx |
| **r/htmx** | Reddit community | reddit.com/r/htmx |
| **r/dotnet** | .NET Reddit | reddit.com/r/dotnet |
| **Stack Overflow [htmx]** | Q&A | stackoverflow.com/questions/tagged/htmx |
| **Stack Overflow [razor-pages]** | Q&A | stackoverflow.com/questions/tagged/razor-pages |

### Blogs and Articles

| Blog | Author/Source | Focus |
|------|---------------|-------|
| **htmx.org Blog** | htmx team | htmx patterns and philosophy |
| **Andrew Lock** | andrewlock.net | ASP.NET Core deep dives |
| **.NET Blog** | Microsoft | Official .NET updates |
| **Scott Hanselman** | hanselman.com | .NET and web development |
| **David Fowler** | Twitter/GitHub | ASP.NET Core architecture |
| **Steve Smith** | ardalis.com | Clean architecture |

### Video Resources

| Channel/Course | Platform | Content |
|----------------|----------|---------|
| **htmx in 100 Seconds** | YouTube (Fireship) | Quick introduction |
| **Nick Chapsas** | YouTube | .NET and C# |
| **IAmTimCorey** | YouTube | C# tutorials |
| **Raw Coding** | YouTube | ASP.NET Core |
| **Pluralsight** | Subscription | Professional courses |

---

## D.6 Sample Projects

### GitHub Repositories

| Repository | Description |
|------------|-------------|
| **bigskysoftware/htmx** | htmx source code and examples |
| **bigskysoftware/contact-app** | Reference htmx application (Python) |
| **htmx-examples** | Community examples collection |
| **dotnet/aspnetcore** | ASP.NET Core source |
| **dotnet/eShop** | Reference .NET application |

### Project Templates

| Template | Install |
|----------|---------|
| **Razor Pages** | `dotnet new razor` |
| **Web App** | `dotnet new webapp` |
| **API** | `dotnet new webapi` |

**Custom htmx Template (create your own):**

```bash
dotnet new install ./MyHtmxTemplate
dotnet new htmx-razor -n MyProject
```

### Reference Implementations

| Project | Technology | Features |
|---------|------------|----------|
| **Chinook Dashboard** | Razor Pages + htmx | Full CRUD, search, modals |
| **TodoMVC-htmx** | Various backends | Classic todo app |
| **RealWorld htmx** | htmx spec | Blog platform |

---

## D.7 Books and References

### Recommended Reading

| Book | Author | Topic |
|------|--------|-------|
| **Hypermedia Systems** | Carson Gross et al. | htmx philosophy (free online) |
| **ASP.NET Core in Action** | Andrew Lock | ASP.NET Core comprehensive |
| **Entity Framework Core in Action** | Jon P Smith | EF Core deep dive |
| **C# in Depth** | Jon Skeet | C# language mastery |
| **Designing Data-Intensive Applications** | Martin Kleppmann | System design |

### Specifications and Standards

| Specification | URL |
|---------------|-----|
| **HTML Living Standard** | html.spec.whatwg.org |
| **HTTP Semantics (RFC 9110)** | httpwg.org/specs/rfc9110.html |
| **REST Dissertation** | ics.uci.edu/~fielding/pubs/dissertation/rest_arch_style.htm |
| **HATEOAS** | Part of REST architectural style |

---

## D.8 Quick Setup Checklist

### New Project Setup

```bash
# Create project
dotnet new webapp -n MyHtmxApp
cd MyHtmxApp

# Add packages
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Htmx

# Add client libraries
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
libman init
libman install htmx.org@1.9.10 -d wwwroot/lib/htmx
libman install hyperscript.org@0.9.12 -d wwwroot/lib/hyperscript

# Run with hot reload
dotnet watch
```

### Essential VS Code Extensions

```bash
code --install-extension ms-dotnettools.csdevkit
code --install-extension otovo-oss.htmx-tags
code --install-extension bradlc.vscode-tailwindcss
```

### Recommended libman.json

```json
{
  "version": "1.0",
  "defaultProvider": "unpkg",
  "libraries": [
    {
      "library": "htmx.org@1.9.10",
      "destination": "wwwroot/lib/htmx/",
      "files": ["dist/htmx.min.js", "dist/ext/json-enc.js", "dist/ext/loading-states.js"]
    },
    {
      "library": "hyperscript.org@0.9.12",
      "destination": "wwwroot/lib/hyperscript/",
      "files": ["dist/_hyperscript.min.js"]
    }
  ]
}
```

---

## Resource Links Summary

### Essential Links

| Resource | URL |
|----------|-----|
| htmx | htmx.org |
| Hyperscript | hyperscript.org |
| ASP.NET Core | learn.microsoft.com/aspnet/core |
| .NET Downloads | dotnet.microsoft.com/download |
| NuGet | nuget.org |
| GitHub | github.com |
| Stack Overflow | stackoverflow.com |

### CDN Quick Reference

```html
<!-- htmx -->
<script src="https://unpkg.com/htmx.org@1.9.10"></script>

<!-- Hyperscript -->
<script src="https://unpkg.com/hyperscript.org@0.9.12"></script>

<!-- Tailwind CSS (Play CDN - dev only) -->
<script src="https://cdn.tailwindcss.com"></script>

<!-- Alpine.js -->
<script defer src="https://unpkg.com/alpinejs@3.x.x/dist/cdn.min.js"></script>
```

---

*All URLs verified as of publication. Check official sources for latest versions.*


