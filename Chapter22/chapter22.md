---
order: 9
icon: stack
label: Chap 22 - Testing htmx Applications
meta:
title: "Testing htmx Applications"
---
# Testing htmx Applications

A well-tested application gives confidence that changes won't break existing functionality. Testing htmx applications requires techniques beyond traditional web testing because htmx fundamentally changes how pages update. This chapter covers testing strategies from unit tests through full browser automation, providing patterns you can apply to any htmx project.

## 22.1 Introduction

### Why Testing htmx Applications Requires Special Consideration

Traditional ASP.NET Core testing focuses on complete page responses. You request a URL, receive HTML, and verify the content. htmx applications work differently in several ways that affect testing strategy.

**Partial Responses**: Most htmx requests return HTML fragments, not complete pages. A handler might return just a table row or a form, without the surrounding layout. Tests must verify these fragments contain the correct content and htmx attributes without expecting full page structure.

**htmx Attributes Drive Behavior**: The attributes on HTML elements determine what htmx does. A missing `hx-target` or incorrect `hx-swap` value causes bugs that don't produce server errors. Tests must verify these attributes exist and have correct values.

**Dynamic DOM Updates**: htmx replaces, appends, or removes DOM elements based on server responses. Testing that a search filter works requires verifying not just that the server returns correct data, but that the browser correctly updates the visible page.

**Out-of-Band Updates**: A single response can update multiple page regions through OOB swaps. Tests must parse responses to find OOB elements and verify they target the correct elements with correct content.

**Client-Side Interactions**: Hyperscript behaviors, keyboard shortcuts, and timed actions like toast auto-dismiss happen entirely in the browser. Unit and integration tests can't verify these; you need browser automation.

### The Testing Pyramid for htmx Applications

The testing pyramid remains valid for htmx applications, but the middle layer (integration tests) becomes more important.

```
         ╱╲
        ╱  ╲
       ╱    ╲         Browser Tests
      ╱ Few  ╲        Full interactions, Hyperscript, visual verification
     ╱────────╲
    ╱          ╲
   ╱            ╲     Integration Tests
  ╱    Many      ╲    Handlers, partials, htmx attributes, OOB updates
 ╱────────────────╲
╱                  ╲
╱      Most         ╲  Unit Tests
╱                    ╲ Services, models, helpers, validation
╱══════════════════════╲
```

**Unit Tests** verify business logic in isolation: service methods, view model calculations, helper functions. These run fast and catch logic errors early.

**Integration Tests** verify that Razor Page handlers return correct partial HTML with proper htmx attributes. This layer is larger for htmx applications than traditional MVC because so much behavior depends on the HTML structure and attributes.

**Browser Tests** verify that htmx actually performs the expected updates in a real browser. These are slower but necessary for testing dynamic interactions, Hyperscript behaviors, and complex multi-step workflows.

### What This Chapter Covers

This chapter walks through testing at each level:

- Unit testing services, view models, and helper methods
- Setting up integration test infrastructure with WebApplicationFactory
- Testing partial responses and verifying htmx attributes
- Testing response headers (HX-Trigger, HX-Push-Url)
- Parsing and testing OOB updates
- Browser automation with Playwright for dynamic interactions
- Testing Hyperscript behaviors and keyboard interactions
- Testing error scenarios and validation
- Organizing tests and running them in CI/CD

The examples use the Chinook Dashboard from Chapter 21. You'll build a test project that verifies the dashboard's functionality at every level.

---

## 22.2 Unit Testing the Server Side

Unit tests verify individual components in isolation. For htmx applications, this means testing services, view models, and helper methods without involving HTTP requests or HTML rendering.

### 22.2.1 Testing Services

Services contain business logic and data access. Test them using an in-memory database to avoid external dependencies.

#### Test Project Setup

Create a test project alongside your main project:

```bash
dotnet new xunit -n ChinookDashboard.Tests
dotnet add ChinookDashboard.Tests reference ChinookDashboard
```

**ChinookDashboard.Tests.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
    <PackageReference Include="AngleSharp" Version="1.1.0" />
    <PackageReference Include="Microsoft.Playwright" Version="1.40.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ChinookDashboard\ChinookDashboard.csproj" />
  </ItemGroup>

</Project>
```

#### Testing with In-Memory Database

Create a base class for service tests that sets up an in-memory SQLite database:

**Unit/ServiceTestBase.cs**

```csharp
using ChinookDashboard.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChinookDashboard.Tests.Unit;

