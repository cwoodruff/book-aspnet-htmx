---
order: 29
icon: stack
label: Chap 2 - Setting Up Your Development Environment
meta:
title: "Setting Up Your Development Environment"
---
# 2

# Setting Up Your Development Environment

![chapter02-setting-up-dev-env.png](Images/chapter02-setting-up-dev-env.png)

The goal is to help you build a solid development environment for htmx in ASP.NET Core 9. This will ensure a smooth workflow and allow you to focus on building interactive web applications without unnecessary distractions. In this chapter, we will guide you through the process of installing .NET 9, setting up an ASP.NET Core Razor Pages project, adding htmx to the project, and configuring tools for an efficient development workflow.

# Installing ASP.NET Core 9 and Required Tools

The first step in setting up your environment is installing .NET 9. Microsoft provides an official .NET SDK that includes everything needed to build and run ASP.NET Core applications. You can download the latest SDK from the secure and official .NET website. Once installed, open a terminal or command prompt and verify the installation by running:

```bash
dotnet --version
```

If the command returns a version number starting with `9.`, your installation was successful. Next, let’s create a new Razor Pages project to use with htmx. Run the following commands:

```Bash
dotnet new razor -o MyHtmxApp
cd MyHtmxApp
dotnet run
```

This command initializes a basic Razor Pages project and starts a development server at `https://localhost:5001/`. Now, we are ready to integrate htmx.

## Adding htmx to an ASP.NET Core 9 Razor Pages Project

To use htmx, you must include its JavaScript file in your project. The easiest way to do this is by linking to the htmx CDN inside your `_Layout.cshtml` file. Open `Pages/Shared/_Layout.cshtml` and add the following inside the `<head>` tag:

```ASP.NET (C#)
<script src="https://unpkg.com/htmx.org@2.0.4"></script>
```

Alternatively, if you prefer to host the file locally, [download htmx.min.js](https://github.com/bigskysoftware/htmx/tree/master/dist) from the official htmx GitHub repo and place it inside the `wwwroot/js/` folder. Then, update _Layout.cshtml to reference it locally:

```ASP.NET (C#)
<script src="/js/htmx.min.js"></script>
```

To confirm that htmx is working correctly, create a simple button that triggers an AJAX request when clicked. Add the following to `Pages/Index.cshtml`:

```ASP.NET (C#)
<button hx-get="/Index?handler=hello" hx-target="#message">Click Me!</button>
<div id="message"></div>
```

Now, modify `Pages/Index.cshtml.cs` to handle the request and return a response:

```C#
public class IndexModel : PageModel
{
    public IActionResult OnGetHello()
    {
        return Content("<strong>Hello, htmx!</strong>", "text/html");
    }
}
```

Run your application and click the button. If "Hello, htmx!" appears in the `#message` div without a full page reload, congratulations! htmx is successfully integrated into your Razor Pages project.

## Configuring a Robust Development Workflow
A well-structured project is easier to maintain and allows for smooth development. Organizing Razor Pages into logical folders keeps things clean. Your project structure should look something like this:

```
MyHtmxApp/
|-- Pages/
|   |-- Shared/
|       |-- _Layout.cshtml
|   |-- Partials/
|   |-- Index.cshtml
|   |-- Index.cshtml.cs
|-- wwwroot/
|-- appsettings.json
```

Hot-reload is a valuable feature in ASP.NET Core that automatically applies changes without restarting the server. This is particularly useful when working with Razor Pages. To enable hot-reload, start your application using:

```Bash
dotnet watch run
```

This command monitors file changes and refreshes the application automatically. Additionally, debugging htmx requests is straightforward using browser developer tools. Open your browser's developer console and inspect network requests to see how htmx interacts with your server.

When working with htmx, the `HX-Request` header helps differentiate between standard and htmx-triggered requests. You can check this in your backend code to return different responses depending on whether the request originated from htmx:

```C#
public IActionResult OnGetHello()
{
    if (Request.Headers.ContainsKey("HX-Request"))
    {
        return Content("<strong>Hello, htmx!</strong>", "text/html");
    }
    return Page();
}
```

Version control is crucial for any project. To initialize a Git repository, navigate to your project directory and run:

```Bash
git init
git add .
git commit -m "Initial commit with htmx setup"
```

For Razor Pages projects, a typical `.gitignore` file should exclude compiled binaries and user-specific files. Here's an example:

```Bash
bin/
obj/
.vscode/
.idea/
```

## Conclusion

With your development environment set up to perfection, you are now fully prepared to embark on the journey of building interactive web applications with htmx and ASP.NET Core 9 Razor Pages. Having .NET installed, a Razor Pages project initialized, and htmx integrated, you are now ready to explore the world of dynamic, interactive applications. In the next chapter, we will delve into the core features of htmx and how to utilize them to create dynamic content updates in Razor Pages