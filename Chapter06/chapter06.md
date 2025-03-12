---
order: 25
icon: stack
label: Chap 6 - Working with hx-put, hx-patch, and hx-delete
meta:
title: "Working with hx-put, hx-patch, and hx-delete"
visibility: hidden
---
# 6

# Working with hx-put, hx-patch, and hx-delete

By incorporating `hx-put`, `hx-patch`, and `hx-delete` into your server interactions, you can significantly enhance your use of HTTP verbs. These commands, when combined with `hx-get` and `hx-post`, provide a comprehensive toolkit for robust, REST-like capabilities directly within your Razor Pages. This approach simplifies data updates and deletions, removing the need for complex JavaScript or extensive client-side frameworks.

In this chapter, you will discover how these htmx attributes can effortlessly manage tasks such as modifying partial records, handling complex data updates, and removing unwanted elements—all while keeping your server-side logic centralized. You'll see how using these verbs can streamline your workflow, maintain clean code, and enhance the user experience by providing near-instant feedback.

Once you have explored `hx-put`, `hx-patch`, and `hx-delete`, you will be equipped to perform real-world updates with minimal difficulty. These techniques will deepen your understanding of htmx and demonstrate how to leverage the full range of HTTP methods to keep your Razor Pages clean, efficient, and highly interactive.

## Embracing RESTful Updates in Razor Pages

RESTful principles emphasize the importance of using the correct HTTP methods to perform different actions, and htmx is an excellent fit for this approach. It provides easy-to-use HTML attributes for PUT, PATCH, and DELETE requests, aligning with RESTful conventions. Instead of relying solely on POST requests for every action, RESTful principles advocate using PUT for full updates, PATCH for partial modifications, and DELETE for removing data. This adherence to RESTful standards ensures that your code is both expressive and readable while following best practices, giving you confidence in your choice of technology.

Transitioning from using `hx-post` to `hx-put`, `hx-patch`, or `hx-delete` is a straightforward process. You're essentially instructing htmx to issue a different verb in the HTTP request, while the mechanics of fetching partial HTML and swapping it into your page remain unchanged. PUT is typically used to completely overwrite a resource, PATCH modifies it partially, and DELETE, as the name suggests, removes it entirely. The server code you write in your Razor Pages will handle these verbs by naming the handler methods appropriately, such as `OnPut()`, `OnPatch()`, and `OnDelete()`.

To set up these handlers, define them in your Razor Page’s code-behind. For example, if you have a page called `EditUser.cshtml.cs`, you might include methods like:

```C#
public IActionResult OnPutEdit(int userId, string newName)
{
    // Replace old data entirely
    // Update the user’s name in your data store
    return Content($"<div>Updated user {userId} to {newName}</div>", "text/html");
}

public IActionResult OnPatchEdit(int userId, string newName)
{
    // Partially update the user’s record, just changing the name
    return Content($"<div>Patched user {userId} to {newName}</div>", "text/html");
}

public IActionResult OnDeleteRemove(int userId)
{
    // Remove user from data store
    return Content($"<div>User {userId} has been removed</div>", "text/html");
}
```

Once the Razor Page logic is established, you can create the appropriate views that trigger these HTTP verbs using htmx attributes. For example, consider implementing inline content editing for a user's name with the `hx-put` attribute. When the user clicks on their name, a text field appears, allowing them to enter a new value and save it. This text field acts as an input for the new username.

```HTML
<span hx-get="/EditUser?handler=Edit&userId=1" hx-target="#editForm" hx-swap="innerHTML">
    John Doe
</span>
<div id="editForm"></div>

<!-- In the partial that returns on hx-get -->
<form hx-put="/EditUser?handler=Edit&userId=1" hx-target="#result" hx-swap="innerHTML">
    <input type="text" name="newName" value="John Doe" />
    <button type="submit">Save</button>
</form>
<div id="result"></div>
```

When you click on the name, a small form is loaded into the `editForm` container via an `hx-get` request. After entering a new name and submitting the form, htmx sends a PUT request to `/EditUser?handler=Edit&userId=1`. The `OnPutEdit` method in your Razor Page handles the request, updates the data store, and returns a snippet of HTML that is swapped into the `#result` div, which is essential for updating the user interface.

By adopting these methods, you create a more intuitive setup with a clearer separation of responsibilities. The server immediately understands the type of operation you want to perform, even before processing the code’s logic. This approach can lead to more consistent code across larger projects and prepares you to work with any tool or system that expects RESTful conventions by default.

## Streamlining Removals with hx-delete

Implementing data deletions with `hx-delete` not only allows you to seamlessly remove records but also provides users with immediate visual feedback, helping them understand the impact of your actions. By using the DELETE method instead of POST, your code becomes more expressive, clearly indicating the purpose of the request. When combined with Razor Pages’ `OnDelete()` handler, this method results in concise logic that communicates intent at a glance.

