using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Razor;

// Extensions/RazorPartialToString.cs
public static class RazorPartialToString
{
    public static async Task<string> RenderPartialViewToString(HttpContext httpContext, string viewPath, object model)
    {
        var serviceProvider = httpContext.RequestServices;
        var razorViewEngine = serviceProvider.GetRequiredService<IRazorViewEngine>();
        var tempDataProvider = serviceProvider.GetRequiredService<ITempDataProvider>();

        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor());
        
        var viewResult = razorViewEngine.FindView(actionContext, viewPath, false);

        if (!viewResult.Success)
        {
            throw new InvalidOperationException($"Couldn't find view '{viewPath}'");
        }

        var view = viewResult.View;
        using var output = new StringWriter();
        var viewContext = new ViewContext(
            actionContext,
            view,
            new ViewDataDictionary<object>(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary()) { Model = model },
            new TempDataDictionary(
                actionContext.HttpContext,
                tempDataProvider),
            output,
            new HtmlHelperOptions()
        );

        await view.RenderAsync(viewContext);
        return output.ToString();
    }
}