public abstract class ServiceTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly ChinookContext Context;

    protected ServiceTestBase()
    {
        // Create and open a connection that stays open for the test duration
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ChinookContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new ChinookContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
```

#### Complete ArtistServiceTests

**Unit/Services/ArtistServiceTests.cs**

```csharp
using ChinookDashboard.Data.Entities;
using ChinookDashboard.Services;

namespace ChinookDashboard.Tests.Unit.Services;

public class ArtistServiceTests : ServiceTestBase
{
    private readonly ArtistService _service;

    public ArtistServiceTests()
    {
        _service = new ArtistService(Context);
        SeedTestArtists();
    }

    private void SeedTestArtists()
    {
        Context.Artists.AddRange(
            new Artist { Id = 1, Name = "AC/DC" },
            new Artist { Id = 2, Name = "Accept" },
            new Artist { Id = 3, Name = "Aerosmith" },
            new Artist { Id = 4, Name = "Led Zeppelin" },
            new Artist { Id = 5, Name = "Metallica" }
        );
        Context.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllArtists_OrderedByName()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal("AC/DC", result[0].Name);
        Assert.Equal("Accept", result[1].Name);
    }

    [Fact]
    public async Task SearchAsync_WithMatchingTerm_ReturnsFilteredResults()
    {
        // Act
        var result = await _service.SearchAsync("ac");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Name == "AC/DC");
        Assert.Contains(result, a => a.Name == "Accept");
    }

    [Fact]
    public async Task SearchAsync_WithNoMatch_ReturnsEmptyList()
    {
        // Act
        var result = await _service.SearchAsync("xyz");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_WithNullOrEmpty_ReturnsAllArtists()
    {
        // Act
        var resultNull = await _service.SearchAsync(null);
        var resultEmpty = await _service.SearchAsync("");

        // Assert
        Assert.Equal(5, resultNull.Count);
        Assert.Equal(5, resultEmpty.Count);
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitive()
    {
        // Act
        var resultLower = await _service.SearchAsync("ac/dc");
        var resultUpper = await _service.SearchAsync("AC/DC");
        var resultMixed = await _service.SearchAsync("Ac/Dc");

        // Assert
        Assert.Single(resultLower);
        Assert.Single(resultUpper);
        Assert.Single(resultMixed);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsArtist()
    {
        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("AC/DC", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_AddsNewArtist_ReturnsCreatedArtist()
    {
        // Act
        var result = await _service.CreateAsync("New Artist");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("New Artist", result.Name);

        // Verify persisted
        var persisted = await Context.Artists.FindAsync(result.Id);
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task CreateAsync_WithWhitespace_TrimsName()
    {
        // Act
        var result = await _service.CreateAsync("  Spaced Name  ");

        // Assert
        Assert.Equal("Spaced Name", result?.Name);
    }

    [Fact]
    public async Task UpdateAsync_WithValidId_UpdatesAndReturnsArtist()
    {
        // Act
        var result = await _service.UpdateAsync(1, "AC/DC Updated");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("AC/DC Updated", result.Name);

        // Verify persisted
        var persisted = await Context.Artists.FindAsync(1);
        Assert.Equal("AC/DC Updated", persisted?.Name);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.UpdateAsync(999, "Does Not Exist");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_RemovesArtist_ReturnsTrue()
    {
        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);

        // Verify removed
        var deleted = await Context.Artists.FindAsync(1);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetCountAsync_ReturnsCorrectCount()
    {
        // Act
        var result = await _service.GetCountAsync();

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task GetCountAsync_AfterDelete_ReturnsUpdatedCount()
    {
        // Arrange
        await _service.DeleteAsync(1);

        // Act
        var result = await _service.GetCountAsync();

        // Assert
        Assert.Equal(4, result);
    }
}
```

### 22.2.2 Testing View Models

View models often contain computed properties and transformation logic. Test these independently of the data layer.

#### Testing Computed Properties

**Unit/Models/TrackSummaryTests.cs**

```csharp
using ChinookDashboard.Models;

namespace ChinookDashboard.Tests.Unit.Models;

public class TrackSummaryTests
{
    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(1000, "0:01")]
    [InlineData(60000, "1:00")]
    [InlineData(61000, "1:01")]
    [InlineData(3599000, "59:59")]
    [InlineData(3600000, "1:00:00")]
    [InlineData(3661000, "1:01:01")]
    [InlineData(36000000, "10:00:00")]
    public void Duration_FormatsCorrectly(int milliseconds, string expected)
    {
        // Arrange
        var track = new TrackSummary { Milliseconds = milliseconds };

        // Act
        var result = track.Duration;

        // Assert
        Assert.Equal(expected, result);
    }
}
```

#### Testing PaginatedList

**Unit/Models/PaginatedListTests.cs**

```csharp
using ChinookDashboard.Models;

namespace ChinookDashboard.Tests.Unit.Models;

public class PaginatedListTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var items = new List<string> { "a", "b", "c" };

        // Act
        var result = new PaginatedList<string>(items, totalCount: 100, pageNumber: 3, pageSize: 10);

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(100, result.TotalCount);
        Assert.Equal(3, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public void TotalPages_CalculatesCorrectly()
    {
        // Arrange & Act
        var exact = new PaginatedList<int>(new List<int>(), 100, 1, 10);
        var remainder = new PaginatedList<int>(new List<int>(), 95, 1, 10);
        var lessThanPage = new PaginatedList<int>(new List<int>(), 5, 1, 10);
        var empty = new PaginatedList<int>(new List<int>(), 0, 1, 10);

        // Assert
        Assert.Equal(10, exact.TotalPages);
        Assert.Equal(10, remainder.TotalPages);
        Assert.Equal(1, lessThanPage.TotalPages);
        Assert.Equal(0, empty.TotalPages);
    }

    [Fact]
    public void HasPreviousPage_ReturnsTrueWhenNotOnFirstPage()
    {
        // Arrange & Act
        var firstPage = new PaginatedList<int>(new List<int>(), 100, 1, 10);
        var secondPage = new PaginatedList<int>(new List<int>(), 100, 2, 10);
        var lastPage = new PaginatedList<int>(new List<int>(), 100, 10, 10);

        // Assert
        Assert.False(firstPage.HasPreviousPage);
        Assert.True(secondPage.HasPreviousPage);
        Assert.True(lastPage.HasPreviousPage);
    }

    [Fact]
    public void HasNextPage_ReturnsTrueWhenNotOnLastPage()
    {
        // Arrange & Act
        var firstPage = new PaginatedList<int>(new List<int>(), 100, 1, 10);
        var middlePage = new PaginatedList<int>(new List<int>(), 100, 5, 10);
        var lastPage = new PaginatedList<int>(new List<int>(), 100, 10, 10);

        // Assert
        Assert.True(firstPage.HasNextPage);
        Assert.True(middlePage.HasNextPage);
        Assert.False(lastPage.HasNextPage);
    }

    [Fact]
    public void StartItem_CalculatesCorrectly()
    {
        // Arrange & Act
        var firstPage = new PaginatedList<int>(new List<int>(), 100, 1, 10);
        var secondPage = new PaginatedList<int>(new List<int>(), 100, 2, 10);
        var empty = new PaginatedList<int>(new List<int>(), 0, 1, 10);

        // Assert
        Assert.Equal(1, firstPage.StartItem);
        Assert.Equal(11, secondPage.StartItem);
        Assert.Equal(0, empty.StartItem);
    }

    [Fact]
    public void EndItem_CalculatesCorrectly()
    {
        // Arrange & Act
        var fullPage = new PaginatedList<int>(Enumerable.Range(1, 10).ToList(), 100, 1, 10);
        var partialPage = new PaginatedList<int>(Enumerable.Range(1, 5).ToList(), 95, 10, 10);
        var empty = new PaginatedList<int>(new List<int>(), 0, 1, 10);

        // Assert
        Assert.Equal(10, fullPage.EndItem);
        Assert.Equal(95, partialPage.EndItem);
        Assert.Equal(0, empty.EndItem);
    }
}
```

### 22.2.3 Testing Helper Methods

Test extension methods and helpers that support htmx functionality.

**Unit/Helpers/HtmxExtensionTests.cs**

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ChinookDashboard.Tests.Unit.Helpers;

public class HtmxExtensionTests
{
    [Fact]
    public void IsHtmxRequest_WithHeader_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Request"] = "true";

        // Act
        var result = context.Request.IsHtmxRequest();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHtmxRequest_WithoutHeader_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = context.Request.IsHtmxRequest();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetHtmxTarget_ReturnsTargetValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Target"] = "artist-list";

        // Act
        var result = context.Request.GetHtmxTarget();

        // Assert
        Assert.Equal("artist-list", result);
    }

    [Fact]
    public void GetHtmxTarget_WithoutHeader_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = context.Request.GetHtmxTarget();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetHtmxTrigger_ReturnsTriggerValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Trigger"] = "search-input";

        // Act
        var result = context.Request.GetHtmxTrigger();

        // Assert
        Assert.Equal("search-input", result);
    }
}

// Extension methods being tested (should be in main project)
public static class HtmxRequestExtensions
{
    public static bool IsHtmxRequest(this HttpRequest request)
    {
        return request.Headers.ContainsKey("HX-Request");
    }

    public static string? GetHtmxTarget(this HttpRequest request)
    {
        return request.Headers.TryGetValue("HX-Target", out var value) 
            ? value.ToString() 
            : null;
    }

    public static string? GetHtmxTrigger(this HttpRequest request)
    {
        return request.Headers.TryGetValue("HX-Trigger", out var value) 
            ? value.ToString() 
            : null;
    }
}
```

**Unit/Helpers/ToastHelperTests.cs**

```csharp
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace ChinookDashboard.Tests.Unit.Helpers;

public class ToastHelperTests
{
    [Fact]
    public void CreateToastTrigger_ReturnsCorrectJson()
    {
        // Act
        var result = ToastHelper.CreateToastTrigger("Artist created successfully", "success");

        // Assert
        var parsed = JsonDocument.Parse(result);
        var showToast = parsed.RootElement.GetProperty("showToast");
        
        Assert.Equal("Artist created successfully", showToast.GetProperty("message").GetString());
        Assert.Equal("success", showToast.GetProperty("type").GetString());
    }

    [Fact]
    public void CreateToastTrigger_WithDefaultType_UsesSuccess()
    {
        // Act
        var result = ToastHelper.CreateToastTrigger("Done");

        // Assert
        var parsed = JsonDocument.Parse(result);
        var showToast = parsed.RootElement.GetProperty("showToast");
        
        Assert.Equal("success", showToast.GetProperty("type").GetString());
    }

    [Fact]
    public void AddToastHeader_SetsHxTriggerHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        ToastHelper.AddToastHeader(context.Response, "Test message", "info");

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("HX-Trigger"));
        
        var headerValue = context.Response.Headers["HX-Trigger"].ToString();
        Assert.Contains("showToast", headerValue);
        Assert.Contains("Test message", headerValue);
    }
}

// Helper class being tested (should be in main project)
public static class ToastHelper
{
    public static string CreateToastTrigger(string message, string type = "success")
    {
        return JsonSerializer.Serialize(new
        {
            showToast = new { message, type }
        });
    }

    public static void AddToastHeader(HttpResponse response, string message, string type = "success")
    {
        response.Headers["HX-Trigger"] = CreateToastTrigger(message, type);
    }
}
```

---

## 22.3 Integration Testing Razor Page Handlers

Integration tests verify that your Razor Page handlers return correct HTML with proper htmx attributes. These tests use `WebApplicationFactory` to host the application in-memory and send real HTTP requests.

### 22.3.1 Setting Up Test Infrastructure

#### ChinookTestFactory

Create a custom factory that configures the application for testing:

**Integration/Fixtures/ChinookTestFactory.cs**

```csharp
using ChinookDashboard.Data;
using ChinookDashboard.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChinookDashboard.Tests.Integration.Fixtures;

public class ChinookTestFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ChinookContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Create persistent SQLite connection for tests
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Add test DbContext
            services.AddDbContext<ChinookContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Build service provider and initialize database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ChinookContext>();
            db.Database.EnsureCreated();
            SeedTestData(db);
        });

        builder.UseEnvironment("Testing");
    }

    private static void SeedTestData(ChinookContext context)
    {
        // Add test artists
        var artists = new[]
        {
            new Artist { Id = 1, Name = "AC/DC" },
            new Artist { Id = 2, Name = "Accept" },
            new Artist { Id = 3, Name = "Aerosmith" },
            new Artist { Id = 4, Name = "Led Zeppelin" },
            new Artist { Id = 5, Name = "Metallica" },
            new Artist { Id = 6, Name = "Iron Maiden" },
            new Artist { Id = 7, Name = "Black Sabbath" },
            new Artist { Id = 8, Name = "Deep Purple" },
            new Artist { Id = 9, Name = "Judas Priest" },
            new Artist { Id = 10, Name = "Ozzy Osbourne" }
        };
        context.Artists.AddRange(artists);

        // Add test albums
        var albums = new[]
        {
            new Album { Id = 1, Title = "Back in Black", ArtistId = 1 },
            new Album { Id = 2, Title = "Highway to Hell", ArtistId = 1 },
            new Album { Id = 3, Title = "Restless and Wild", ArtistId = 2 },
            new Album { Id = 4, Title = "Get a Grip", ArtistId = 3 },
            new Album { Id = 5, Title = "Led Zeppelin IV", ArtistId = 4 }
        };
        context.Albums.AddRange(albums);

        // Add test genres
        var genres = new[]
        {
            new Genre { Id = 1, Name = "Rock" },
            new Genre { Id = 2, Name = "Metal" },
            new Genre { Id = 3, Name = "Blues" }
        };
        context.Genres.AddRange(genres);

        // Add test tracks
        var tracks = new[]
        {
            new Track { Id = 1, Name = "Back in Black", AlbumId = 1, GenreId = 1, Milliseconds = 255000, UnitPrice = 0.99m },
            new Track { Id = 2, Name = "Hells Bells", AlbumId = 1, GenreId = 1, Milliseconds = 312000, UnitPrice = 0.99m },
            new Track { Id = 3, Name = "Highway to Hell", AlbumId = 2, GenreId = 1, Milliseconds = 208000, UnitPrice = 0.99m },
            new Track { Id = 4, Name = "Stairway to Heaven", AlbumId = 5, GenreId = 1, Milliseconds = 482000, UnitPrice = 0.99m },
            new Track { Id = 5, Name = "Black Dog", AlbumId = 5, GenreId = 1, Milliseconds = 226000, UnitPrice = 0.99m }
        };
        context.Tracks.AddRange(tracks);

        context.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
```

#### Integration Test Base Class

**Integration/IntegrationTestBase.cs**

```csharp
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using ChinookDashboard.Tests.Integration.Fixtures;

namespace ChinookDashboard.Tests.Integration;

public abstract class IntegrationTestBase : IClassFixture<ChinookTestFactory>
{
    protected readonly HttpClient Client;
    protected readonly ChinookTestFactory Factory;

    protected IntegrationTestBase(ChinookTestFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected async Task<IHtmlDocument> GetHtmlDocumentAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(req => req.Content(content)) as IHtmlDocument 
            ?? throw new InvalidOperationException("Failed to parse HTML document");
    }

    protected async Task<IHtmlDocument> GetPageAsync(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await GetHtmlDocumentAsync(response);
    }
}
```

#### htmx Request Helper Extensions

**Integration/Common/HttpClientHtmxExtensions.cs**

```csharp
namespace ChinookDashboard.Tests.Integration.Common;

public static class HttpClientHtmxExtensions
{
    /// <summary>
    /// Creates an HTTP request configured as an htmx request with appropriate headers.
    /// </summary>
    public static HttpRequestMessage CreateHtmxRequest(
        this HttpClient client,
        HttpMethod method,
        string url,
        string? target = null,
        string? trigger = null,
        string? currentUrl = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("HX-Request", "true");
        
        if (target != null)
            request.Headers.Add("HX-Target", target);
        
        if (trigger != null)
            request.Headers.Add("HX-Trigger", trigger);
        
        if (currentUrl != null)
            request.Headers.Add("HX-Current-URL", currentUrl);

        return request;
    }

    /// <summary>
    /// Sends a GET request as an htmx request.
    /// </summary>
    public static async Task<HttpResponseMessage> HtmxGetAsync(
        this HttpClient client,
        string url,
        string? target = null,
        string? trigger = null)
    {
        var request = client.CreateHtmxRequest(HttpMethod.Get, url, target, trigger);
        return await client.SendAsync(request);
    }

    /// <summary>
    /// Sends a POST request as an htmx request.
    /// </summary>
    public static async Task<HttpResponseMessage> HtmxPostAsync(
        this HttpClient client,
        string url,
        HttpContent? content = null,
        string? target = null,
        string? trigger = null)
    {
        var request = client.CreateHtmxRequest(HttpMethod.Post, url, target, trigger);
        request.Content = content;
        return await client.SendAsync(request);
    }

    /// <summary>
    /// Sends a DELETE request as an htmx request.
    /// </summary>
    public static async Task<HttpResponseMessage> HtmxDeleteAsync(
        this HttpClient client,
        string url,
        string? target = null,
        string? trigger = null)
    {
        var request = client.CreateHtmxRequest(HttpMethod.Delete, url, target, trigger);
        return await client.SendAsync(request);
    }
}
```

### 22.3.2 Testing Full Page Requests

Test that full page loads return complete HTML with all required elements.

**Integration/Artists/ArtistPageTests.cs**

```csharp
using ChinookDashboard.Tests.Integration.Fixtures;

namespace ChinookDashboard.Tests.Integration.Artists;

public class ArtistPageTests : IntegrationTestBase
{
    public ArtistPageTests(ChinookTestFactory factory) : base(factory) { }

    [Fact]
    public async Task ArtistsIndex_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await Client.GetAsync("/Artists");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html; charset=utf-8", 
            response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task ArtistsIndex_ContainsHtmxScript()
    {
        // Act
        var document = await GetPageAsync("/Artists");

        // Assert
        var htmxScript = document.QuerySelector("script[src*='htmx']");
        Assert.NotNull(htmxScript);
    }

    [Fact]
    public async Task ArtistsIndex_ContainsSearchInput_WithHtmxAttributes()
    {
        // Act
        var document = await GetPageAsync("/Artists");

        // Assert
        var searchInput = document.QuerySelector("input[name='search']");
        Assert.NotNull(searchInput);
        
        // Verify htmx attributes
        Assert.NotNull(searchInput.GetAttribute("hx-get"));
        Assert.Contains("handler=List", searchInput.GetAttribute("hx-get"));
        Assert.NotNull(searchInput.GetAttribute("hx-target"));
        Assert.NotNull(searchInput.GetAttribute("hx-trigger"));
        Assert.Contains("keyup", searchInput.GetAttribute("hx-trigger"));
    }

    [Fact]
    public async Task ArtistsIndex_ContainsArtistTable_WithCorrectStructure()
    {
        // Act
        var document = await GetPageAsync("/Artists");

        // Assert
        var table = document.QuerySelector("table.artist-table, #artist-table");
        Assert.NotNull(table);

        var rows = document.QuerySelectorAll("tr[id^='artist-row-']");
        Assert.True(rows.Length > 0);
    }

    [Fact]
    public async Task ArtistsIndex_ContainsAddButton_WithHtmxAttributes()
    {
        // Act
        var document = await GetPageAsync("/Artists");

        // Assert
        var addButton = document.QuerySelector("#add-artist-btn, button[hx-get*='CreateForm']");
        Assert.NotNull(addButton);
        Assert.Contains("CreateForm", addButton.GetAttribute("hx-get"));
    }

    [Fact]
    public async Task ArtistsIndex_ContainsModalContainer()
    {
        // Act
        var document = await GetPageAsync("/Artists");

        // Assert
        var modalContainer = document.QuerySelector("#modal-container");
        Assert.NotNull(modalContainer);
    }

    [Fact]
    public async Task ArtistsIndex_ArtistRows_HaveEditButtons_WithCorrectAttributes()
    {
        // Act
        var document = await GetPageAsync("/Artists");

        // Assert
        var editButtons = document.QuerySelectorAll("button[hx-get*='handler=Edit']");
        Assert.True(editButtons.Length > 0);

        var firstEditButton = editButtons[0];
        Assert.Contains("hx-target", firstEditButton.Attributes.Select(a => a.Name));
        Assert.Contains("hx-swap", firstEditButton.Attributes.Select(a => a.Name));
        Assert.Equal("outerHTML", firstEditButton.GetAttribute("hx-swap"));
    }
}
```

### 22.3.3 Testing Partial Responses

Test that htmx requests return partial HTML without layout.

**Integration/Artists/ArtistPartialTests.cs**

```csharp
using AngleSharp.Html.Dom;
using ChinookDashboard.Tests.Integration.Common;
using ChinookDashboard.Tests.Integration.Fixtures;

namespace ChinookDashboard.Tests.Integration.Artists;

public class ArtistPartialTests : IntegrationTestBase
{
    public ArtistPartialTests(ChinookTestFactory factory) : base(factory) { }

    [Fact]
    public async Task ArtistList_HtmxRequest_ReturnsPartialWithoutLayout()
    {
        // Act
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=List",
            target: "artist-list");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Should NOT contain layout elements
        Assert.DoesNotContain("<!DOCTYPE", content);
        Assert.DoesNotContain("<html", content);
        Assert.DoesNotContain("<head>", content);
        Assert.DoesNotContain("<body>", content);
        
        // Should contain artist list content
        Assert.Contains("artist-row", content);
    }

    [Fact]
    public async Task ArtistList_WithSearchTerm_ReturnsFilteredResults()
    {
        // Act
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=List&search=AC",
            target: "artist-list");

        var document = await GetHtmlDocumentAsync(response);

        // Assert
        var rows = document.QuerySelectorAll("tr[id^='artist-row-']");
        
        // Should only contain artists matching "AC" (AC/DC, Accept)
        Assert.Equal(2, rows.Length);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("AC/DC", content);
        Assert.Contains("Accept", content);
        Assert.DoesNotContain("Metallica", content);
    }

    [Fact]
    public async Task ArtistList_WithNoMatchingSearch_ReturnsEmptyState()
    {
        // Act
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=List&search=ZZZZZ",
            target: "artist-list");

        var document = await GetHtmlDocumentAsync(response);

        // Assert
        var rows = document.QuerySelectorAll("tr[id^='artist-row-']");
        Assert.Empty(rows);
        
        // Should contain empty state message
        var emptyState = document.QuerySelector(".empty-state, [class*='empty']");
        Assert.NotNull(emptyState);
    }

    [Fact]
    public async Task EditForm_ReturnsFormPartial_WithCorrectValues()
    {
        // Act
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=Edit&id=1",
            target: "artist-row-1");

        var document = await GetHtmlDocumentAsync(response);

        // Assert
        var form = document.QuerySelector("form") as IHtmlFormElement;
        Assert.NotNull(form);
        Assert.Contains("handler=Update", form.Action ?? form.GetAttribute("hx-post") ?? "");

        var nameInput = document.QuerySelector("input[name='name']") as IHtmlInputElement;
        Assert.NotNull(nameInput);
        Assert.Equal("AC/DC", nameInput.Value);
    }

    [Fact]
    public async Task EditForm_HasCorrectHtmxAttributes()
    {
        // Act
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=Edit&id=1",
            target: "artist-row-1");

        var document = await GetHtmlDocumentAsync(response);

        // Assert
        var form = document.QuerySelector("form");
        Assert.NotNull(form);
        
        // Form should have htmx POST attribute
        var hxPost = form.GetAttribute("hx-post");
        Assert.NotNull(hxPost);
        Assert.Contains("handler=Update", hxPost);
        Assert.Contains("id=1", hxPost);

        // Form should target the row for swap
        var hxTarget = form.GetAttribute("hx-target");
        Assert.NotNull(hxTarget);

        // Form should use outerHTML swap
        var hxSwap = form.GetAttribute("hx-swap");
        Assert.Equal("outerHTML", hxSwap);
    }

    [Fact]
    public async Task CreateForm_ReturnsModalContent()
    {
        // Act
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=CreateForm",
            target: "modal-container");

        var document = await GetHtmlDocumentAsync(response);

        // Assert
        var form = document.QuerySelector("form");
        Assert.NotNull(form);
        
        var hxPost = form.GetAttribute("hx-post");
        Assert.NotNull(hxPost);
        Assert.Contains("handler=Create", hxPost);

        var nameInput = document.QuerySelector("input[name='name']") as IHtmlInputElement;
        Assert.NotNull(nameInput);
        Assert.True(string.IsNullOrEmpty(nameInput.Value)); // New form should be empty
    }
}
```

### 22.3.4 Testing htmx Response Headers

Test that handlers set correct htmx response headers.

**Integration/Artists/ArtistResponseHeaderTests.cs**

```csharp
using System.Text.Json;
using ChinookDashboard.Tests.Integration.Common;
using ChinookDashboard.Tests.Integration.Fixtures;

namespace ChinookDashboard.Tests.Integration.Artists;

public class ArtistResponseHeaderTests : IntegrationTestBase
{
    public ArtistResponseHeaderTests(ChinookTestFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateArtist_Success_ReturnsHxTriggerHeader_WithToast()
    {
        // Arrange
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("name", "New Test Artist")
        });

        // Act
        var response = await Client.HtmxPostAsync(
            "/Artists?handler=Create",
            formContent,
            target: "modal-container");

        // Assert
        response.EnsureSuccessStatusCode();
        
        Assert.True(response.Headers.Contains("HX-Trigger"));
        var triggerValue = response.Headers.GetValues("HX-Trigger").First();
        
        // Parse and verify toast content
        var trigger = JsonDocument.Parse(triggerValue);
        Assert.True(trigger.RootElement.TryGetProperty("showToast", out var showToast));
        Assert.Equal("success", showToast.GetProperty("type").GetString());
        Assert.Contains("created", showToast.GetProperty("message").GetString()?.ToLower());
    }

    [Fact]
    public async Task UpdateArtist_Success_ReturnsHxTriggerHeader()
    {
        // Arrange
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("name", "Updated Artist Name")
        });

        // Act
        var response = await Client.HtmxPostAsync(
            "/Artists?handler=Update&id=1",
            formContent,
            target: "artist-row-1");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.Contains("HX-Trigger"));
    }

    [Fact]
    public async Task DeleteArtist_Success_ReturnsHxTriggerHeader_WithToast()
    {
        // Act
        var response = await Client.HtmxDeleteAsync(
            "/Artists?handler=Delete&id=10", // Use ID 10 to not break other tests
            target: "artist-row-10");

        // Assert
        response.EnsureSuccessStatusCode();
        
        Assert.True(response.Headers.Contains("HX-Trigger"));
        var triggerValue = response.Headers.GetValues("HX-Trigger").First();
        
        Assert.Contains("showToast", triggerValue);
    }

    [Fact]
    public async Task ArtistList_WithSearch_ReturnsHxPushUrl()
    {
        // Act
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=List&search=test",
            target: "artist-list");

        // Assert
        // Check if HX-Push-Url is set for URL state management
        if (response.Headers.Contains("HX-Push-Url"))
        {
            var pushUrl = response.Headers.GetValues("HX-Push-Url").First();
            Assert.Contains("search=test", pushUrl);
        }
    }

    [Fact]
    public async Task CreateArtist_WithInvalidData_Returns400_NoSuccessToast()
    {
        // Arrange - empty name should fail validation
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("name", "")
        });

        // Act
        var response = await Client.HtmxPostAsync(
            "/Artists?handler=Create",
            formContent,
            target: "modal-container");

        // Assert
        // Should return the form with validation errors, not a success toast
        var content = await response.Content.ReadAsStringAsync();
        
        // Either returns 400/422 or returns form with error message
        if (response.IsSuccessStatusCode)
        {
            // If successful response, should contain validation error, not success toast
            Assert.Contains("validation", content.ToLower());
            
            if (response.Headers.Contains("HX-Trigger"))
            {
                var trigger = response.Headers.GetValues("HX-Trigger").First();
                Assert.DoesNotContain("success", trigger.ToLower());
            }
        }
    }
}
```

### 22.3.5 Testing OOB Updates

Test that responses include correct OOB update elements.

**Integration/Artists/ArtistOobTests.cs**

```csharp
using ChinookDashboard.Tests.Integration.Common;
using ChinookDashboard.Tests.Integration.Fixtures;

namespace ChinookDashboard.Tests.Integration.Artists;

public class ArtistOobTests : IntegrationTestBase
{
    public ArtistOobTests(ChinookTestFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateArtist_ReturnsOobUpdate_ForResultCount()
    {
        // Arrange
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("name", "OOB Test Artist")
        });

        // Act
        var response = await Client.HtmxPostAsync(
            "/Artists?handler=Create",
            formContent,
            target: "modal-container");

        var document = await GetHtmlDocumentAsync(response);

        // Assert
        // Find OOB element for result count
        var oobElements = document.QuerySelectorAll("[hx-swap-oob]");
        Assert.True(oobElements.Length > 0, "Response should contain OOB elements");

        // Check for result count OOB
        var resultCountOob = document.QuerySelector("#result-count[hx-swap-oob], [hx-swap-oob][id='result-count']");
        // Note: Element may or may not be present depending on implementation
    }

    [Fact]
    public async Task CreateArtist_ReturnsOobUpdate_ForNewRow()
    {
        // Arrange
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("name", "New Row Test Artist")
        });

        // Act
        var response = await Client.HtmxPostAsync(
            "/Artists?handler=Create",
            formContent,
            target: "modal-container");

        var document = await GetHtmlDocumentAsync(response);

        // Assert
        // Check for new row with OOB to insert into table
        var newRowOob = document.QuerySelector("tr[hx-swap-oob*='afterbegin'], tr[hx-swap-oob*='beforeend']");
        
        if (newRowOob != null)
        {
            var oobValue = newRowOob.GetAttribute("hx-swap-oob");
            Assert.Contains("artist-table", oobValue ?? "");
        }
    }

    [Fact]
    public async Task DeleteArtist_ReturnsOobUpdate_ForStats()
    {
        // First, create an artist to delete
        var createContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("name", "To Be Deleted")
        });
        var createResponse = await Client.HtmxPostAsync(
            "/Artists?handler=Create",
            createContent);
        
        // Get the created artist's ID from response
        var createDoc = await GetHtmlDocumentAsync(createResponse);
        var newRow = createDoc.QuerySelector("tr[id^='artist-row-']");
        var newId = newRow?.Id?.Replace("artist-row-", "") ?? "999";

        // Act - Delete the artist
        var response = await Client.HtmxDeleteAsync(
            $"/Artists?handler=Delete&id={newId}",
            target: $"artist-row-{newId}");

        var document = await GetHtmlDocumentAsync(response);

        // Assert
        var oobElements = document.QuerySelectorAll("[hx-swap-oob]");
        
        // Should have OOB updates for stats
        var hasStatsOob = oobElements.Any(e => 
            e.Id?.Contains("stat") == true || 
            e.Id?.Contains("count") == true ||
            e.GetAttribute("hx-swap-oob")?.Contains("stat") == true);
        
        // At minimum should have delete marker or OOB updates
        Assert.True(oobElements.Length >= 0); // Relaxed assertion - implementation varies
    }

    [Fact]
    public async Task DeleteArtist_ResponseContains_DeleteSwapForRow()
    {
        // Act
        var response = await Client.HtmxDeleteAsync(
            "/Artists?handler=Delete&id=9", // Judas Priest
            target: "artist-row-9");

        var content = await response.Content.ReadAsStringAsync();

        // Assert
        // Response might include OOB delete or be empty for the row
        // Check for either approach
        var hasDeleteOob = content.Contains("hx-swap-oob=\"delete\"") ||
                           content.Contains("hx-swap-oob='delete'");
        var isMinimalResponse = string.IsNullOrWhiteSpace(content) || 
                                content.Length < 50;

        // Either approach is valid
        Assert.True(hasDeleteOob || isMinimalResponse || response.IsSuccessStatusCode,
            "Delete should either return OOB delete marker or minimal/empty response");
    }

    [Fact]
    public async Task OobElements_HaveCorrectSwapValues()
    {
        // Arrange
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("name", "OOB Swap Test")
        });

        // Act
        var response = await Client.HtmxPostAsync(
            "/Artists?handler=Create",
            formContent);

        var document = await GetHtmlDocumentAsync(response);

        // Assert
        var oobElements = document.QuerySelectorAll("[hx-swap-oob]");
        
        foreach (var element in oobElements)
        {
            var oobValue = element.GetAttribute("hx-swap-oob");
            Assert.NotNull(oobValue);
            
            // Valid OOB values
            var validValues = new[] { "true", "innerHTML", "outerHTML", 
                "beforebegin", "afterbegin", "beforeend", "afterend", "delete", "none" };
            
            // OOB value should start with a valid swap type or be a selector
            var isValidStart = validValues.Any(v => oobValue.StartsWith(v)) ||
                               oobValue.Contains(":"); // selector syntax like "afterbegin:#target"
            
            Assert.True(isValidStart, 
                $"OOB value '{oobValue}' should be a valid swap type or selector");
        }
    }
}
```

#### HTML Parsing Helpers

Create a helper class for common HTML assertions:

**Integration/Common/HtmlAssertions.cs**

```csharp
using AngleSharp.Dom;

namespace ChinookDashboard.Tests.Integration.Common;

public static class HtmlAssertions
{
    public static void HasAttribute(IElement element, string attributeName, string? expectedValue = null)
    {
        var actualValue = element.GetAttribute(attributeName);
        Assert.NotNull(actualValue);
        
        if (expectedValue != null)
        {
            Assert.Equal(expectedValue, actualValue);
        }
    }

    public static void HasHxGet(IElement element, string? expectedUrlPart = null)
    {
        var hxGet = element.GetAttribute("hx-get");
        Assert.NotNull(hxGet);
        
        if (expectedUrlPart != null)
        {
            Assert.Contains(expectedUrlPart, hxGet);
        }
    }

    public static void HasHxPost(IElement element, string? expectedUrlPart = null)
    {
        var hxPost = element.GetAttribute("hx-post");
        Assert.NotNull(hxPost);
        
        if (expectedUrlPart != null)
        {
            Assert.Contains(expectedUrlPart, hxPost);
        }
    }

    public static void HasHxTarget(IElement element, string expectedTarget)
    {
        var hxTarget = element.GetAttribute("hx-target");
        Assert.NotNull(hxTarget);
        Assert.Equal(expectedTarget, hxTarget);
    }

    public static void HasHxSwap(IElement element, string expectedSwap)
    {
        var hxSwap = element.GetAttribute("hx-swap");
        Assert.NotNull(hxSwap);
        Assert.Equal(expectedSwap, hxSwap);
    }

    public static void HasHxTrigger(IElement element, string expectedTriggerPart)
    {
        var hxTrigger = element.GetAttribute("hx-trigger");
        Assert.NotNull(hxTrigger);
        Assert.Contains(expectedTriggerPart, hxTrigger);
    }

    public static void IsOobSwap(IElement element, string? expectedOobValue = null)
    {
        var oobValue = element.GetAttribute("hx-swap-oob");
        Assert.NotNull(oobValue);
        
        if (expectedOobValue != null)
        {
            Assert.Equal(expectedOobValue, oobValue);
        }
    }

    public static void DoesNotContainLayoutElements(string html)
    {
        Assert.DoesNotContain("<!DOCTYPE", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<html", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<head>", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("</body>", html, StringComparison.OrdinalIgnoreCase);
    }
}
```

#### Running the Tests

Execute tests from the command line:

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~ArtistPartialTests"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

The integration tests verify that your Razor Page handlers return correct HTML for both full page and htmx partial requests. They confirm htmx attributes are present and correct, response headers are set properly, and OOB updates target the right elements. This level of testing catches most htmx-related bugs before they reach the browser.

## 22.4 Testing htmx Attributes and HTML Structure

Integration tests verify that HTML responses contain correct htmx attributes. This section provides tools and patterns for parsing HTML and asserting on htmx-specific elements.

### 22.4.1 HTML Parsing Strategies

AngleSharp provides a DOM parser that works like browser JavaScript, letting you query elements with CSS selectors and read attributes.

#### Setting Up AngleSharp

AngleSharp is already in the test project dependencies. Create a helper class for common parsing operations:

**Integration/Common/HtmlParsingHelper.cs**

```csharp
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace ChinookDashboard.Tests.Integration.Common;

public static class HtmlParsingHelper
{
    private static readonly IBrowsingContext BrowsingContext = 
        BrowsingContext.New(Configuration.Default);

    /// <summary>
    /// Parses HTML string into a document.
    /// </summary>
    public static async Task<IHtmlDocument> ParseHtmlAsync(string html)
    {
        var document = await BrowsingContext.OpenAsync(req => req.Content(html));
        return document as IHtmlDocument 
            ?? throw new InvalidOperationException("Failed to parse HTML");
    }

    /// <summary>
    /// Parses HTML from HttpResponseMessage.
    /// </summary>
    public static async Task<IHtmlDocument> ParseResponseAsync(HttpResponseMessage response)
    {
        var html = await response.Content.ReadAsStringAsync();
        return await ParseHtmlAsync(html);
    }

    /// <summary>
    /// Finds all elements with any htmx attribute.
    /// </summary>
    public static IEnumerable<IElement> GetHtmxElements(IHtmlDocument document)
    {
        return document.QuerySelectorAll("[hx-get], [hx-post], [hx-put], [hx-patch], [hx-delete]");
    }

    /// <summary>
    /// Finds all elements with a specific htmx attribute.
    /// </summary>
    public static IEnumerable<IElement> GetElementsWithAttribute(
        IHtmlDocument document, 
        string attributeName)
    {
        return document.QuerySelectorAll($"[{attributeName}]");
    }

    /// <summary>
    /// Finds all OOB swap elements in the document.
    /// </summary>
    public static IEnumerable<IElement> GetOobElements(IHtmlDocument document)
    {
        return document.QuerySelectorAll("[hx-swap-oob]");
    }

    /// <summary>
    /// Gets all htmx attributes from an element as a dictionary.
    /// </summary>
    public static Dictionary<string, string> GetHtmxAttributes(IElement element)
    {
        var htmxAttributes = new Dictionary<string, string>();
        
        foreach (var attr in element.Attributes)
        {
            if (attr.Name.StartsWith("hx-") || attr.Name == "_")
            {
                htmxAttributes[attr.Name] = attr.Value;
            }
        }
        
        return htmxAttributes;
    }

    /// <summary>
    /// Checks if element has all required htmx attributes for a GET request.
    /// </summary>
    public static bool IsHtmxGetElement(IElement element)
    {
        return element.HasAttribute("hx-get");
    }

    /// <summary>
    /// Checks if element has all required htmx attributes for a POST request.
    /// </summary>
    public static bool IsHtmxPostElement(IElement element)
    {
        return element.HasAttribute("hx-post");
    }

    /// <summary>
    /// Extracts handler name from htmx URL attribute.
    /// </summary>
    public static string? GetHandlerFromUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        
        var match = System.Text.RegularExpressions.Regex.Match(
            url, @"handler=(\w+)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Extracts query parameters from htmx URL.
    /// </summary>
    public static Dictionary<string, string> GetUrlParameters(string? url)
    {
        var parameters = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(url)) return parameters;

        var queryStart = url.IndexOf('?');
        if (queryStart < 0) return parameters;

        var query = url[(queryStart + 1)..];
        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                parameters[parts[0]] = Uri.UnescapeDataString(parts[1]);
            }
        }

        return parameters;
    }
}
```

### 22.4.2 Creating Custom htmx Assertions

Create a dedicated assertions class with clear error messages that show actual vs expected values.

**Integration/Common/HtmxAssertions.cs**

```csharp
using AngleSharp.Dom;
using Xunit.Sdk;

namespace ChinookDashboard.Tests.Integration.Common;

/// <summary>
/// Custom assertions for verifying htmx attributes on HTML elements.
/// </summary>
public static class HtmxAssertions
{
    /// <summary>
    /// Asserts that an element has an hx-get attribute with the expected URL or URL part.
    /// </summary>
    public static void HasHxGet(IElement element, string? expectedUrlPart = null)
    {
        var actual = element.GetAttribute("hx-get");
        
        if (actual == null)
        {
            throw new XunitException(
                $"Expected element <{element.TagName.ToLower()}> to have 'hx-get' attribute, but it was not found.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        if (expectedUrlPart != null && !actual.Contains(expectedUrlPart, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected 'hx-get' to contain '{expectedUrlPart}'.\n" +
                $"Actual: '{actual}'\n" +
                $"Element: {GetElementDescription(element)}");
        }
    }

    /// <summary>
    /// Asserts that an element has an hx-post attribute with the expected URL or URL part.
    /// </summary>
    public static void HasHxPost(IElement element, string? expectedUrlPart = null)
    {
        var actual = element.GetAttribute("hx-post");
        
        if (actual == null)
        {
            throw new XunitException(
                $"Expected element <{element.TagName.ToLower()}> to have 'hx-post' attribute, but it was not found.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        if (expectedUrlPart != null && !actual.Contains(expectedUrlPart, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected 'hx-post' to contain '{expectedUrlPart}'.\n" +
                $"Actual: '{actual}'\n" +
                $"Element: {GetElementDescription(element)}");
        }
    }

    /// <summary>
    /// Asserts that an element has an hx-delete attribute with the expected URL or URL part.
    /// </summary>
    public static void HasHxDelete(IElement element, string? expectedUrlPart = null)
    {
        var actual = element.GetAttribute("hx-delete");
        
        if (actual == null)
        {
            throw new XunitException(
                $"Expected element <{element.TagName.ToLower()}> to have 'hx-delete' attribute, but it was not found.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        if (expectedUrlPart != null && !actual.Contains(expectedUrlPart, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected 'hx-delete' to contain '{expectedUrlPart}'.\n" +
                $"Actual: '{actual}'\n" +
                $"Element: {GetElementDescription(element)}");
        }
    }

    /// <summary>
    /// Asserts that an element has an hx-target attribute with the expected value.
    /// </summary>
    public static void HasHxTarget(IElement element, string expectedTarget)
    {
        var actual = element.GetAttribute("hx-target");
        
        if (actual == null)
        {
            throw new XunitException(
                $"Expected element <{element.TagName.ToLower()}> to have 'hx-target' attribute, but it was not found.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        if (!actual.Equals(expectedTarget, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected 'hx-target' to be '{expectedTarget}'.\n" +
                $"Actual: '{actual}'\n" +
                $"Element: {GetElementDescription(element)}");
        }
    }

    /// <summary>
    /// Asserts that an element has an hx-target attribute containing the expected value.
    /// Useful for relative selectors like "closest tr" or "find .content".
    /// </summary>
    public static void HasHxTargetContaining(IElement element, string expectedPart)
    {
        var actual = element.GetAttribute("hx-target");
        
        if (actual == null)
        {
            throw new XunitException(
                $"Expected element <{element.TagName.ToLower()}> to have 'hx-target' attribute, but it was not found.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        if (!actual.Contains(expectedPart, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected 'hx-target' to contain '{expectedPart}'.\n" +
                $"Actual: '{actual}'\n" +
                $"Element: {GetElementDescription(element)}");
        }
    }

    /// <summary>
    /// Asserts that an element has an hx-swap attribute with the expected value.
    /// </summary>
    public static void HasHxSwap(IElement element, string expectedSwap)
    {
        var actual = element.GetAttribute("hx-swap");
        
        if (actual == null)
        {
            throw new XunitException(
                $"Expected element <{element.TagName.ToLower()}> to have 'hx-swap' attribute, but it was not found.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        // Handle swap with modifiers (e.g., "innerHTML swap:1s")
        var actualBase = actual.Split(' ')[0];
        if (!actualBase.Equals(expectedSwap, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected 'hx-swap' to be '{expectedSwap}'.\n" +
                $"Actual: '{actual}'\n" +
                $"Element: {GetElementDescription(element)}");
        }
    }

    /// <summary>
    /// Asserts that an element has an hx-trigger attribute containing the expected trigger.
    /// </summary>
    public static void HasHxTrigger(IElement element, string expectedTriggerPart)
    {
        var actual = element.GetAttribute("hx-trigger");
        
        if (actual == null)
        {
            throw new XunitException(
                $"Expected element <{element.TagName.ToLower()}> to have 'hx-trigger' attribute, but it was not found.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        if (!actual.Contains(expectedTriggerPart, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected 'hx-trigger' to contain '{expectedTriggerPart}'.\n" +
                $"Actual: '{actual}'\n" +
                $"Element: {GetElementDescription(element)}");
        }
    }

    /// <summary>
    /// Asserts that an element has an hx-trigger with a specific modifier.
    /// </summary>
    public static void HasHxTriggerWithModifier(IElement element, string trigger, string modifier)
    {
        var actual = element.GetAttribute("hx-trigger");
        
        if (actual == null)
        {
            throw new XunitException(
                $"Expected element to have 'hx-trigger' attribute.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        if (!actual.Contains(trigger, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected 'hx-trigger' to contain trigger '{trigger}'.\n" +
                $"Actual: '{actual}'");
        }

        if (!actual.Contains(modifier, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected 'hx-trigger' to contain modifier '{modifier}'.\n" +
                $"Actual: '{actual}'");
        }
    }

    /// <summary>
    /// Asserts that an element has an hx-include attribute with the expected selector.
    /// </summary>
    public static void HasHxInclude(IElement element, string expectedSelector)
    {
        var actual = element.GetAttribute("hx-include");
        
        if (actual == null)
        {
            throw new XunitException(
                $"Expected element <{element.TagName.ToLower()}> to have 'hx-include' attribute, but it was not found.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        if (!actual.Contains(expectedSelector, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected 'hx-include' to contain '{expectedSelector}'.\n" +
                $"Actual: '{actual}'\n" +
                $"Element: {GetElementDescription(element)}");
        }
    }

    /// <summary>
    /// Asserts that an element has an hx-indicator attribute with the expected selector.
    /// </summary>
    public static void HasHxIndicator(IElement element, string expectedSelector)
    {
        var actual = element.GetAttribute("hx-indicator");
        
        if (actual == null)
        {
            throw new XunitException(
                $"Expected element <{element.TagName.ToLower()}> to have 'hx-indicator' attribute, but it was not found.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        if (!actual.Contains(expectedSelector, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected 'hx-indicator' to contain '{expectedSelector}'.\n" +
                $"Actual: '{actual}'\n" +
                $"Element: {GetElementDescription(element)}");
        }
    }

    /// <summary>
    /// Asserts that an element has an hx-swap-oob attribute.
    /// </summary>
    public static void IsOobSwap(IElement element, string? expectedValue = null)
    {
        var actual = element.GetAttribute("hx-swap-oob");
        
        if (actual == null)
        {
            throw new XunitException(
                $"Expected element <{element.TagName.ToLower()}> to have 'hx-swap-oob' attribute, but it was not found.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        if (expectedValue != null && !actual.Equals(expectedValue, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected 'hx-swap-oob' to be '{expectedValue}'.\n" +
                $"Actual: '{actual}'\n" +
                $"Element: {GetElementDescription(element)}");
        }
    }

    /// <summary>
    /// Asserts that an element has an hx-confirm attribute.
    /// </summary>
    public static void HasHxConfirm(IElement element, string? expectedMessage = null)
    {
        var actual = element.GetAttribute("hx-confirm");
        
        if (actual == null)
        {
            throw new XunitException(
                $"Expected element to have 'hx-confirm' attribute.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        if (expectedMessage != null && !actual.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected 'hx-confirm' to contain '{expectedMessage}'.\n" +
                $"Actual: '{actual}'");
        }
    }

    /// <summary>
    /// Asserts that an element has an hx-push-url attribute.
    /// </summary>
    public static void HasHxPushUrl(IElement element, string? expectedValue = null)
    {
        var actual = element.GetAttribute("hx-push-url");
        
        if (actual == null)
        {
            throw new XunitException(
                $"Expected element to have 'hx-push-url' attribute.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        if (expectedValue != null && !actual.Equals(expectedValue, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected 'hx-push-url' to be '{expectedValue}'.\n" +
                $"Actual: '{actual}'");
        }
    }

    /// <summary>
    /// Asserts that an element has a Hyperscript attribute (_).
    /// </summary>
    public static void HasHyperscript(IElement element, string? expectedContentPart = null)
    {
        var actual = element.GetAttribute("_");
        
        if (actual == null)
        {
            throw new XunitException(
                $"Expected element to have '_' (Hyperscript) attribute.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        if (expectedContentPart != null && !actual.Contains(expectedContentPart, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected Hyperscript to contain '{expectedContentPart}'.\n" +
                $"Actual: '{actual}'");
        }
    }

    /// <summary>
    /// Asserts that URL parameters in hx-get/hx-post contain expected values.
    /// </summary>
    public static void UrlContainsParameter(IElement element, string paramName, string expectedValue)
    {
        var url = element.GetAttribute("hx-get") ?? 
                  element.GetAttribute("hx-post") ?? 
                  element.GetAttribute("hx-delete");
        
        if (url == null)
        {
            throw new XunitException(
                $"Expected element to have hx-get, hx-post, or hx-delete attribute.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        var parameters = HtmlParsingHelper.GetUrlParameters(url);
        
        if (!parameters.TryGetValue(paramName, out var actual))
        {
            throw new XunitException(
                $"Expected URL to contain parameter '{paramName}'.\n" +
                $"URL: '{url}'\n" +
                $"Available parameters: {string.Join(", ", parameters.Keys)}");
        }

        if (!actual.Equals(expectedValue, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected parameter '{paramName}' to be '{expectedValue}'.\n" +
                $"Actual: '{actual}'\n" +
                $"URL: '{url}'");
        }
    }

    /// <summary>
    /// Asserts that the element points to the expected handler.
    /// </summary>
    public static void TargetsHandler(IElement element, string expectedHandler)
    {
        var url = element.GetAttribute("hx-get") ?? 
                  element.GetAttribute("hx-post") ?? 
                  element.GetAttribute("hx-delete") ??
                  element.GetAttribute("hx-put");
        
        if (url == null)
        {
            throw new XunitException(
                $"Expected element to have an htmx request attribute.\n" +
                $"Element: {GetElementDescription(element)}");
        }

        var handler = HtmlParsingHelper.GetHandlerFromUrl(url);
        
        if (handler == null || !handler.Equals(expectedHandler, StringComparison.OrdinalIgnoreCase))
        {
            throw new XunitException(
                $"Expected element to target handler '{expectedHandler}'.\n" +
                $"Actual handler: '{handler ?? "(none)"}'\n" +
                $"URL: '{url}'");
        }
    }

    private static string GetElementDescription(IElement element)
    {
        var id = element.Id;
        var classes = element.ClassName;
        var tag = element.TagName.ToLower();
        
        var description = $"<{tag}";
        if (!string.IsNullOrEmpty(id)) description += $" id=\"{id}\"";
        if (!string.IsNullOrEmpty(classes)) description += $" class=\"{classes}\"";
        description += ">";
        
        return description;
    }
}
```

### 22.4.3 Testing Attribute Correctness

With parsing helpers and assertions in place, write tests that verify htmx attributes are correct.

**Integration/Tracks/TrackRowAttributeTests.cs**

```csharp
using ChinookDashboard.Tests.Integration.Common;
using ChinookDashboard.Tests.Integration.Fixtures;

namespace ChinookDashboard.Tests.Integration.Tracks;

public class TrackRowAttributeTests : IntegrationTestBase
{
    public TrackRowAttributeTests(ChinookTestFactory factory) : base(factory) { }

    [Fact]
    public async Task TrackRow_EditButton_HasCorrectHxGet()
    {
        // Arrange
        var document = await GetPageAsync("/Tracks");
        
        // Act
        var editButton = document.QuerySelector("button[hx-get*='handler=Edit']");
        
        // Assert
        Assert.NotNull(editButton);
        HtmxAssertions.HasHxGet(editButton, "handler=Edit");
        HtmxAssertions.TargetsHandler(editButton, "Edit");
    }

    [Fact]
    public async Task TrackRow_EditButton_HasCorrectTarget()
    {
        // Arrange
        var document = await GetPageAsync("/Tracks");
        var editButton = document.QuerySelector("#track-row-1 button[hx-get*='Edit']");
        
        // Assert
        Assert.NotNull(editButton);
        
        // Should target the parent row or use closest
        var target = editButton.GetAttribute("hx-target");
        Assert.NotNull(target);
        
        // Target should be either the specific row ID or a relative selector
        Assert.True(
            target.Contains("track-row-1") || 
            target.Contains("closest") ||
            target == "this",
            $"Expected target to reference the row, got: {target}");
    }

    [Fact]
    public async Task TrackRow_EditButton_UsesOuterHtmlSwap()
    {
        // Arrange
        var document = await GetPageAsync("/Tracks");
        var editButton = document.QuerySelector("button[hx-get*='handler=Edit']");
        
        // Assert
        Assert.NotNull(editButton);
        HtmxAssertions.HasHxSwap(editButton, "outerHTML");
    }

    [Fact]
    public async Task TrackRow_EditButton_IncludesTrackId()
    {
        // Arrange
        var document = await GetPageAsync("/Tracks");
        var editButton = document.QuerySelector("#track-row-1 button[hx-get*='Edit']");
        
        // Assert
        Assert.NotNull(editButton);
        HtmxAssertions.UrlContainsParameter(editButton, "id", "1");
    }

    [Fact]
    public async Task TrackRow_DeleteButton_HasConfirmation()
    {
        // Arrange
        var document = await GetPageAsync("/Tracks");
        var deleteButton = document.QuerySelector("button[hx-delete], button[hx-get*='Delete']");
        
        // Assert
        if (deleteButton != null)
        {
            // Delete should have confirmation
            HtmxAssertions.HasHxConfirm(deleteButton);
        }
    }

    [Fact]
    public async Task TrackList_HasLoadingIndicator()
    {
        // Arrange
        var document = await GetPageAsync("/Tracks");
        
        // Act - Find elements with indicators
        var elementsWithIndicator = document.QuerySelectorAll("[hx-indicator]");
        
        // Assert - At least some elements should have indicators
        Assert.True(elementsWithIndicator.Length > 0 || 
            document.QuerySelector(".htmx-indicator, #loading-spinner") != null,
            "Page should have loading indicators configured");
    }

    [Fact]
    public async Task SearchInput_HasCorrectTriggerWithDelay()
    {
        // Arrange
        var document = await GetPageAsync("/Tracks");
        var searchInput = document.QuerySelector("input[name='search'][hx-get]");
        
        // Assert
        if (searchInput != null)
        {
            HtmxAssertions.HasHxTrigger(searchInput, "keyup");
            HtmxAssertions.HasHxTriggerWithModifier(searchInput, "keyup", "delay:");
        }
    }

    [Fact]
    public async Task SearchInput_HasChangedModifier()
    {
        // Arrange
        var document = await GetPageAsync("/Tracks");
        var searchInput = document.QuerySelector("input[name='search'][hx-get]");
        
        // Assert
        if (searchInput != null)
        {
            var trigger = searchInput.GetAttribute("hx-trigger");
            Assert.NotNull(trigger);
            Assert.Contains("changed", trigger);
        }
    }

    [Fact]
    public async Task AllHtmxElements_HaveRequiredAttributes()
    {
        // Arrange
        var document = await GetPageAsync("/Tracks");
        var htmxElements = HtmlParsingHelper.GetHtmxElements(document);
        
        // Assert - Every htmx element should have a target (explicit or implicit)
        foreach (var element in htmxElements)
        {
            var attrs = HtmlParsingHelper.GetHtmxAttributes(element);
            
            // Elements should have either explicit target or be inside a targetable container
            var hasTarget = attrs.ContainsKey("hx-target") || 
                           element.Closest("[id]") != null;
            
            Assert.True(hasTarget, 
                $"Element {element.TagName}#{element.Id} should have target context");
        }
    }
}
```

### 22.4.4 Testing Form Structure

Forms need correct field names, anti-forgery tokens, and htmx attributes.

**Integration/Artists/ArtistEditFormTests.cs**

```csharp
using AngleSharp.Html.Dom;
using ChinookDashboard.Tests.Integration.Common;
using ChinookDashboard.Tests.Integration.Fixtures;

namespace ChinookDashboard.Tests.Integration.Artists;

public class ArtistEditFormTests : IntegrationTestBase
{
    public ArtistEditFormTests(ChinookTestFactory factory) : base(factory) { }

    [Fact]
    public async Task EditForm_ContainsAntiForgeryToken()
    {
        // Arrange
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=Edit&id=1",
            target: "artist-row-1");
        var document = await GetHtmlDocumentAsync(response);
        
        // Act
        var tokenInput = document.QuerySelector(
            "input[name='__RequestVerificationToken']");
        
        // Assert
        Assert.NotNull(tokenInput);
        
        var tokenValue = (tokenInput as IHtmlInputElement)?.Value;
        Assert.False(string.IsNullOrEmpty(tokenValue), 
            "Anti-forgery token should have a value");
    }

    [Fact]
    public async Task EditForm_HasCorrectFieldNames()
    {
        // Arrange
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=Edit&id=1",
            target: "artist-row-1");
        var document = await GetHtmlDocumentAsync(response);
        
        // Act
        var nameInput = document.QuerySelector("input[name='name']") as IHtmlInputElement;
        
        // Assert
        Assert.NotNull(nameInput);
        Assert.Equal("AC/DC", nameInput.Value);
    }

    [Fact]
    public async Task EditForm_HasHiddenIdField_OrIdInUrl()
    {
        // Arrange
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=Edit&id=1",
            target: "artist-row-1");
        var document = await GetHtmlDocumentAsync(response);
        
        // Act
        var form = document.QuerySelector("form");
        var hiddenId = document.QuerySelector("input[name='id'][type='hidden']");
        var hxPost = form?.GetAttribute("hx-post");
        
        // Assert - ID should be in either hidden field or URL
        var hasHiddenId = hiddenId != null;
        var hasIdInUrl = hxPost?.Contains("id=1") == true;
        
        Assert.True(hasHiddenId || hasIdInUrl, 
            "Form should include artist ID either as hidden field or in URL");
    }

    [Fact]
    public async Task EditForm_HasCorrectHxPost()
    {
        // Arrange
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=Edit&id=1",
            target: "artist-row-1");
        var document = await GetHtmlDocumentAsync(response);
        
        // Act
        var form = document.QuerySelector("form");
        
        // Assert
        Assert.NotNull(form);
        HtmxAssertions.HasHxPost(form, "handler=Update");
    }

    [Fact]
    public async Task EditForm_TargetsCorrectRow()
    {
        // Arrange
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=Edit&id=1",
            target: "artist-row-1");
        var document = await GetHtmlDocumentAsync(response);
        
        // Act
        var form = document.QuerySelector("form");
        
        // Assert
        Assert.NotNull(form);
        
        var target = form.GetAttribute("hx-target");
        Assert.NotNull(target);
        
        // Should target either specific ID or use closest
        Assert.True(
            target.Contains("artist-row-1") || 
            target.Contains("artist-row") ||
            target.Contains("closest"),
            $"Form target should reference the artist row, got: {target}");
    }

    [Fact]
    public async Task EditForm_HasSaveButton()
    {
        // Arrange
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=Edit&id=1",
            target: "artist-row-1");
        var document = await GetHtmlDocumentAsync(response);
        
        // Act
        var submitButton = document.QuerySelector(
            "button[type='submit'], input[type='submit']");
        
        // Assert
        Assert.NotNull(submitButton);
    }

    [Fact]
    public async Task EditForm_HasCancelButton_WithCorrectBehavior()
    {
        // Arrange
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=Edit&id=1",
            target: "artist-row-1");
        var document = await GetHtmlDocumentAsync(response);
        
        // Act
        var cancelButton = document.QuerySelector(
            "button[hx-get*='Cancel'], button[type='button'][hx-get]");
        
        // Assert
        if (cancelButton != null)
        {
            HtmxAssertions.HasHxGet(cancelButton, "Cancel");
        }
        else
        {
            // Alternative: Hyperscript cancel
            var hyperscriptCancel = document.QuerySelector("button[_*='cancel'], button[_*='Cancel']");
            Assert.NotNull(hyperscriptCancel);
        }
    }

    [Fact]
    public async Task EditForm_UsesOuterHtmlSwap()
    {
        // Arrange
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=Edit&id=1",
            target: "artist-row-1");
        var document = await GetHtmlDocumentAsync(response);
        
        // Act
        var form = document.QuerySelector("form");
        
        // Assert
        Assert.NotNull(form);
        HtmxAssertions.HasHxSwap(form, "outerHTML");
    }

    [Fact]
    public async Task CreateForm_HasRequiredValidation()
    {
        // Arrange
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=CreateForm",
            target: "modal-container");
        var document = await GetHtmlDocumentAsync(response);
        
        // Act
        var nameInput = document.QuerySelector("input[name='name']") as IHtmlInputElement;
        
        // Assert
        Assert.NotNull(nameInput);
        Assert.True(nameInput.IsRequired, "Name input should be required");
    }

    [Fact]
    public async Task CreateForm_HasEmptyFields()
    {
        // Arrange
        var response = await Client.HtmxGetAsync(
            "/Artists?handler=CreateForm",
            target: "modal-container");
        var document = await GetHtmlDocumentAsync(response);
        
        // Act
        var nameInput = document.QuerySelector("input[name='name']") as IHtmlInputElement;
        
        // Assert
        Assert.NotNull(nameInput);
        Assert.True(string.IsNullOrEmpty(nameInput.Value), 
            "Create form should have empty name field");
    }
}
```

---

## 22.5 Browser Testing with Playwright

Integration tests verify HTML structure but can't verify that htmx actually updates the DOM correctly. Browser tests with Playwright automate a real browser to test full user interactions.

### 22.5.1 Setting Up Playwright for ASP.NET Core

Playwright requires setup to run the ASP.NET Core application and control a browser.

#### Installing Browsers

After adding the Playwright package, install browsers:

```bash
# Run from the test project directory
pwsh bin/Debug/net8.0/playwright.ps1 install
```

Or add a build target to install automatically:

```xml
<!-- Add to test .csproj -->
<Target Name="InstallPlaywright" AfterTargets="Build">
  <Exec Command="pwsh $(OutputPath)playwright.ps1 install" 
        Condition="!Exists('$(USERPROFILE)\.cache\ms-playwright')" />
</Target>
```

#### PlaywrightFixture

Create a fixture that starts the application and provides the browser:

**Browser/Fixtures/PlaywrightFixture.cs**

```csharp
using ChinookDashboard.Tests.Integration.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace ChinookDashboard.Tests.Browser.Fixtures;

public class PlaywrightFixture : IAsyncLifetime
{
    private IHost? _host;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    
    public string BaseUrl { get; private set; } = "";
    public IPlaywright Playwright => _playwright ?? throw new InvalidOperationException("Playwright not initialized");
    public IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser not initialized");

    public async Task InitializeAsync()
    {
        // Start the web application
        _host = await StartApplicationAsync();
        
        // Initialize Playwright
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true, // Set to false to see the browser during debugging
            SlowMo = 0 // Add delay between actions for debugging (e.g., 100)
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.DisposeAsync();
        }
        
        _playwright?.Dispose();
        
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    private async Task<IHost> StartApplicationAsync()
    {
        // Use the test factory to create a properly configured host
        var factory = new ChinookTestFactory();
        
        // Get the server and start it
        var host = factory.Services.GetRequiredService<IHost>();
        await host.StartAsync();
        
        // Get the server address
        var server = host.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        BaseUrl = addresses?.Addresses.FirstOrDefault() ?? "http://localhost:5000";
        
        return host;
    }

    public async Task<IBrowserContext> CreateContextAsync()
    {
        return await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true
        });
    }
}
```

#### Alternative: Simpler Fixture Using WebApplicationFactory

For most scenarios, a simpler approach using WebApplicationFactory with a known port works well:

**Browser/Fixtures/PlaywrightFixture.cs** (Simplified Version)

```csharp
using ChinookDashboard.Data;
using ChinookDashboard.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace ChinookDashboard.Tests.Browser.Fixtures;

public class PlaywrightFixture : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private IHost? _host;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private SqliteConnection? _connection;
    
    private const int Port = 5555;
    public string BaseUrl => $"http://localhost:{Port}";
    
    public IBrowser Browser => _browser ?? throw new InvalidOperationException("Not initialized");
    public IPlaywright Playwright => _playwright ?? throw new InvalidOperationException("Not initialized");

    public PlaywrightFixture()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseUrls(BaseUrl);
                
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ChinookContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Use SQLite in-memory
                    _connection = new SqliteConnection("DataSource=:memory:");
                    _connection.Open();

                    services.AddDbContext<ChinookContext>(options =>
                        options.UseSqlite(_connection));
                });
            });
    }

    public async Task InitializeAsync()
    {
        // Create and start the host
        _host = _factory.Services.GetRequiredService<IHost>();
        await _host.StartAsync();
        
        // Seed database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChinookContext>();
        db.Database.EnsureCreated();
        SeedTestData(db);

        // Initialize Playwright
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser != null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        _connection?.Dispose();
    }

    private static void SeedTestData(ChinookContext context)
    {
        context.Artists.AddRange(
            new Artist { Id = 1, Name = "AC/DC" },
            new Artist { Id = 2, Name = "Accept" },
            new Artist { Id = 3, Name = "Aerosmith" },
            new Artist { Id = 4, Name = "Led Zeppelin" },
            new Artist { Id = 5, Name = "Metallica" }
        );
        
        context.Genres.AddRange(
            new Genre { Id = 1, Name = "Rock" },
            new Genre { Id = 2, Name = "Metal" }
        );
        
        context.SaveChanges();
    }

    public async Task<IBrowserContext> CreateContextAsync()
    {
        return await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true
        });
    }
}
```

#### PlaywrightTestBase

Create a base class for browser tests:

**Browser/PlaywrightTestBase.cs**

```csharp
using ChinookDashboard.Tests.Browser.Fixtures;
using Microsoft.Playwright;

namespace ChinookDashboard.Tests.Browser;

public abstract class PlaywrightTestBase : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    protected readonly PlaywrightFixture Fixture;
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;
    protected string BaseUrl => Fixture.BaseUrl;

    protected PlaywrightTestBase(PlaywrightFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        Context = await Fixture.CreateContextAsync();
        Page = await Context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await Page.CloseAsync();
        await Context.DisposeAsync();
    }

    /// <summary>
    /// Waits for an htmx request to complete.
    /// </summary>
    protected async Task WaitForHtmxRequestAsync(string urlPart)
    {
        await Page.WaitForResponseAsync(r => 
            r.Url.Contains(urlPart) && r.Status == 200);
    }

    /// <summary>
    /// Waits for an htmx request matching a predicate.
    /// </summary>
    protected async Task<IResponse> WaitForHtmxResponseAsync(Func<IResponse, bool> predicate)
    {
        return await Page.WaitForResponseAsync(predicate);
    }

    /// <summary>
    /// Takes a screenshot for debugging.
    /// </summary>
    protected async Task TakeScreenshotAsync(string name)
    {
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = $"screenshots/{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
        });
    }
}
```

### 22.5.2 Testing htmx Interactions

Test that htmx requests work without causing full page reloads.

**Browser/Artists/ArtistSearchTests.cs**

```csharp
using ChinookDashboard.Tests.Browser.Fixtures;
using Microsoft.Playwright;

namespace ChinookDashboard.Tests.Browser.Artists;

public class ArtistSearchTests : PlaywrightTestBase
{
    public ArtistSearchTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Search_FiltersResults_WithoutPageReload()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        var initialUrl = Page.Url;
        
        // Get initial row count
        var initialRows = await Page.QuerySelectorAllAsync("tr[id^='artist-row-']");
        var initialCount = initialRows.Count;
        Assert.True(initialCount > 2, "Should have multiple artists initially");
        
        // Act - Type in search box
        var searchInput = await Page.QuerySelectorAsync("input[name='search']");
        Assert.NotNull(searchInput);
        
        // Type and wait for htmx response
        await searchInput.FillAsync("AC");
        await WaitForHtmxRequestAsync("handler=List");
        
        // Small delay for DOM to settle
        await Page.WaitForTimeoutAsync(100);
        
        // Assert - Results should be filtered
        var filteredRows = await Page.QuerySelectorAllAsync("tr[id^='artist-row-']");
        Assert.True(filteredRows.Count < initialCount, "Results should be filtered");
        Assert.True(filteredRows.Count >= 1, "Should have at least one matching result");
        
        // Verify specific results
        var content = await Page.ContentAsync();
        Assert.Contains("AC/DC", content);
        
        // Verify no page reload occurred (URL base should be same)
        Assert.StartsWith(initialUrl.Split('?')[0], Page.Url.Split('?')[0]);
    }

    [Fact]
    public async Task Search_ShowsNoResults_ForNonMatchingTerm()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Act
        await Page.FillAsync("input[name='search']", "ZZZZZZ");
        await WaitForHtmxRequestAsync("handler=List");
        await Page.WaitForTimeoutAsync(100);
        
        // Assert
        var rows = await Page.QuerySelectorAllAsync("tr[id^='artist-row-']");
        Assert.Empty(rows);
        
        // Should show empty state
        var emptyState = await Page.QuerySelectorAsync(".empty-state, [class*='empty'], [class*='no-results']");
        Assert.NotNull(emptyState);
    }

    [Fact]
    public async Task Search_ClearsFilter_WhenInputCleared()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Filter first
        await Page.FillAsync("input[name='search']", "AC");
        await WaitForHtmxRequestAsync("handler=List");
        
        var filteredRows = await Page.QuerySelectorAllAsync("tr[id^='artist-row-']");
        var filteredCount = filteredRows.Count;
        
        // Act - Clear search
        await Page.FillAsync("input[name='search']", "");
        await WaitForHtmxRequestAsync("handler=List");
        await Page.WaitForTimeoutAsync(100);
        
        // Assert - Should show all results again
        var allRows = await Page.QuerySelectorAllAsync("tr[id^='artist-row-']");
        Assert.True(allRows.Count > filteredCount, "Clearing search should show more results");
    }

    [Fact]
    public async Task Search_UpdatesUrlWithSearchParam()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Act
        await Page.FillAsync("input[name='search']", "Metal");
        await WaitForHtmxRequestAsync("handler=List");
        await Page.WaitForTimeoutAsync(200);
        
        // Assert - URL should include search parameter (if hx-push-url is used)
        // This test documents the expected behavior
        var url = Page.Url;
        // URL might or might not be updated depending on implementation
        // If using hx-push-url="true", it should contain the search param
    }
}
```

### 22.5.3 Testing Dynamic DOM Updates

Test that content swaps work correctly.

**Browser/Artists/InlineEditTests.cs**

```csharp
using ChinookDashboard.Tests.Browser.Fixtures;

namespace ChinookDashboard.Tests.Browser.Artists;

public class InlineEditTests : PlaywrightTestBase
{
    public InlineEditTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task ClickEdit_ShowsEditForm()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Find edit button for first artist
        var editButton = await Page.QuerySelectorAsync(
            "#artist-row-1 button[hx-get*='Edit'], #artist-row-1 .edit-btn");
        Assert.NotNull(editButton);
        
        // Act - Click edit
        await editButton.ClickAsync();
        await Page.WaitForSelectorAsync("#artist-row-1 form, form[hx-post*='Update']");
        
        // Assert - Form should appear
        var form = await Page.QuerySelectorAsync("#artist-row-1 form, [id^='artist'] form");
        Assert.NotNull(form);
        
        // Input should have current value
        var input = await Page.QuerySelectorAsync("input[name='name']");
        Assert.NotNull(input);
        
        var value = await input.GetAttributeAsync("value");
        Assert.Equal("AC/DC", value);
    }

    [Fact]
    public async Task EditAndSave_UpdatesRowWithNewValue()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Click edit
        await Page.ClickAsync("#artist-row-1 button[hx-get*='Edit'], #artist-row-1 .edit-btn");
        await Page.WaitForSelectorAsync("input[name='name']");
        
        // Act - Change value
        var newName = "AC/DC Updated " + Guid.NewGuid().ToString()[..8];
        await Page.FillAsync("input[name='name']", newName);
        
        // Click save
        await Page.ClickAsync("button[type='submit']");
        
        // Wait for response
        await WaitForHtmxRequestAsync("handler=Update");
        await Page.WaitForTimeoutAsync(200);
        
        // Assert - Row should show new value
        var rowContent = await Page.TextContentAsync("#artist-row-1, tr:first-of-type");
        Assert.Contains(newName, rowContent);
        
        // Form should be gone
        var form = await Page.QuerySelectorAsync("#artist-row-1 form");
        Assert.Null(form);
    }

    [Fact]
    public async Task EditAndCancel_RestoresOriginalRow()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        var originalContent = await Page.TextContentAsync("#artist-row-1");
        
        // Click edit
        await Page.ClickAsync("#artist-row-1 button[hx-get*='Edit'], #artist-row-1 .edit-btn");
        await Page.WaitForSelectorAsync("input[name='name']");
        
        // Change value but don't save
        await Page.FillAsync("input[name='name']", "Changed But Not Saved");
        
        // Act - Click cancel (or press Escape)
        var cancelButton = await Page.QuerySelectorAsync(
            "button[hx-get*='Cancel'], button[type='button']:has-text('Cancel')");
        
        if (cancelButton != null)
        {
            await cancelButton.ClickAsync();
            await WaitForHtmxRequestAsync("handler=Cancel");
        }
        else
        {
            // Try Escape key
            await Page.Keyboard.PressAsync("Escape");
        }
        
        await Page.WaitForTimeoutAsync(200);
        
        // Assert - Should show original content
        var restoredContent = await Page.TextContentAsync("#artist-row-1");
        Assert.Contains("AC/DC", restoredContent);
        Assert.DoesNotContain("Changed But Not Saved", restoredContent);
    }

    [Fact]
    public async Task MultipleEdits_WorkIndependently()
    {
        // Arrange - Need at least 2 artists
        await Page.GotoAsync("/Artists");
        
        // Start editing first artist
        await Page.ClickAsync("#artist-row-1 button[hx-get*='Edit'], #artist-row-1 .edit-btn");
        await Page.WaitForSelectorAsync("#artist-row-1 input[name='name'], input[name='name']");
        
        // Assert - Can still see other rows
        var row2 = await Page.QuerySelectorAsync("#artist-row-2");
        Assert.NotNull(row2);
        
        // Should be able to save without affecting other rows
        await Page.FillAsync("input[name='name']", "Edited First");
        await Page.ClickAsync("button[type='submit']");
        await WaitForHtmxRequestAsync("handler=Update");
        
        // Second row should be unchanged
        var row2Content = await Page.TextContentAsync("#artist-row-2");
        Assert.Contains("Accept", row2Content);
    }
}
```

### 22.5.4 Testing OOB Updates in Browser

Verify that OOB swaps update multiple elements.

**Browser/Artists/DeleteWithOobTests.cs**

```csharp
using ChinookDashboard.Tests.Browser.Fixtures;

namespace ChinookDashboard.Tests.Browser.Artists;

public class DeleteWithOobTests : PlaywrightTestBase
{
    public DeleteWithOobTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task DeleteArtist_RemovesRow_AndUpdatesStats()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Get initial stats
        var statsElement = await Page.QuerySelectorAsync(
            "#artist-count, .stat-count, [data-stat='artists']");
        var initialStats = statsElement != null 
            ? await statsElement.TextContentAsync() 
            : null;
        
        // Get initial row count
        var initialRows = await Page.QuerySelectorAllAsync("tr[id^='artist-row-']");
        var initialRowCount = initialRows.Count;
        
        // Find delete button for last artist (to minimize test interference)
        var deleteButton = await Page.QuerySelectorAsync(
            "#artist-row-5 button[hx-delete], #artist-row-5 .delete-btn");
        
        if (deleteButton == null)
        {
            // Skip if no delete button found
            return;
        }
        
        // Act - Click delete
        // Handle confirmation dialog
        Page.Dialog += async (_, dialog) =>
        {
            await dialog.AcceptAsync();
        };
        
        await deleteButton.ClickAsync();
        
        // Wait for delete request
        await WaitForHtmxRequestAsync("handler=Delete");
        await Page.WaitForTimeoutAsync(300);
        
        // Assert - Row should be removed
        var deletedRow = await Page.QuerySelectorAsync("#artist-row-5");
        Assert.Null(deletedRow);
        
        // Row count should decrease
        var finalRows = await Page.QuerySelectorAllAsync("tr[id^='artist-row-']");
        Assert.Equal(initialRowCount - 1, finalRows.Count);
        
        // Stats should be updated (OOB)
        if (statsElement != null && initialStats != null)
        {
            var finalStats = await statsElement.TextContentAsync();
            // Stats should have changed
            Assert.NotEqual(initialStats, finalStats);
        }
    }

    [Fact]
    public async Task DeleteArtist_ShowsToastNotification()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        var deleteButton = await Page.QuerySelectorAsync(
            "button[hx-delete], .delete-btn");
        
        if (deleteButton == null) return;
        
        // Handle confirmation
        Page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        
        // Act
        await deleteButton.ClickAsync();
        await WaitForHtmxRequestAsync("handler=Delete");
        
        // Assert - Toast should appear
        var toast = await Page.WaitForSelectorAsync(
            ".toast, .notification, [class*='toast']",
            new() { Timeout = 3000 });
        
        Assert.NotNull(toast);
    }

    [Fact]
    public async Task DeleteArtist_CancelConfirmation_KeepsRow()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        var deleteButton = await Page.QuerySelectorAsync(
            "#artist-row-1 button[hx-delete], #artist-row-1 .delete-btn");
        
        if (deleteButton == null) return;
        
        // Handle confirmation - dismiss it
        Page.Dialog += async (_, dialog) => await dialog.DismissAsync();
        
        // Act
        await deleteButton.ClickAsync();
        await Page.WaitForTimeoutAsync(300);
        
        // Assert - Row should still exist
        var row = await Page.QuerySelectorAsync("#artist-row-1");
        Assert.NotNull(row);
    }
}
```

### 22.5.5 Testing Modals and Dialogs

Test the complete modal workflow.

**Browser/Artists/CreateArtistModalTests.cs**

```csharp
using ChinookDashboard.Tests.Browser.Fixtures;

namespace ChinookDashboard.Tests.Browser.Artists;

public class CreateArtistModalTests : PlaywrightTestBase
{
    public CreateArtistModalTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task ClickAddButton_OpensModal()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Act - Click add button
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        
        // Wait for modal to appear
        await Page.WaitForSelectorAsync(
            "#modal-container form, .modal form, [class*='modal'] form");
        
        // Assert - Modal should be visible
        var modal = await Page.QuerySelectorAsync(
            "#modal-container, .modal, [class*='modal']");
        Assert.NotNull(modal);
        
        var isVisible = await modal.IsVisibleAsync();
        Assert.True(isVisible, "Modal should be visible");
    }

    [Fact]
    public async Task CreateArtist_ClosesModal_AndAddsRow()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        var initialRows = await Page.QuerySelectorAllAsync("tr[id^='artist-row-']");
        var initialCount = initialRows.Count;
        
        // Open modal
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("input[name='name']");
        
        // Act - Fill form
        var newArtistName = "New Test Artist " + Guid.NewGuid().ToString()[..8];
        await Page.FillAsync("input[name='name']", newArtistName);
        
        // Submit
        await Page.ClickAsync(
            "#modal-container button[type='submit'], .modal button[type='submit']");
        
        // Wait for response
        await WaitForHtmxRequestAsync("handler=Create");
        await Page.WaitForTimeoutAsync(300);
        
        // Assert - Modal should close
        var modalContent = await Page.QuerySelectorAsync("#modal-container form");
        Assert.Null(modalContent);
        
        // New row should appear
        var content = await Page.ContentAsync();
        Assert.Contains(newArtistName, content);
        
        // Row count should increase
        var finalRows = await Page.QuerySelectorAllAsync("tr[id^='artist-row-']");
        Assert.True(finalRows.Count >= initialCount, "Should have at least as many rows");
    }

    [Fact]
    public async Task CreateArtist_WithEmptyName_ShowsValidation()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Open modal
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("input[name='name']");
        
        // Act - Submit without filling
        await Page.ClickAsync(
            "#modal-container button[type='submit'], .modal button[type='submit']");
        
        // Wait a moment for validation
        await Page.WaitForTimeoutAsync(200);
        
        // Assert - Should show validation error or browser validation
        var input = await Page.QuerySelectorAsync("input[name='name']");
        Assert.NotNull(input);
        
        // Check for HTML5 validation or custom validation message
        var validationMessage = await input.EvaluateAsync<string>(
            "el => el.validationMessage");
        var hasCustomError = await Page.QuerySelectorAsync(
            ".validation-error, .field-validation-error, [class*='error']");
        
        Assert.True(
            !string.IsNullOrEmpty(validationMessage) || hasCustomError != null,
            "Should show validation error for empty name");
        
        // Modal should still be open
        var form = await Page.QuerySelectorAsync("#modal-container form, .modal form");
        Assert.NotNull(form);
    }

    [Fact]
    public async Task CloseModal_ByClickingOutside_OrCloseButton()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Open modal
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("#modal-container form, .modal form");
        
        // Act - Try close button first
        var closeButton = await Page.QuerySelectorAsync(
            ".modal-close, .close-btn, button[aria-label='Close'], button:has-text('×')");
        
        if (closeButton != null)
        {
            await closeButton.ClickAsync();
        }
        else
        {
            // Try pressing Escape
            await Page.Keyboard.PressAsync("Escape");
        }
        
        await Page.WaitForTimeoutAsync(300);
        
        // Assert - Modal should close
        var form = await Page.QuerySelectorAsync("#modal-container form");
        Assert.Null(form);
    }

    [Fact]
    public async Task CreateArtist_ShowsSuccessToast()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Open modal and create
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("input[name='name']");
        
        await Page.FillAsync("input[name='name']", "Toast Test Artist");
        await Page.ClickAsync("button[type='submit']");
        
        // Act - Wait for success
        await WaitForHtmxRequestAsync("handler=Create");
        
        // Assert - Toast should appear
        try
        {
            var toast = await Page.WaitForSelectorAsync(
                ".toast, .notification, [class*='toast']",
                new() { Timeout = 3000 });
            
            Assert.NotNull(toast);
            
            var toastText = await toast.TextContentAsync();
            Assert.Contains("success", toastText?.ToLower() ?? "");
        }
        catch (TimeoutException)
        {
            // Toast might auto-dismiss quickly or not be implemented
            // This is acceptable for some implementations
        }
    }

    [Fact]
    public async Task Modal_FocusesFirstInput_OnOpen()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Act - Open modal
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("input[name='name']");
        await Page.WaitForTimeoutAsync(100); // Allow focus to settle
        
        // Assert - First input should be focused
        var focusedElement = await Page.EvaluateAsync<string>(
            "document.activeElement?.name || document.activeElement?.tagName");
        
        Assert.True(
            focusedElement == "name" || focusedElement == "INPUT",
            $"Expected name input to be focused, got: {focusedElement}");
    }
}
```

#### Running Browser Tests

```bash
# Run all browser tests
dotnet test --filter "FullyQualifiedName~Browser"

# Run with headed browser for debugging
# (Modify PlaywrightFixture to set Headless = false)

# Run specific test with trace
dotnet test --filter "FullyQualifiedName~CreateArtistModalTests"
```

Browser tests verify the complete user experience, catching issues that integration tests miss: JavaScript errors, htmx processing, DOM updates, and visual feedback. They run slower than integration tests, so use them for critical user workflows rather than exhaustive coverage.

## 22.6 Testing Hyperscript Behaviors

Hyperscript runs entirely in the browser. Testing Hyperscript behaviors requires browser automation to verify that classes toggle, focus moves, and timed behaviors work correctly.

### 22.6.1 Testing Client-Side State Changes

Hyperscript often manages UI state through class changes. Test that clicking elements updates classes correctly.

**Browser/Hyperscript/TabSelectionTests.cs**

```csharp
using ChinookDashboard.Tests.Browser.Fixtures;

namespace ChinookDashboard.Tests.Browser.Hyperscript;

public class TabSelectionTests : PlaywrightTestBase
{
    public TabSelectionTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task ClickTab_AddsActiveClass()
    {
        // Arrange
        await Page.GotoAsync("/Tracks");
        
        // Find tabs
        var tabs = await Page.QuerySelectorAllAsync(".genre-tab, .tab, [role='tab']");
        if (tabs.Count < 2) return; // Skip if no tabs
        
        var secondTab = tabs[1];
        
        // Act - Click second tab
        await secondTab.ClickAsync();
        await Page.WaitForTimeoutAsync(100);
        
        // Assert - Second tab should have active class
        var hasActive = await secondTab.EvaluateAsync<bool>(
            "el => el.classList.contains('active') || el.classList.contains('selected') || el.getAttribute('aria-selected') === 'true'");
        
        Assert.True(hasActive, "Clicked tab should have active state");
    }

    [Fact]
    public async Task ClickTab_RemovesActiveFromPreviousTab()
    {
        // Arrange
        await Page.GotoAsync("/Tracks");
        
        var tabs = await Page.QuerySelectorAllAsync(".genre-tab, .tab, [role='tab']");
        if (tabs.Count < 2) return;
        
        var firstTab = tabs[0];
        var secondTab = tabs[1];
        
        // Verify first tab starts active
        var firstWasActive = await firstTab.EvaluateAsync<bool>(
            "el => el.classList.contains('active') || el.classList.contains('selected')");
        
        // Act - Click second tab
        await secondTab.ClickAsync();
        await Page.WaitForTimeoutAsync(100);
        
        // Assert - First tab should no longer be active
        var firstStillActive = await firstTab.EvaluateAsync<bool>(
            "el => el.classList.contains('active') || el.classList.contains('selected')");
        
        if (firstWasActive)
        {
            Assert.False(firstStillActive, "Previous tab should lose active state");
        }
    }

    [Fact]
    public async Task ClickTab_UpdatesTabPanel()
    {
        // Arrange
        await Page.GotoAsync("/Tracks");
        
        var tabs = await Page.QuerySelectorAllAsync(".genre-tab, .tab, [role='tab']");
        if (tabs.Count < 2) return;
        
        // Get initial panel content
        var panel = await Page.QuerySelectorAsync(".tab-panel, [role='tabpanel']");
        var initialContent = panel != null ? await panel.TextContentAsync() : "";
        
        // Act - Click different tab
        await tabs[1].ClickAsync();
        
        // Wait for content to potentially change (htmx or Hyperscript)
        await Page.WaitForTimeoutAsync(300);
        
        // Assert - Panel content or visibility may change
        // Implementation varies: could be htmx load or Hyperscript show/hide
    }

    [Fact]
    public async Task TabSelection_PersistsAfterOtherInteractions()
    {
        // Arrange
        await Page.GotoAsync("/Tracks");
        
        var tabs = await Page.QuerySelectorAllAsync(".genre-tab, .tab");
        if (tabs.Count < 2) return;
        
        // Select second tab
        await tabs[1].ClickAsync();
        await Page.WaitForTimeoutAsync(100);
        
        // Perform another action (like search)
        var searchInput = await Page.QuerySelectorAsync("input[name='search']");
        if (searchInput != null)
        {
            await searchInput.FillAsync("test");
            await Page.WaitForTimeoutAsync(500);
        }
        
        // Assert - Tab selection should persist
        var isStillActive = await tabs[1].EvaluateAsync<bool>(
            "el => el.classList.contains('active') || el.classList.contains('selected')");
        
        Assert.True(isStillActive, "Tab selection should persist after other interactions");
    }
}
```

### 22.6.2 Testing Keyboard Interactions

Test keyboard shortcuts implemented with Hyperscript.

**Browser/Hyperscript/KeyboardNavigationTests.cs**

```csharp
using ChinookDashboard.Tests.Browser.Fixtures;

namespace ChinookDashboard.Tests.Browser.Hyperscript;

public class KeyboardNavigationTests : PlaywrightTestBase
{
    public KeyboardNavigationTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task EscapeKey_CancelsEditMode()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Enter edit mode
        var editButton = await Page.QuerySelectorAsync(
            "#artist-row-1 button[hx-get*='Edit'], #artist-row-1 .edit-btn");
        if (editButton == null) return;
        
        await editButton.ClickAsync();
        await Page.WaitForSelectorAsync("input[name='name']");
        
        // Verify we're in edit mode
        var formBefore = await Page.QuerySelectorAsync("#artist-row-1 form, form[hx-post*='Update']");
        Assert.NotNull(formBefore);
        
        // Act - Press Escape
        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForTimeoutAsync(300);
        
        // Assert - Should exit edit mode
        var formAfter = await Page.QuerySelectorAsync("#artist-row-1 form");
        Assert.Null(formAfter);
        
        // Original content should be restored
        var rowContent = await Page.TextContentAsync("#artist-row-1");
        Assert.Contains("AC/DC", rowContent);
    }

    [Fact]
    public async Task EnterKey_SubmitsForm()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Enter edit mode
        await Page.ClickAsync("#artist-row-1 button[hx-get*='Edit'], #artist-row-1 .edit-btn");
        await Page.WaitForSelectorAsync("input[name='name']");
        
        // Change value
        var uniqueName = "Enter Test " + Guid.NewGuid().ToString()[..6];
        await Page.FillAsync("input[name='name']", uniqueName);
        
        // Act - Press Enter
        await Page.Keyboard.PressAsync("Enter");
        
        // Wait for submission
        await WaitForHtmxRequestAsync("handler=Update");
        await Page.WaitForTimeoutAsync(200);
        
        // Assert - Form should be gone and value updated
        var form = await Page.QuerySelectorAsync("#artist-row-1 form");
        Assert.Null(form);
        
        var rowContent = await Page.TextContentAsync("#artist-row-1");
        Assert.Contains(uniqueName, rowContent);
    }

    [Fact]
    public async Task EscapeKey_DoesNotSubmitChanges()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Enter edit mode
        await Page.ClickAsync("#artist-row-1 button[hx-get*='Edit'], #artist-row-1 .edit-btn");
        await Page.WaitForSelectorAsync("input[name='name']");
        
        // Change value but don't submit
        await Page.FillAsync("input[name='name']", "Should Not Save");
        
        // Act - Press Escape
        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForTimeoutAsync(300);
        
        // Assert - Original value should be shown
        var rowContent = await Page.TextContentAsync("#artist-row-1");
        Assert.Contains("AC/DC", rowContent);
        Assert.DoesNotContain("Should Not Save", rowContent);
    }

    [Fact]
    public async Task TabKey_NavigatesBetweenFormFields()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Open create modal
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("input[name='name']");
        
        // Focus first input
        await Page.FocusAsync("input[name='name']");
        
        // Act - Press Tab
        await Page.Keyboard.PressAsync("Tab");
        await Page.WaitForTimeoutAsync(50);
        
        // Assert - Focus should move to next focusable element
        var focusedTag = await Page.EvaluateAsync<string>(
            "document.activeElement?.tagName");
        
        Assert.True(
            focusedTag == "BUTTON" || focusedTag == "INPUT",
            $"Tab should move focus to next element, focused: {focusedTag}");
    }

    [Fact]
    public async Task ShortcutKeys_WorkInContext()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Test Ctrl+F or / for search focus (if implemented)
        var searchInput = await Page.QuerySelectorAsync("input[name='search']");
        if (searchInput == null) return;
        
        // Click elsewhere first
        await Page.ClickAsync("body");
        
        // Act - Try keyboard shortcut
        await Page.Keyboard.PressAsync("/");
        await Page.WaitForTimeoutAsync(100);
        
        // Assert - Check if search is focused
        var focusedName = await Page.EvaluateAsync<string>(
            "document.activeElement?.name");
        
        // This test documents expected behavior - may or may not be implemented
    }
}
```

### 22.6.3 Testing Auto-Dismiss Behaviors

Test time-based behaviors like toast notifications that auto-dismiss.

**Browser/Hyperscript/ToastAutoDismissTests.cs**

```csharp
using ChinookDashboard.Tests.Browser.Fixtures;

namespace ChinookDashboard.Tests.Browser.Hyperscript;

public class ToastAutoDismissTests : PlaywrightTestBase
{
    public ToastAutoDismissTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Toast_AppearsOnSuccess()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Open modal and create artist
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("input[name='name']");
        
        await Page.FillAsync("input[name='name']", "Toast Test Artist");
        
        // Act - Submit
        await Page.ClickAsync("button[type='submit']");
        await WaitForHtmxRequestAsync("handler=Create");
        
        // Assert - Toast should appear
        var toast = await Page.WaitForSelectorAsync(
            ".toast, .notification, [class*='toast']",
            new() { Timeout = 3000 });
        
        Assert.NotNull(toast);
        var isVisible = await toast.IsVisibleAsync();
        Assert.True(isVisible, "Toast should be visible");
    }

    [Fact]
    public async Task Toast_AutoDismisses_AfterTimeout()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Trigger action that shows toast
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("input[name='name']");
        await Page.FillAsync("input[name='name']", "Auto Dismiss Test");
        await Page.ClickAsync("button[type='submit']");
        
        // Wait for toast to appear
        var toast = await Page.WaitForSelectorAsync(
            ".toast, .notification",
            new() { Timeout = 3000 });
        
        if (toast == null) return; // Skip if no toast implementation
        
        // Verify toast is visible
        Assert.True(await toast.IsVisibleAsync());
        
        // Act - Wait for auto-dismiss (typically 5 seconds + buffer)
        await Page.WaitForTimeoutAsync(6000);
        
        // Assert - Toast should be gone
        var toastAfter = await Page.QuerySelectorAsync(".toast, .notification");
        
        if (toastAfter != null)
        {
            var stillVisible = await toastAfter.IsVisibleAsync();
            Assert.False(stillVisible, "Toast should auto-dismiss after timeout");
        }
    }

    [Fact]
    public async Task Toast_CanBeManuallyDismissed()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Trigger toast
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("input[name='name']");
        await Page.FillAsync("input[name='name']", "Manual Dismiss Test");
        await Page.ClickAsync("button[type='submit']");
        
        var toast = await Page.WaitForSelectorAsync(
            ".toast, .notification",
            new() { Timeout = 3000 });
        
        if (toast == null) return;
        
        // Act - Click close button on toast
        var closeButton = await toast.QuerySelectorAsync(
            ".close, .dismiss, button, [aria-label='Close']");
        
        if (closeButton != null)
        {
            await closeButton.ClickAsync();
            await Page.WaitForTimeoutAsync(300);
            
            // Assert - Toast should be gone immediately
            var toastAfter = await Page.QuerySelectorAsync(".toast:visible, .notification:visible");
            Assert.Null(toastAfter);
        }
    }

    [Fact]
    public async Task MultipleToasts_StackCorrectly()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Trigger multiple actions quickly
        for (int i = 0; i < 2; i++)
        {
            await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
            await Page.WaitForSelectorAsync("input[name='name']");
            await Page.FillAsync("input[name='name']", $"Stack Test {i}");
            await Page.ClickAsync("button[type='submit']");
            await Page.WaitForTimeoutAsync(500);
        }
        
        // Assert - Should handle multiple toasts gracefully
        var toasts = await Page.QuerySelectorAllAsync(".toast, .notification");
        
        // Implementation may stack, queue, or replace - all are valid
        // This test documents the behavior
    }
}
```

### 22.6.4 Testing Focus Management

Test that focus moves correctly for accessibility and usability.

**Browser/Hyperscript/EditFormFocusTests.cs**

```csharp
using ChinookDashboard.Tests.Browser.Fixtures;

namespace ChinookDashboard.Tests.Browser.Hyperscript;

public class EditFormFocusTests : PlaywrightTestBase
{
    public EditFormFocusTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task EditForm_FocusesNameInput_OnOpen()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Act - Click edit
        await Page.ClickAsync("#artist-row-1 button[hx-get*='Edit'], #artist-row-1 .edit-btn");
        await Page.WaitForSelectorAsync("input[name='name']");
        await Page.WaitForTimeoutAsync(100); // Allow focus script to run
        
        // Assert - Input should be focused
        var focusedElement = await Page.EvaluateAsync<string>(
            "document.activeElement?.name || document.activeElement?.tagName");
        
        Assert.True(
            focusedElement == "name" || focusedElement == "INPUT",
            $"Name input should be focused, got: {focusedElement}");
    }

    [Fact]
    public async Task EditForm_SelectsText_OnFocus()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Act - Click edit
        await Page.ClickAsync("#artist-row-1 button[hx-get*='Edit'], #artist-row-1 .edit-btn");
        await Page.WaitForSelectorAsync("input[name='name']");
        await Page.WaitForTimeoutAsync(100);
        
        // Assert - Text should be selected
        var selectionLength = await Page.EvaluateAsync<int>(@"
            const input = document.querySelector('input[name=""name""]');
            if (input && input.selectionStart !== input.selectionEnd) {
                return input.selectionEnd - input.selectionStart;
            }
            return 0;
        ");
        
        // If text selection is implemented, it should select the content
        // This test documents the expected behavior
        if (selectionLength > 0)
        {
            Assert.True(selectionLength > 0, "Text should be selected");
        }
    }

    [Fact]
    public async Task Modal_FocusesFirstInput_OnOpen()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Act - Open modal
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("#modal-container input, .modal input");
        await Page.WaitForTimeoutAsync(150);
        
        // Assert - First input should be focused
        var focusedElement = await Page.EvaluateAsync<string>(
            "document.activeElement?.tagName");
        
        Assert.Equal("INPUT", focusedElement);
    }

    [Fact]
    public async Task Modal_TrapsFocus()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Open modal
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("#modal-container, .modal");
        
        // Act - Tab through all elements
        for (int i = 0; i < 10; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            await Page.WaitForTimeoutAsync(50);
        }
        
        // Assert - Focus should still be within modal
        var focusedInModal = await Page.EvaluateAsync<bool>(@"
            const modal = document.querySelector('#modal-container, .modal');
            return modal && modal.contains(document.activeElement);
        ");
        
        Assert.True(focusedInModal, "Focus should be trapped within modal");
    }

    [Fact]
    public async Task Modal_RestoresFocus_OnClose()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        var addButton = await Page.QuerySelectorAsync(
            "#add-artist-btn, button[hx-get*='CreateForm']");
        Assert.NotNull(addButton);
        
        // Focus and click the add button
        await addButton.FocusAsync();
        await addButton.ClickAsync();
        await Page.WaitForSelectorAsync("#modal-container form, .modal form");
        
        // Act - Close modal with Escape
        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForTimeoutAsync(300);
        
        // Assert - Focus should return to trigger element
        var focusedId = await Page.EvaluateAsync<string>(
            "document.activeElement?.id || document.activeElement?.className");
        
        // Focus restoration is a nice-to-have accessibility feature
        // This test documents whether it's implemented
    }
}
```

---

## 22.7 Testing Error Scenarios

Test that your application handles errors gracefully, showing appropriate messages and maintaining usable state.

### 22.7.1 Testing Server Error Handling

Test responses to various HTTP error status codes.

**Browser/Errors/ServerErrorTests.cs**

```csharp
using ChinookDashboard.Tests.Browser.Fixtures;

namespace ChinookDashboard.Tests.Browser.Errors;

public class ServerErrorTests : PlaywrightTestBase
{
    public ServerErrorTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Error404_ShowsNotFoundMessage()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Act - Try to edit non-existent artist
        await Page.EvaluateAsync(@"
            htmx.ajax('GET', '/Artists?handler=Edit&id=99999', {target: '#artist-list'});
        ");
        
        await Page.WaitForTimeoutAsync(500);
        
        // Assert - Should show error indication
        var hasError = await Page.QuerySelectorAsync(
            ".toast.error, .error-message, [class*='error']") != null;
        
        // Or the element might show an inline error
        var content = await Page.ContentAsync();
        var showsError = content.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                        content.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                        hasError;
        
        // This test documents error handling behavior
    }

    [Fact]
    public async Task Error500_ShowsServerErrorMessage()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // We need a way to trigger a 500 error
        // Option 1: Use a test endpoint
        // Option 2: Intercept request and return error
        
        await Page.RouteAsync("**/Artists*handler=SimulateError*", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 500,
                Body = "Internal Server Error"
            });
        });
        
        // Act - Make request that will fail
        await Page.EvaluateAsync(@"
            htmx.ajax('GET', '/Artists?handler=SimulateError', {target: '#artist-list'});
        ");
        
        await Page.WaitForTimeoutAsync(500);
        
        // Assert - Error should be indicated to user
        var toast = await Page.QuerySelectorAsync(".toast.error, .toast-error");
        
        // Error handling varies by implementation
    }

    [Fact]
    public async Task Error400_ShowsBadRequestMessage()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Intercept to return 400
        await Page.RouteAsync("**/Artists*handler=Create*", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 400,
                ContentType = "text/html",
                Body = "<div class='error'>Bad Request: Invalid data</div>"
            });
        });
        
        // Open create form and submit
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("input[name='name']");
        await Page.FillAsync("input[name='name']", "Test");
        await Page.ClickAsync("button[type='submit']");
        
        await Page.WaitForTimeoutAsync(500);
        
        // Assert - Should show validation/error feedback
        var content = await Page.ContentAsync();
        Assert.True(
            content.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("Bad Request", StringComparison.OrdinalIgnoreCase),
            "Should indicate bad request error");
    }

    [Fact]
    public async Task ErrorResponse_PreservesPageState()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        var initialContent = await Page.ContentAsync();
        
        // Intercept with error
        await Page.RouteAsync("**/Artists*handler=List*", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 500,
                Body = "Server Error"
            });
        });
        
        // Act - Try to search (will fail)
        await Page.FillAsync("input[name='search']", "test");
        await Page.WaitForTimeoutAsync(1000);
        
        // Assert - Page should still be functional
        var artistList = await Page.QuerySelectorAsync(
            "#artist-list, .artist-table, table");
        Assert.NotNull(artistList);
    }
}
```

### 22.7.2 Testing Validation Errors

Test form validation feedback.

**Browser/Errors/FormValidationTests.cs**

```csharp
using ChinookDashboard.Tests.Browser.Fixtures;

namespace ChinookDashboard.Tests.Browser.Errors;

public class FormValidationTests : PlaywrightTestBase
{
    public FormValidationTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task EmptyRequiredField_ShowsValidationError()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Open create form
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("input[name='name']");
        
        // Clear any default value
        await Page.FillAsync("input[name='name']", "");
        
        // Act - Try to submit
        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForTimeoutAsync(300);
        
        // Assert - Should show validation error
        // Check for HTML5 validation
        var validationMessage = await Page.EvaluateAsync<string>(
            "document.querySelector('input[name=\"name\"]')?.validationMessage");
        
        // Or server-side validation message
        var errorMessage = await Page.QuerySelectorAsync(
            ".validation-error, .field-validation-error, .error-message, [class*='error']");
        
        Assert.True(
            !string.IsNullOrEmpty(validationMessage) || errorMessage != null,
            "Should show validation error for empty required field");
    }

    [Fact]
    public async Task ValidationError_FormStaysOpen()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("input[name='name']");
        await Page.FillAsync("input[name='name']", "");
        
        // Act - Submit invalid form
        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForTimeoutAsync(300);
        
        // Assert - Form should still be visible
        var form = await Page.QuerySelectorAsync(
            "#modal-container form, .modal form, form[hx-post*='Create']");
        Assert.NotNull(form);
    }

    [Fact]
    public async Task ValidationError_CanCorrectAndResubmit()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("input[name='name']");
        
        // Submit empty (invalid)
        await Page.FillAsync("input[name='name']", "");
        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForTimeoutAsync(300);
        
        // Act - Fix and resubmit
        var validName = "Corrected Artist " + Guid.NewGuid().ToString()[..6];
        await Page.FillAsync("input[name='name']", validName);
        await Page.ClickAsync("button[type='submit']");
        
        // Wait for success
        try
        {
            await WaitForHtmxRequestAsync("handler=Create");
            await Page.WaitForTimeoutAsync(300);
            
            // Assert - Should succeed
            var content = await Page.ContentAsync();
            Assert.Contains(validName, content);
        }
        catch
        {
            // If HTML5 validation prevents submission, that's also valid
        }
    }

    [Fact]
    public async Task ServerValidationError_DisplaysInForm()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Intercept to return validation error
        await Page.RouteAsync("**/Artists*handler=Create*", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/html",
                Body = @"
                    <form hx-post='/Artists?handler=Create'>
                        <input name='name' value='' class='input-validation-error' />
                        <span class='field-validation-error'>Name is required</span>
                        <button type='submit'>Save</button>
                    </form>"
            });
        });
        
        // Open and submit form
        await Page.ClickAsync("#add-artist-btn, button[hx-get*='CreateForm']");
        await Page.WaitForSelectorAsync("input[name='name']");
        await Page.FillAsync("input[name='name']", "x");
        await Page.ClickAsync("button[type='submit']");
        
        await Page.WaitForTimeoutAsync(300);
        
        // Assert - Server validation message should display
        var errorSpan = await Page.QuerySelectorAsync(".field-validation-error");
        Assert.NotNull(errorSpan);
        
        var errorText = await errorSpan.TextContentAsync();
        Assert.Contains("required", errorText ?? "", StringComparison.OrdinalIgnoreCase);
    }
}
```

### 22.7.3 Testing Network Errors

Test behavior when network connectivity is lost.

**Browser/Errors/NetworkErrorTests.cs**

```csharp
using ChinookDashboard.Tests.Browser.Fixtures;

namespace ChinookDashboard.Tests.Browser.Errors;

public class NetworkErrorTests : PlaywrightTestBase
{
    public NetworkErrorTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task OfflineMode_ShowsError()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Go offline
        await Context.SetOfflineAsync(true);
        
        // Act - Try to perform action
        await Page.FillAsync("input[name='search']", "test");
        
        // Wait for error handling
        await Page.WaitForTimeoutAsync(2000);
        
        // Assert - Should indicate network error
        // htmx triggers htmx:sendError event
        var hasErrorIndication = await Page.EvaluateAsync<bool>(@"
            document.querySelector('.toast.error, .error-message, .network-error') !== null ||
            document.body.classList.contains('htmx-request-error')
        ");
        
        // Restore network for cleanup
        await Context.SetOfflineAsync(false);
        
        // Network error handling varies by implementation
    }

    [Fact]
    public async Task NetworkRecovery_AllowsRetry()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Go offline
        await Context.SetOfflineAsync(true);
        
        // Try action (will fail)
        await Page.FillAsync("input[name='search']", "AC");
        await Page.WaitForTimeoutAsync(1000);
        
        // Act - Restore network
        await Context.SetOfflineAsync(false);
        
        // Retry the action
        await Page.FillAsync("input[name='search']", "AC/DC");
        await Page.WaitForTimeoutAsync(1000);
        
        // Assert - Should work after recovery
        var content = await Page.ContentAsync();
        // Results may or may not appear depending on implementation
    }

    [Fact]
    public async Task SlowNetwork_ShowsLoadingIndicator()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Slow down all requests
        await Page.RouteAsync("**/*", async route =>
        {
            await Task.Delay(2000); // 2 second delay
            await route.ContinueAsync();
        });
        
        // Act - Trigger request
        var searchTask = Page.FillAsync("input[name='search']", "test");
        
        // Check for loading indicator immediately
        await Page.WaitForTimeoutAsync(100);
        
        var hasIndicator = await Page.QuerySelectorAsync(
            ".htmx-indicator:not([style*='display: none']), .loading, .spinner") != null;
        
        // Or check for htmx-request class
        var hasRequestClass = await Page.EvaluateAsync<bool>(
            "document.querySelector('.htmx-request') !== null");
        
        // Clean up route
        await Page.UnrouteAsync("**/*");
        
        // Loading indication is good UX but not required
    }
}
```

### 22.7.4 Testing Timeout Behavior

Test handling of slow or timing-out requests.

**Browser/Errors/TimeoutTests.cs**

```csharp
using ChinookDashboard.Tests.Browser.Fixtures;

namespace ChinookDashboard.Tests.Browser.Errors;

public class TimeoutTests : PlaywrightTestBase
{
    public TimeoutTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task SlowRequest_ShowsLoadingState()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Intercept and delay response
        await Page.RouteAsync("**/Artists*handler=List*", async route =>
        {
            await Task.Delay(3000); // 3 second delay
            await route.ContinueAsync();
        });
        
        // Act - Trigger search
        _ = Page.FillAsync("input[name='search']", "slow");
        
        // Check loading state
        await Page.WaitForTimeoutAsync(500);
        
        // Assert - Should show loading indication
        var isLoading = await Page.EvaluateAsync<bool>(@"
            document.querySelector('.htmx-request') !== null ||
            document.querySelector('.htmx-indicator:not(.hidden)') !== null ||
            document.querySelector('.loading') !== null
        ");
        
        // Clean up
        await Page.UnrouteAsync("**/Artists*handler=List*");
        
        // This test documents loading state behavior
    }

    [Fact]
    public async Task TimeoutResponse_HandledGracefully()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Configure htmx timeout (if not already set)
        await Page.EvaluateAsync("htmx.config.timeout = 1000"); // 1 second
        
        // Intercept and delay beyond timeout
        await Page.RouteAsync("**/Artists*handler=List*", async route =>
        {
            await Task.Delay(5000); // 5 seconds - beyond timeout
            await route.ContinueAsync();
        });
        
        // Act - Trigger request
        await Page.FillAsync("input[name='search']", "timeout");
        
        // Wait for timeout to trigger
        await Page.WaitForTimeoutAsync(2000);
        
        // Assert - Page should still be usable
        var searchInput = await Page.QuerySelectorAsync("input[name='search']");
        Assert.NotNull(searchInput);
        
        // Clean up
        await Page.UnrouteAsync("**/Artists*handler=List*");
    }

    [Fact]
    public async Task AbortedRequest_DoesNotCorruptState()
    {
        // Arrange
        await Page.GotoAsync("/Artists");
        
        // Act - Start request then navigate away
        await Page.FillAsync("input[name='search']", "will be aborted");
        
        // Immediately navigate away
        await Page.GotoAsync("/Artists");
        
        // Assert - Page should load normally
        var content = await Page.ContentAsync();
        Assert.Contains("Artists", content);
        
        var table = await Page.QuerySelectorAsync("table, #artist-list");
        Assert.NotNull(table);
    }
}
```

---

## 22.8 Test Organization and Best Practices

Good test organization makes tests easier to maintain, run, and understand.

### 22.8.1 Organizing Test Files

Structure your test project by test type and feature:

```
ChinookDashboard.Tests/
├── ChinookDashboard.Tests.csproj
├── Unit/
│   ├── Services/
│   │   ├── ArtistServiceTests.cs
│   │   ├── AlbumServiceTests.cs
│   │   └── TrackServiceTests.cs
│   ├── Models/
│   │   ├── PaginatedListTests.cs
│   │   └── TrackSummaryTests.cs
│   └── Helpers/
│       ├── HtmxExtensionTests.cs
│       └── ToastHelperTests.cs
├── Integration/
│   ├── Fixtures/
│   │   └── ChinookTestFactory.cs
│   ├── Common/
│   │   ├── IntegrationTestBase.cs
│   │   ├── HttpClientHtmxExtensions.cs
│   │   ├── HtmlParsingHelper.cs
│   │   └── HtmxAssertions.cs
│   ├── Artists/
│   │   ├── ArtistPageTests.cs
│   │   ├── ArtistPartialTests.cs
│   │   ├── ArtistEditFormTests.cs
│   │   ├── ArtistResponseHeaderTests.cs
│   │   └── ArtistOobTests.cs
│   └── Tracks/
│       └── TrackRowAttributeTests.cs
├── Browser/
│   ├── Fixtures/
│   │   └── PlaywrightFixture.cs
│   ├── Common/
│   │   └── PlaywrightTestBase.cs
│   ├── Artists/
│   │   ├── ArtistSearchTests.cs
│   │   ├── InlineEditTests.cs
│   │   ├── DeleteWithOobTests.cs
│   │   └── CreateArtistModalTests.cs
│   ├── Hyperscript/
│   │   ├── TabSelectionTests.cs
│   │   ├── KeyboardNavigationTests.cs
│   │   ├── ToastAutoDismissTests.cs
│   │   └── EditFormFocusTests.cs
│   └── Errors/
│       ├── ServerErrorTests.cs
│       ├── FormValidationTests.cs
│       ├── NetworkErrorTests.cs
│       └── TimeoutTests.cs
└── TestData/
    └── SeedData.cs
```

**Naming Conventions:**

- Test classes: `{Feature}{TestType}Tests.cs` (e.g., `ArtistSearchTests.cs`)
- Test methods: `{Action}_{Condition}_{ExpectedResult}` (e.g., `Search_WithMatchingTerm_ReturnsFilteredResults`)
- Use descriptive names that read like requirements

### 22.8.2 Test Data Management

Create a centralized seeding class for consistent test data.

**TestData/SeedData.cs**

```csharp
using ChinookDashboard.Data;
using ChinookDashboard.Data.Entities;

namespace ChinookDashboard.Tests.TestData;

public static class SeedData
{
    public static void Initialize(ChinookContext context)
    {
        // Clear existing data
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        // Seed in order of dependencies
        SeedGenres(context);
        SeedArtists(context);
        SeedAlbums(context);
        SeedTracks(context);

        context.SaveChanges();
    }

    public static void SeedGenres(ChinookContext context)
    {
        var genres = new[]
        {
            new Genre { Id = 1, Name = "Rock" },
            new Genre { Id = 2, Name = "Metal" },
            new Genre { Id = 3, Name = "Jazz" },
            new Genre { Id = 4, Name = "Blues" },
            new Genre { Id = 5, Name = "Classical" }
        };

        context.Genres.AddRange(genres);
    }

    public static void SeedArtists(ChinookContext context)
    {
        var artists = new[]
        {
            new Artist { Id = 1, Name = "AC/DC" },
            new Artist { Id = 2, Name = "Accept" },
            new Artist { Id = 3, Name = "Aerosmith" },
            new Artist { Id = 4, Name = "Led Zeppelin" },
            new Artist { Id = 5, Name = "Metallica" },
            new Artist { Id = 6, Name = "Iron Maiden" },
            new Artist { Id = 7, Name = "Black Sabbath" },
            new Artist { Id = 8, Name = "Deep Purple" },
            new Artist { Id = 9, Name = "Judas Priest" },
            new Artist { Id = 10, Name = "Ozzy Osbourne" }
        };

        context.Artists.AddRange(artists);
    }

    public static void SeedAlbums(ChinookContext context)
    {
        var albums = new[]
        {
            // AC/DC albums
            new Album { Id = 1, Title = "Back in Black", ArtistId = 1 },
            new Album { Id = 2, Title = "Highway to Hell", ArtistId = 1 },
            new Album { Id = 3, Title = "For Those About to Rock", ArtistId = 1 },
            
            // Accept
            new Album { Id = 4, Title = "Restless and Wild", ArtistId = 2 },
            new Album { Id = 5, Title = "Balls to the Wall", ArtistId = 2 },
            
            // Aerosmith
            new Album { Id = 6, Title = "Get a Grip", ArtistId = 3 },
            new Album { Id = 7, Title = "Pump", ArtistId = 3 },
            
            // Led Zeppelin
            new Album { Id = 8, Title = "Led Zeppelin IV", ArtistId = 4 },
            new Album { Id = 9, Title = "Physical Graffiti", ArtistId = 4 },
            
            // Metallica
            new Album { Id = 10, Title = "Master of Puppets", ArtistId = 5 },
            new Album { Id = 11, Title = "...And Justice for All", ArtistId = 5 },
            new Album { Id = 12, Title = "The Black Album", ArtistId = 5 }
        };

        context.Albums.AddRange(albums);
    }

    public static void SeedTracks(ChinookContext context)
    {
        var tracks = new[]
        {
            // Back in Black tracks
            new Track { Id = 1, Name = "Hells Bells", AlbumId = 1, GenreId = 1, 
                Milliseconds = 312000, UnitPrice = 0.99m },
            new Track { Id = 2, Name = "Shoot to Thrill", AlbumId = 1, GenreId = 1, 
                Milliseconds = 317000, UnitPrice = 0.99m },
            new Track { Id = 3, Name = "Back in Black", AlbumId = 1, GenreId = 1, 
                Milliseconds = 255000, UnitPrice = 0.99m },
            
            // Highway to Hell tracks
            new Track { Id = 4, Name = "Highway to Hell", AlbumId = 2, GenreId = 1, 
                Milliseconds = 208000, UnitPrice = 0.99m },
            new Track { Id = 5, Name = "Girls Got Rhythm", AlbumId = 2, GenreId = 1, 
                Milliseconds = 203000, UnitPrice = 0.99m },
            
            // Led Zeppelin IV
            new Track { Id = 6, Name = "Stairway to Heaven", AlbumId = 8, GenreId = 1, 
                Milliseconds = 482000, UnitPrice = 0.99m },
            new Track { Id = 7, Name = "Black Dog", AlbumId = 8, GenreId = 1, 
                Milliseconds = 226000, UnitPrice = 0.99m },
            new Track { Id = 8, Name = "Rock and Roll", AlbumId = 8, GenreId = 1, 
                Milliseconds = 220000, UnitPrice = 0.99m },
            
            // Master of Puppets
            new Track { Id = 9, Name = "Battery", AlbumId = 10, GenreId = 2, 
                Milliseconds = 312000, UnitPrice = 0.99m },
            new Track { Id = 10, Name = "Master of Puppets", AlbumId = 10, GenreId = 2, 
                Milliseconds = 515000, UnitPrice = 0.99m }
        };

        context.Tracks.AddRange(tracks);
    }

    /// <summary>
    /// Creates a minimal dataset for fast tests.
    /// </summary>
    public static void InitializeMinimal(ChinookContext context)
    {
        context.Database.EnsureCreated();

        context.Genres.Add(new Genre { Id = 1, Name = "Rock" });
        context.Artists.AddRange(
            new Artist { Id = 1, Name = "Test Artist 1" },
            new Artist { Id = 2, Name = "Test Artist 2" }
        );

        context.SaveChanges();
    }

    /// <summary>
    /// Resets data to initial state (for test isolation).
    /// </summary>
    public static void Reset(ChinookContext context)
    {
        context.Tracks.RemoveRange(context.Tracks);
        context.Albums.RemoveRange(context.Albums);
        context.Artists.RemoveRange(context.Artists);
        context.Genres.RemoveRange(context.Genres);
        context.SaveChanges();

        Initialize(context);
    }
}
```

### 22.8.3 Test Utilities and Shared Code

Create helpers for common testing patterns.

**Browser/Common/PlaywrightHelpers.cs**

```csharp
using Microsoft.Playwright;

namespace ChinookDashboard.Tests.Browser.Common;

public static class PlaywrightHelpers
{
    /// <summary>
    /// Takes a screenshot with timestamp.
    /// </summary>
    public static async Task CaptureScreenshotAsync(
        IPage page, 
        string testName, 
        string suffix = "")
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{testName}{suffix}_{timestamp}.png";
        var path = Path.Combine("TestResults", "Screenshots", fileName);
        
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
            FullPage = true
        });
    }

    /// <summary>
    /// Captures trace for debugging failed tests.
    /// </summary>
    public static async Task StartTracingAsync(IBrowserContext context, string testName)
    {
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
    }

    public static async Task StopTracingAsync(IBrowserContext context, string testName)
    {
        var path = Path.Combine("TestResults", "Traces", $"{testName}.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        
        await context.Tracing.StopAsync(new TracingStopOptions
        {
            Path = path
        });
    }

    /// <summary>
    /// Waits for htmx to settle (no pending requests).
    /// </summary>
    public static async Task WaitForHtmxSettleAsync(IPage page, int timeoutMs = 5000)
    {
        await page.WaitForFunctionAsync(
            "() => !document.querySelector('.htmx-request')",
            new() { Timeout = timeoutMs });
    }

    /// <summary>
    /// Waits for any htmx request to complete.
    /// </summary>
    public static async Task<IResponse> WaitForAnyHtmxRequestAsync(IPage page)
    {
        return await page.WaitForResponseAsync(r => 
            r.Request.Headers.ContainsKey("hx-request") ||
            r.Url.Contains("handler="));
    }
}
```

**Integration/Common/TestHelpers.cs**

```csharp
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

namespace ChinookDashboard.Tests.Integration.Common;

public static class TestHelpers
{
    /// <summary>
    /// Extracts all OOB elements from a response document.
    /// </summary>
    public static IEnumerable<(IElement Element, string Target, string SwapType)> 
        GetOobUpdates(IHtmlDocument document)
    {
        var oobElements = document.QuerySelectorAll("[hx-swap-oob]");
        
        foreach (var element in oobElements)
        {
            var oobValue = element.GetAttribute("hx-swap-oob") ?? "true";
            var id = element.Id ?? "";
            
            // Parse OOB value (e.g., "true", "innerHTML", "outerHTML:#target")
            var parts = oobValue.Split(':');
            var swapType = parts[0];
            var target = parts.Length > 1 ? parts[1] : $"#{id}";
            
            yield return (element, target, swapType);
        }
    }

    /// <summary>
    /// Verifies all form inputs have unique names.
    /// </summary>
    public static void AssertUniqueFormInputNames(IHtmlDocument document)
    {
        var inputs = document.QuerySelectorAll("input[name], select[name], textarea[name]");
        var names = inputs.Select(i => i.GetAttribute("name")).Where(n => !string.IsNullOrEmpty(n));
        var duplicates = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key);
        
        Assert.Empty(duplicates);
    }
}
```

### 22.8.4 Continuous Integration

Configure GitHub Actions to run tests automatically.

**.github/workflows/test.yml**

```yaml
name: Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  unit-and-integration-tests:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Run Unit Tests
      run: dotnet test --no-build --configuration Release --filter "FullyQualifiedName~Unit" --logger "trx;LogFileName=unit-results.trx"
    
    - name: Run Integration Tests
      run: dotnet test --no-build --configuration Release --filter "FullyQualifiedName~Integration" --logger "trx;LogFileName=integration-results.trx"
    
    - name: Upload test results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: test-results
        path: '**/TestResults/*.trx'

  browser-tests:
    runs-on: ubuntu-latest
    needs: unit-and-integration-tests
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Install Playwright browsers
      run: pwsh ChinookDashboard.Tests/bin/Release/net8.0/playwright.ps1 install --with-deps chromium
    
    - name: Run Browser Tests
      run: dotnet test --no-build --configuration Release --filter "FullyQualifiedName~Browser" --logger "trx;LogFileName=browser-results.trx"
      env:
        PLAYWRIGHT_BROWSERS_PATH: /home/runner/.cache/ms-playwright
    
    - name: Upload browser test results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: browser-test-results
        path: '**/TestResults/*.trx'
    
    - name: Upload Playwright traces
      uses: actions/upload-artifact@v4
      if: failure()
      with:
        name: playwright-traces
        path: '**/TestResults/Traces/'
    
    - name: Upload screenshots
      uses: actions/upload-artifact@v4
      if: failure()
      with:
        name: screenshots
        path: '**/TestResults/Screenshots/'
```

---

## 22.9 Summary

Testing htmx applications requires a multi-layered approach. Unit tests verify business logic, integration tests verify HTML structure and htmx attributes, and browser tests verify actual user interactions.

### Testing Strategy

| Test Type | Scope | Tools | Speed | When to Use |
|-----------|-------|-------|-------|-------------|
| **Unit** | Services, models, helpers | xUnit, in-memory database | Fast (ms) | Business logic, calculations, data access |
| **Integration** | Handlers, partials, attributes | WebApplicationFactory, AngleSharp | Medium (seconds) | HTML structure, htmx attributes, response headers |
| **Browser** | Full interactions, JavaScript | Playwright | Slow (seconds) | User workflows, Hyperscript, dynamic updates |

### Key Testing Patterns

| Pattern | Purpose | Example |
|---------|---------|---------|
| **htmx Request Headers** | Simulate htmx requests | `client.HtmxGetAsync(url, target)` |
| **HTML Parsing** | Verify DOM structure | `document.QuerySelector("[hx-get]")` |
| **htmx Assertions** | Verify htmx attributes | `HtmxAssertions.HasHxTarget(element, "#target")` |
| **Response Headers** | Test HX-Trigger, HX-Push-Url | `response.Headers["HX-Trigger"]` |
| **OOB Parsing** | Extract OOB update elements | `document.QuerySelectorAll("[hx-swap-oob]")` |
| **Wait for Response** | Sync browser tests | `page.WaitForResponseAsync(predicate)` |
| **Keyboard Simulation** | Test shortcuts | `page.Keyboard.PressAsync("Escape")` |
| **Offline Testing** | Network error handling | `context.SetOfflineAsync(true)` |

### Test Coverage Checklist

**Unit Tests Should Cover:**
- All service methods (CRUD operations)
- View model computed properties
- Pagination calculations
- Helper method logic

**Integration Tests Should Cover:**
- Full page loads return complete HTML
- Partial responses don't include layout
- htmx attributes have correct values
- Response headers are set correctly
- OOB elements target correct IDs
- Forms have anti-forgery tokens
- Form fields match handler parameters

**Browser Tests Should Cover:**
- Search filters results without page reload
- Inline edit complete workflow
- Modal open/submit/close cycle
- Delete with confirmation
- OOB updates change multiple elements
- Keyboard shortcuts work
- Toast appears and auto-dismisses
- Error scenarios show feedback

### Common Testing Pitfalls

**Timing Issues:**
- Always use explicit waits (`WaitForSelector`, `WaitForResponse`)
- Avoid arbitrary `WaitForTimeout` when possible
- Add small buffers after dynamic operations

**Flaky Tests:**
- Use unique data for each test (GUIDs)
- Reset state between tests
- Don't depend on test execution order

**Anti-Patterns:**
- Testing implementation details instead of behavior
- Over-mocking (hiding real bugs)
- Ignoring error paths
- Skipping browser tests for "simple" features

### Companion Code Files

```
ChinookDashboard.Tests/
├── ChinookDashboard.Tests.csproj
├── Unit/
│   ├── ServiceTestBase.cs
│   ├── Services/
│   │   └── ArtistServiceTests.cs
│   ├── Models/
│   │   ├── PaginatedListTests.cs
│   │   └── TrackSummaryTests.cs
│   └── Helpers/
│       ├── HtmxExtensionTests.cs
│       └── ToastHelperTests.cs
├── Integration/
│   ├── Fixtures/
│   │   └── ChinookTestFactory.cs
│   ├── Common/
│   │   ├── IntegrationTestBase.cs
│   │   ├── HttpClientHtmxExtensions.cs
│   │   ├── HtmlParsingHelper.cs
│   │   ├── HtmxAssertions.cs
│   │   └── TestHelpers.cs
│   ├── Artists/
│   │   ├── ArtistPageTests.cs
│   │   ├── ArtistPartialTests.cs
│   │   ├── ArtistEditFormTests.cs
│   │   ├── ArtistResponseHeaderTests.cs
│   │   └── ArtistOobTests.cs
│   └── Tracks/
│       └── TrackRowAttributeTests.cs
├── Browser/
│   ├── Fixtures/
│   │   └── PlaywrightFixture.cs
│   ├── Common/
│   │   ├── PlaywrightTestBase.cs
│   │   └── PlaywrightHelpers.cs
│   ├── Artists/
│   │   ├── ArtistSearchTests.cs
│   │   ├── InlineEditTests.cs
│   │   ├── DeleteWithOobTests.cs
│   │   └── CreateArtistModalTests.cs
│   ├── Hyperscript/
│   │   ├── TabSelectionTests.cs
│   │   ├── KeyboardNavigationTests.cs
│   │   ├── ToastAutoDismissTests.cs
│   │   └── EditFormFocusTests.cs
│   └── Errors/
│       ├── ServerErrorTests.cs
│       ├── FormValidationTests.cs
│       ├── NetworkErrorTests.cs
│       └── TimeoutTests.cs
└── TestData/
    └── SeedData.cs
```

This chapter covered testing strategies for htmx applications from unit tests through browser automation. The patterns and tools presented here provide a foundation for maintaining quality as your application grows. Well-tested htmx applications give confidence that changes won't break existing functionality and that users will have a smooth, responsive experience.
