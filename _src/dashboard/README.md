# Chinook Dashboard

An htmx-powered music catalog dashboard built with ASP.NET Core Razor Pages.

## Features

- **Dashboard**: Overview statistics, genre distribution, recent tracks, top artists
- **Artists Management**: Search, create, inline edit, delete with confirmation
- **Albums Management**: Grid view, detail panel, CRUD operations
- **Tracks Management**: Search, filter by genre, inline edit, bulk actions

## Tech Stack

- ASP.NET Core 8.0 Razor Pages
- htmx 1.9.10
- Hyperscript 0.9.12
- Tailwind CSS (via CDN)
- SQLite with Entity Framework Core

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- SQLite

### Setup

1. Clone the repository
2. Restore client-side libraries:
   ```bash
   dotnet tool install -g Microsoft.Web.LibraryManager.Cli
   libman restore
   ```
3. Download the Chinook database:
   - Download from: https://github.com/lerocha/chinook-database
   - Place `chinook.db` in the project root
4. Run the application:
   ```bash
   dotnet run
   ```
5. Open https://localhost:5001 (or the URL shown in console)

### Alternative: Manual Library Download

If libman doesn't work, download files manually:
- htmx: https://unpkg.com/htmx.org@1.9.10/dist/htmx.min.js
- Hyperscript: https://unpkg.com/hyperscript.org@0.9.12/dist/_hyperscript.min.js

Place in `wwwroot/lib/htmx/` and `wwwroot/lib/hyperscript/` respectively.

## Project Structure

```
ChinookDashboard/
├── Data/                    # DbContext and Entities
├── Services/                # Business logic services
├── Models/                  # View models and DTOs
├── Pages/                   # Razor Pages
│   ├── Index.cshtml        # Dashboard
│   ├── Artists/            # Artist management
│   ├── Albums/             # Album management
│   ├── Tracks/             # Track management
│   └── Shared/             # Partial views and components
└── wwwroot/                # Static files
    ├── css/
    ├── js/
    └── lib/                # Client libraries
```

## htmx Patterns Used

- **Partial page updates**: All CRUD operations return HTML partials
- **Inline editing**: Edit artists/tracks in place
- **Modal dialogs**: Create forms in modals
- **OOB updates**: Update counters and stats after operations
- **HX-Trigger headers**: Show toast notifications
- **Search with debounce**: Type-ahead search with delay
- **Infinite scroll/Pagination**: Load more content
- **Bulk operations**: Select multiple items for batch actions

## Key Files

| File | Purpose |
|------|---------|
| `Pages/Index.cshtml` | Dashboard with stats and charts |
| `Pages/Artists/Index.cshtml` | Artist list with inline edit |
| `Pages/Albums/Index.cshtml` | Album grid with detail panel |
| `Pages/Tracks/Index.cshtml` | Track list with bulk actions |
| `wwwroot/js/site.js` | htmx event handlers and toast system |

## License

This project is for educational purposes as part of the htmx and ASP.NET Core book.
