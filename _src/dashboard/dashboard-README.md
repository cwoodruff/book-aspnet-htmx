# Chinook Dashboard

A music catalog dashboard built with ASP.NET Core Razor Pages, htmx, and Hyperscript. This project demonstrates building interactive, data-rich web applications without a JavaScript framework.

## Overview

This dashboard provides a complete data management interface for the Chinook music database, featuring:

- **Dashboard Home**: Summary statistics, genre distribution chart, recent tracks, and top artists
- **Artist Management**: Searchable/sortable table with inline editing, modal create/delete
- **Album Browser**: Card grid with filters, drill-down detail view with track listing
- **Track Listing**: Full-featured data table with sorting, filtering, pagination, and bulk actions

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- A code editor (Visual Studio, VS Code, or JetBrains Rider)
- No database server required (uses SQLite)

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/cwoodruff/book-aspnet-htmx.git
cd book-aspnet-htmx/_src/chap21/ChinookDashboard
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Run the Application

```bash
dotnet run
```

### 4. Open in Browser

Navigate to `https://localhost:5001` or the URL shown in the console output.

## Database

The project uses SQLite with the Chinook sample database. The database file (`chinook.db`) is included in the repository - no setup required.

### Database Contents

- **275 Artists**: Music artists and bands
- **347 Albums**: Album titles with artist relationships
- **3,503 Tracks**: Individual songs with metadata
- **25 Genres**: Music genre classifications

### Resetting the Database

If you need to reset the database to its original state:

1. Delete the `chinook.db` file
2. Download a fresh copy from the [Chinook Database repository](https://github.com/lerocha/chinook-database)
3. Rename it to `chinook.db` and place in the project root

## Project Structure

```
ChinookDashboard/
├── Data/                    # EF Core DbContext and entities
├── Services/                # Business logic layer
├── Models/                  # View models and DTOs
├── Pages/                   # Razor Pages
│   ├── Index.cshtml        # Dashboard home
│   ├── Artists/            # Artist management
│   ├── Albums/             # Album browser
│   ├── Tracks/             # Track listing
│   └── Shared/             # Layouts and partials
│       ├── Components/     # Reusable UI components
│       ├── Dashboard/      # Dashboard-specific partials
│       ├── Artists/        # Artist partials
│       ├── Albums/         # Album partials
│       └── Tracks/         # Track partials
└── wwwroot/                # Static files (CSS, JS)
```

## Key Technologies

### htmx

htmx handles all AJAX interactions through HTML attributes:

- `hx-get`, `hx-post`, `hx-delete`: HTTP requests
- `hx-target`, `hx-swap`: Content placement
- `hx-trigger`: Event handling with debouncing
- `hx-push-url`: Browser history integration
- `hx-swap-oob`: Out-of-band updates

### Hyperscript

Hyperscript manages client-side interactions:

- Tab active state toggling
- Modal open/close behavior
- Auto-dismiss notifications
- Focus management
- Escape key handling

### Entity Framework Core

EF Core with SQLite provides data access:

- Code-first entity mapping to existing database
- Navigation properties for related data
- Efficient queries with projections

## Features Demonstrated

| Feature | htmx Technique | Location |
|---------|----------------|----------|
| Independent panel loading | `hx-trigger="load"` | Dashboard home |
| Auto-refresh | `hx-trigger="every 30s"` | Stat cards |
| Debounced search | `hx-trigger="keyup changed delay:300ms"` | All list views |
| Request cancellation | `hx-sync="this:replace"` | Search inputs |
| URL state | `hx-push-url` | Filters and pagination |
| Browser history | `hx-history-elt` | Album drill-down |
| Inline editing | `hx-swap="outerHTML"` | Artists, Tracks |
| Modal dialogs | `hx-target="#modal-container"` | Create/Delete |
| Multi-area updates | `hx-swap-oob` | Stats, toasts |
| Bulk actions | `hx-include` with checkboxes | Tracks |

## Configuration

### Connection String

The database connection is configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "ChinookConnection": "Data Source=chinook.db"
  }
}
```

### Anti-Forgery Tokens

htmx is configured to include anti-forgery tokens automatically via the layout's JavaScript:

```javascript
document.body.addEventListener('htmx:configRequest', function(event) {
    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    if (token) {
        event.detail.headers['RequestVerificationToken'] = token.value;
    }
});
```

## Development

### Adding a New Feature

1. Create entity class in `Data/Entities/`
2. Add DbSet to `ChinookContext`
3. Create service interface and implementation
4. Register service in `Program.cs`
5. Create Razor Page in appropriate folder
6. Create partials in `Shared/[Feature]/`

### CSS Organization

- `dashboard.css`: Layout, grid, feature-specific styles
- `components.css`: Reusable component styles (modals, toasts, buttons)

### JavaScript Helpers

`site.js` contains:
- `formatDuration()`: Format milliseconds as mm:ss
- `debounce()`: Debounce function calls
- `confirmAction()`: Confirmation dialogs
- Modal management functions
- Toast notification functions
- Global htmx event handlers

## Troubleshooting

### Database Not Found

If you see "SQLite Error: unable to open database file":
- Verify `chinook.db` exists in the project root
- Check the connection string in `appsettings.json`

### htmx Requests Not Working

- Open browser developer tools (F12)
- Check the Network tab for request/response details
- Verify the handler name matches (case-sensitive)
- Check for anti-forgery token issues in console

### Styles Not Applied

- Clear browser cache (Ctrl+Shift+R)
- Verify CSS files are referenced in `_Layout.cshtml`
- Check for CSS syntax errors in browser console

## License

This project is provided as educational material for the book "ASP.NET Core and htmx". The Chinook database is available under the [MIT License](https://github.com/lerocha/chinook-database/blob/master/LICENSE.md).

## Additional Resources

- [htmx Documentation](https://htmx.org/docs/)
- [Hyperscript Documentation](https://hyperscript.org/docs/)
- [ASP.NET Core Razor Pages](https://docs.microsoft.com/aspnet/core/razor-pages/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [Chinook Database](https://github.com/lerocha/chinook-database)