For instance, if your application has a list of items to manage, you can provide a button or link that issues a DELETE request when clicked. In Razor Pages, you would name your handler method `OnDeleteSomething()`, allowing htmx to send a DELETE request to that endpoint. This is a common approach to effectively handling deletions.

```C#
public IActionResult OnDeleteUser(int userId)
{
    // Perform data store removal of user
    bool success = DeleteUserFromDatabase(userId);
    if (!success)
        return BadRequest("Unable to delete user.");
    return Content($"<div>User {userId} was successfully deleted.</div>", "text/html");
}
```

For the UI side, you can attach hx-delete to a link or button that targets your Razor Page handler. Each user row or entry in your HTML might include a removal link like this:

```HTML
<div id="user-1">
    <span>Alice Smith</span>
    <button hx-delete="/Users?handler=User&userId=1"
            hx-target="#user-1"
            hx-swap="outerHTML">
        Delete
    </button>
</div>
```

When clicked, this triggers a DELETE request, which the server processes in the `OnDeleteUser()` method. If the request is successful, it returns a snippet of HTML. In this case, we use `outerHTML` for the `hx-swap` attribute, allowing us to remove the user’s entire container from the DOM once the response is received. This creates a smooth effect where the user disappears from the list without requiring a page refresh.

To provide users with additional feedback, you might choose to replace the deleted item’s row with a confirmation message. You can adjust the content returned in your `OnDelete` method or consider returning a partial view that confirms the user has been removed. Additionally, you could handle error conditions by returning an error message snippet, which htmx can then insert into the target area, informing users if something went wrong.

Using `hx-delete` enhances clarity by allowing you to differentiate removal operations from other interactions. It also makes your code easier to understand and maintain. You naturally adhere to REST-like conventions within Razor Pages, resulting in an interface that feels immediate and responsive. This keeps your users engaged and connected without the complexities of managing multiple frontend frameworks.

## Streamlined RESTful Responses

When using `hx-put`, `hx-patch`, and `hx-delete`, it's essential to customize your server responses to accurately indicate the success or failure of each request. For example, a `200 OK` status allows htmx to process an HTML snippet for updating your user interface. In contrast, a `204 No Content` status signals a successful update or deletion without needing to return any additional data. On the other hand, a `400 Bad Request` status indicates an error, and you can provide an error message to display on your page.

Returning a 200 status code along with a small HTML partial to update a section of the page is a common scenario. This status code, which represents a complete success, can be implemented in your Razor Page as follows:

```C#
public IActionResult OnPutUpdate(int id, string value)
{
    // Perform your update logic...
    return Content($"<div>Update successful for item {id}.</div>", "text/html");
}
```

If you don't need to send back any new content, returning a 204 status code helps keep the process lightweight. This is especially useful when the user interface needs to remove the old element or make adjustments following a successful request. You can use the `NoContent()` method for this purpose.

```C#
public IActionResult OnDeleteItem(int id)
{
    // Perform delete logic...
    return NoContent(); // Tells htmx there's nothing to replace, but it was successful
}
```

When dealing with invalid input or failures, returning a 400 status code can help you clearly communicate errors. You can include a brief explanation of what went wrong in the response body:

```C#
public IActionResult OnPatchData(int id, string newValue)
{
    if (string.IsNullOrEmpty(newValue))
        return BadRequest("Value cannot be empty.");
    // Otherwise, perform your partial update...
    return Content($"<p>Item {id} successfully patched to '{newValue}'</p>", "text/html");
}
```

To ensure your user interface (UI) accurately reflects each response, consider the location where the snippet is being injected. For a 200 response that includes HTML, it's advisable to target the parent container of the item so that htmx can visibly update the Document Object Model (DOM). If you return a 204 response, htmx won’t replace anything; therefore, make sure your client code accounts for the removal or refresh of relevant elements as necessary. In error cases, you can send back HTML that displays a message, targeting a specific container in the UI to ensure the message is clearly visible.

Debugging these RESTful htmx operations is straightforward using your browser’s developer tools. Simply open the Network tab to view the headers and status codes of each request. A quick scan will show the issued verb, the data sent, and the server's response. For real-time details, you can also attach event listeners for htmx hooks, such as htmx:beforeRequest or htmx:afterRequest, making the debugging process even easier.

In the next chapter, we will explore how to gain precise control over where and how UI elements are updated once your server responds. By mastering `hx-target` and `hx-swap`, you will be able to fine-tune everything from small inline changes to entire page segments, all while maintaining a consistent UI and ensuring quick interactions.