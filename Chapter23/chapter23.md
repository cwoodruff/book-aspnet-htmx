---
order: 8
icon: stack
label: Chap 23 - Deploying htmx Applications on Azure
meta:
title: "Deploying htmx Applications on Azure"
---
# Deploying htmx Applications on Azure

Building an htmx application is only half the work. Getting it running reliably in production requires careful configuration, proper infrastructure, and automated deployment pipelines. This chapter takes the Chinook Dashboard from development to a production-ready Azure deployment.

## 23.1 Introduction

### Why Deployment Matters for htmx Applications

htmx applications have specific characteristics that affect deployment strategy. Understanding these helps you make better infrastructure decisions.

**Latency Sensitivity**: htmx makes frequent small requests. Each button click, form submission, or search keystroke triggers a server round-trip. High latency makes applications feel sluggish. Server proximity to users matters more than with traditional page-based applications.

**Partial Response Compression**: htmx responses are often small HTML fragments. A table row might be 500 bytes, a form 2KB. Compression algorithms work best on larger payloads, but even small gains add up when users generate dozens of requests per session.

**Caching Opportunities**: Some htmx responses are highly cacheable. A genre dropdown or static list doesn't change often. Proper cache headers reduce server load and improve response times.

**Static Asset Optimization**: htmx.js, Hyperscript, and your CSS are requested on every page load. CDN delivery and aggressive caching for these files significantly improves initial page load.

### Azure Hosting Options

Azure offers several ways to host ASP.NET Core applications:

**Azure App Service** is the focus of this chapter. It provides managed hosting with automatic OS patching, built-in load balancing, deployment slots, and easy scaling. Best for most web applications.

**Azure Container Apps** runs containerized applications with automatic scaling, including scale-to-zero. Good if you want container portability without managing Kubernetes.

**Azure Kubernetes Service (AKS)** provides full Kubernetes orchestration. Best for complex microservices architectures or teams already invested in Kubernetes.

**Azure Static Web Apps** hosts static files with optional API backends. Not ideal for htmx applications since server-side rendering is central to the pattern.

For the Chinook Dashboard, App Service provides the right balance of simplicity, features, and cost. The patterns in this chapter apply to other hosting options with minor modifications.

### What This Chapter Covers

This chapter walks through:

- Preparing the application for production (configuration, compression, assets)
- Creating Azure resources (App Service, SQL Database, Key Vault)
- Building CI/CD pipelines with GitHub Actions
- htmx-specific deployment considerations (caching partials, error handling)
- Monitoring with Application Insights
- Scaling and performance optimization
- Security hardening

By the end, you'll have a fully automated pipeline that builds, tests, and deploys your htmx application to Azure.

---

## 23.2 Preparing Your Application for Production

Before deploying, configure your application for production workloads. Development defaults prioritize convenience; production requires security, performance, and reliability.

### 23.2.1 Environment-Specific Configuration

ASP.NET Core loads configuration from multiple sources in a specific order. Later sources override earlier ones:

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific)
3. Environment variables
4. Command-line arguments

The `ASPNETCORE_ENVIRONMENT` variable determines which environment file loads. Azure App Service sets this to "Production" by default.

**appsettings.Production.json**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
      "ChinookDashboard": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "ChinookConnection": "SET_IN_AZURE_APP_SETTINGS"
  },
  "ApplicationInsights": {
    "ConnectionString": "SET_IN_AZURE_APP_SETTINGS"
  },
  "Htmx": {
    "Timeout": 30000,
    "HistoryCacheSize": 10,
    "DefaultSwapStyle": "innerHTML",
    "DefaultSettleDelay": 20,
    "IncludeIndicatorStyles": true
  },
  "Caching": {
    "StaticFilesCacheMaxAge": 23536000,
    "PartialResponsesCacheMaxAge": 0,
    "EnableResponseCaching": true
  },
  "Security": {
    "EnableHsts": true,
    "HstsMaxAgeDays": 365,
    "EnableHttpsRedirection": true
  },
  "FeatureFlags": {
    "EnableDetailedErrors": false,
    "EnableSwagger": false,
    "EnableDeveloperExceptionPage": false
  }
}
```

**Key points about this configuration:**

- **Logging levels**: Set Microsoft namespaces to Warning to reduce noise. Keep your application namespace at Information for useful operational logs.
- **Connection strings**: Use placeholder values. Actual secrets come from Azure App Settings or Key Vault.
- **htmx settings**: These values can be injected into your layout for client-side configuration.
- **Feature flags**: Disable development features in production.

**Managing Secrets**

Never commit secrets to source control. The `appsettings.Production.json` contains structure but not actual secrets. Sensitive values come from:

1. **Azure App Settings**: Set in the Azure Portal or via CLI
2. **Azure Key Vault**: For highly sensitive secrets with audit logging
3. **Environment variables**: Set by the deployment platform

```bash
# Don't do this - secrets in code
"ConnectionStrings": {
  "ChinookConnection": "Server=myserver.database.windows.net;Password=secret123"
}

# Do this - placeholder that Azure overrides
"ConnectionStrings": {
  "ChinookConnection": "SET_IN_AZURE_APP_SETTINGS"
}
```

### 23.2.2 Optimizing Static Assets

Production applications should serve minified, cached static files.

#### Client-Side Library Management with LibMan

LibMan (Library Manager) manages client-side dependencies without npm complexity.

**libman.json**

```json
{
  "version": "1.0",
  "defaultProvider": "cdnjs",
  "libraries": [
    {
      "library": "htmx.org@1.9.10",
      "destination": "wwwroot/lib/htmx/",
      "files": [
        "htmx.min.js",
        "htmx.js"
      ]
    },
    {
      "library": "hyperscript.org@0.9.12",
      "destination": "wwwroot/lib/hyperscript/",
      "files": [
        "_hyperscript.min.js",
        "_hyperscript.js"
      ]
    }
  ]
}
```

Restore libraries during build:

```bash
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
libman restore
```

#### Bundling and Minification

For custom CSS and JavaScript, use WebOptimizer or BundlerMinifier:

**bundleconfig.json**

```json
[
  {
    "outputFileName": "wwwroot/css/site.min.css",
    "inputFiles": [
      "wwwroot/css/site.css",
      "wwwroot/css/htmx-indicators.css",
      "wwwroot/css/toast.css"
    ],
    "minify": {
      "enabled": true,
      "renameLocals": true
    }
  },
  {
    "outputFileName": "wwwroot/js/site.min.js",
    "inputFiles": [
      "wwwroot/js/htmx-config.js",
      "wwwroot/js/toast-handler.js"
    ],
    "minify": {
      "enabled": true
    }
  }
]
```

#### Static File Caching

Configure aggressive caching for static files. Fingerprinted assets (with hash in filename) can cache for one year.

```csharp
// In Program.cs
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 1 year
        const int durationInSeconds = 60 * 60 * 24 * 365;
        ctx.Context.Response.Headers.Append(
            "Cache-Control", $"public, max-age={durationInSeconds}");
    }
});
```

For versioned files (using asp-append-version tag helper), the cache is automatically invalidated when files change:

```html
<link rel="stylesheet" href="~/css/site.min.css" asp-append-version="true" />
<script src="~/lib/htmx/htmx.min.js" asp-append-version="true"></script>
```

### 23.2.3 Response Compression

Compression reduces bandwidth and improves response times. htmx partial responses benefit even though they're small.

#### Why Compression Matters for htmx

Consider a typical htmx interaction:

- Search results partial: 3KB uncompressed → 800 bytes compressed (73% reduction)
- Edit form partial: 2KB uncompressed → 500 bytes compressed (75% reduction)
- Table row: 500 bytes uncompressed → 180 bytes compressed (64% reduction)

Over a session with 50 htmx requests, compression saves significant bandwidth.

#### Brotli vs Gzip

Brotli typically achieves 15-20% better compression than Gzip and is supported by all modern browsers. Configure both with Brotli as primary:

```csharp
// In Program.cs - Add before builder.Build()

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "text/html",
        "application/json",
        "text/plain",
        "text/css",
        "application/javascript",
        "text/javascript",
        "application/xml",
        "text/xml"
    });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});
```

Enable compression early in the pipeline:

```csharp
var app = builder.Build();

// Compression should be early in the pipeline
app.UseResponseCompression();

app.UseHttpsRedirection();
app.UseStaticFiles();
// ... rest of pipeline
```

### 23.2.4 Database Considerations

Production database configuration differs significantly from development.

#### Connection String Format for Azure SQL

Azure SQL connection strings include additional parameters:

```
Server=tcp:chinook-server.database.windows.net,1433;
Initial Catalog=ChinookDb;
Persist Security Info=False;
User ID=chinook-admin;
Password={your_password};
MultipleActiveResultSets=False;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

#### Key Vault References

Instead of storing connection strings directly in App Settings, reference Key Vault:

```
@Microsoft.KeyVault(SecretUri=https://chinook-vault.vault.azure.net/secrets/ChinookConnection/)
```

#### Production DbContext Configuration

Configure Entity Framework for production workloads:

```csharp
// In Program.cs
builder.Services.AddDbContext<ChinookContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("ChinookConnection");
    
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Connection resiliency for transient failures
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        
        // Command timeout for long-running queries
        sqlOptions.CommandTimeout(30);
    });
    
    // Production optimizations
    if (builder.Environment.IsProduction())
    {
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
});
```

#### Migration Strategy

For production deployments, generate SQL scripts rather than running migrations directly:

```bash
# Generate migration script
dotnet ef migrations script --idempotent --output migrations.sql

# Review the script before applying
# Apply via Azure Portal Query Editor or sqlcmd during deployment
```

The `--idempotent` flag generates scripts that check if migrations have already been applied, making them safe to run multiple times.

### Complete Production Program.cs

Here's the complete `Program.cs` with all production configurations:

```csharp
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using ChinookDashboard.Data;
using ChinookDashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// ===== Services Configuration =====

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "text/html",
        "application/json",
        "text/plain",
        "text/css",
        "application/javascript"
    });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

// Database Context
builder.Services.AddDbContext<ChinookContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("ChinookConnection");
    
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(30);
    });
    
    if (builder.Environment.IsProduction())
    {
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
});

// Application Services
builder.Services.AddScoped<IArtistService, ArtistService>();
builder.Services.AddScoped<IAlbumService, AlbumService>();
builder.Services.AddScoped<ITrackService, TrackService>();

// Razor Pages
builder.Services.AddRazorPages();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ChinookContext>("database");

// Anti-forgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

var app = builder.Build();

// ===== Middleware Pipeline =====

// Response compression (early in pipeline)
app.UseResponseCompression();

// Error handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Static files with caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var cacheMaxAge = builder.Configuration.GetValue<int>("Caching:StaticFilesCacheMaxAge", 86400);
        ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={cacheMaxAge}");
    }
});

app.UseRouting();
app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");

app.MapRazorPages();

app.Run();
```

---

## 23.3 Setting Up Azure Resources

With the application prepared, create the Azure infrastructure to host it.

### 23.3.1 Azure App Service Setup

Azure App Service provides managed hosting for web applications. Choose the right tier based on your needs:

| Tier | Use Case | Features |
|------|----------|----------|
| F1 (Free) | Testing only | 60 min/day compute, no custom domain |
| B1 (Basic) | Dev/Test | Custom domain, manual scale |
| P1v3 (Premium) | Production | Auto-scale, deployment slots, more CPU/RAM |

For production htmx applications, P1v3 provides deployment slots for zero-downtime deployments and auto-scaling for traffic spikes.

#### Azure CLI Setup Script

Create a script to provision all resources:

**infrastructure/azure-setup.sh**

```bash
#!/bin/bash

# ============================================
# Azure Resource Setup for Chinook Dashboard
# ============================================

# Configuration - Change these values
RESOURCE_GROUP="chinook-rg"
LOCATION="eastus"
APP_SERVICE_PLAN="chinook-plan"
WEB_APP_NAME="chinook-dashboard"  # Must be globally unique
SQL_SERVER_NAME="chinook-sql"     # Must be globally unique
SQL_DATABASE_NAME="ChinookDb"
SQL_ADMIN_USER="chinook-admin"
KEY_VAULT_NAME="chinook-vault"    # Must be globally unique

# Prompt for SQL password (don't hardcode!)
echo "Enter SQL Admin Password (min 8 chars, requires uppercase, lowercase, number):"
read -s SQL_ADMIN_PASSWORD

echo ""
echo "Starting Azure resource provisioning..."
echo "========================================"

# ============================================
# 1. Resource Group
# ============================================
echo "Creating resource group..."
az group create \
    --name $RESOURCE_GROUP \
    --location $LOCATION \
    --output none

echo "✓ Resource group created: $RESOURCE_GROUP"

# ============================================
# 2. App Service Plan
# ============================================
echo "Creating App Service Plan..."
az appservice plan create \
    --name $APP_SERVICE_PLAN \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku P1V3 \
    --is-linux false \
    --output none

echo "✓ App Service Plan created: $APP_SERVICE_PLAN (P1V3)"

# ============================================
# 3. Web App
# ============================================
echo "Creating Web App..."
az webapp create \
    --name $WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --plan $APP_SERVICE_PLAN \
    --runtime "dotnet:8" \
    --output none

echo "✓ Web App created: $WEB_APP_NAME"

# Configure Web App settings
echo "Configuring Web App settings..."
az webapp config set \
    --name $WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --always-on true \
    --http20-enabled true \
    --min-tls-version 1.2 \
    --ftps-state Disabled \
    --output none

# Enable system-assigned managed identity
az webapp identity assign \
    --name $WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --output none

echo "✓ Web App configured with Always On, HTTP/2, TLS 1.2"

# ============================================
# 4. Deployment Slot (Staging)
# ============================================
echo "Creating staging deployment slot..."
az webapp deployment slot create \
    --name $WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --slot staging \
    --output none

# Configure staging slot
az webapp config set \
    --name $WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --slot staging \
    --always-on true \
    --output none

echo "✓ Staging slot created"

# ============================================
# 5. SQL Server
# ============================================
echo "Creating SQL Server..."
az sql server create \
    --name $SQL_SERVER_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --admin-user $SQL_ADMIN_USER \
    --admin-password "$SQL_ADMIN_PASSWORD" \
    --output none

echo "✓ SQL Server created: $SQL_SERVER_NAME"

# Configure firewall to allow Azure services
echo "Configuring SQL Server firewall..."
az sql server firewall-rule create \
    --server $SQL_SERVER_NAME \
    --resource-group $RESOURCE_GROUP \
    --name AllowAzureServices \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0 \
    --output none

echo "✓ SQL Server firewall configured for Azure services"

# ============================================
# 6. SQL Database
# ============================================
echo "Creating SQL Database..."
az sql db create \
    --name $SQL_DATABASE_NAME \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --service-objective S0 \
    --backup-storage-redundancy Local \
    --output none

echo "✓ SQL Database created: $SQL_DATABASE_NAME (Standard S0)"

# ============================================
# 7. Key Vault
# ============================================
echo "Creating Key Vault..."
az keyvault create \
    --name $KEY_VAULT_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --enable-rbac-authorization false \
    --output none

echo "✓ Key Vault created: $KEY_VAULT_NAME"

# Get Web App's managed identity
WEB_APP_IDENTITY=$(az webapp identity show \
    --name $WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query principalId \
    --output tsv)

# Grant Web App access to Key Vault secrets
az keyvault set-policy \
    --name $KEY_VAULT_NAME \
    --object-id $WEB_APP_IDENTITY \
    --secret-permissions get list \
    --output none

echo "✓ Key Vault access policy set for Web App"

# ============================================
# 8. Store Connection String in Key Vault
# ============================================
echo "Storing connection string in Key Vault..."

CONNECTION_STRING="Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Initial Catalog=${SQL_DATABASE_NAME};Persist Security Info=False;User ID=${SQL_ADMIN_USER};Password=${SQL_ADMIN_PASSWORD};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az keyvault secret set \
    --vault-name $KEY_VAULT_NAME \
    --name "ChinookConnection" \
    --value "$CONNECTION_STRING" \
    --output none

echo "✓ Connection string stored in Key Vault"

# ============================================
# 9. Configure Web App Connection String
# ============================================
echo "Configuring Web App to use Key Vault reference..."

KEY_VAULT_REFERENCE="@Microsoft.KeyVault(SecretUri=https://${KEY_VAULT_NAME}.vault.azure.net/secrets/ChinookConnection/)"

az webapp config connection-string set \
    --name $WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --connection-string-type SQLAzure \
    --settings ChinookConnection="$KEY_VAULT_REFERENCE" \
    --output none

# Also set for staging slot
az webapp config connection-string set \
    --name $WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --slot staging \
    --connection-string-type SQLAzure \
    --settings ChinookConnection="$KEY_VAULT_REFERENCE" \
    --output none

echo "✓ Web App configured with Key Vault reference"

# ============================================
# 10. Configure App Settings
# ============================================
echo "Setting application configuration..."

az webapp config appsettings set \
    --name $WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings \
        ASPNETCORE_ENVIRONMENT=Production \
        Logging__LogLevel__Default=Information \
        Logging__LogLevel__Microsoft.AspNetCore=Warning \
    --output none

echo "✓ App settings configured"

# ============================================
# Summary
# ============================================
echo ""
echo "========================================"
echo "Azure Resource Setup Complete!"
echo "========================================"
echo ""
echo "Resources created:"
echo "  • Resource Group: $RESOURCE_GROUP"
echo "  • App Service Plan: $APP_SERVICE_PLAN (P1V3)"
echo "  • Web App: $WEB_APP_NAME"
echo "  • Staging Slot: $WEB_APP_NAME/staging"
echo "  • SQL Server: $SQL_SERVER_NAME.database.windows.net"
echo "  • SQL Database: $SQL_DATABASE_NAME"
echo "  • Key Vault: $KEY_VAULT_NAME"
echo ""
echo "Web App URLs:"
echo "  • Production: https://${WEB_APP_NAME}.azurewebsites.net"
echo "  • Staging: https://${WEB_APP_NAME}-staging.azurewebsites.net"
echo ""
echo "Next steps:"
echo "  1. Run database migrations against $SQL_SERVER_NAME"
echo "  2. Configure GitHub Actions with deployment credentials"
echo "  3. Deploy your application"
echo ""
```

Make the script executable and run it:

```bash
chmod +x infrastructure/azure-setup.sh
./infrastructure/azure-setup.sh
```

### 23.3.2 Azure SQL Database Setup

The setup script creates the database, but you may need additional configuration.

#### Connection String Format

The complete connection string for Azure SQL:

```
Server=tcp:chinook-sql.database.windows.net,1433;
Initial Catalog=ChinookDb;
Persist Security Info=False;
User ID=chinook-admin;
Password={your_password};
MultipleActiveResultSets=False;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

#### Firewall Configuration

The script allows Azure services. To connect from your local machine for migrations:

```bash
# Get your current IP
MY_IP=$(curl -s ifconfig.me)

# Add firewall rule
az sql server firewall-rule create \
    --server chinook-sql \
    --resource-group chinook-rg \
    --name MyLocalIP \
    --start-ip-address $MY_IP \
    --end-ip-address $MY_IP
```

#### Running Migrations

Apply migrations to the production database:

```bash
# Generate idempotent script
dotnet ef migrations script --idempotent --output migrations.sql

# Apply using sqlcmd
sqlcmd -S chinook-sql.database.windows.net \
       -d ChinookDb \
       -U chinook-admin \
       -P 'YourPassword' \
       -i migrations.sql
```

Or use the Azure Portal Query Editor for smaller scripts.

### 23.3.3 Application Settings and Connection Strings

Azure App Settings override `appsettings.json` values. Configure them via CLI or Portal.

#### Setting App Settings via CLI

```bash
# Set multiple settings at once
az webapp config appsettings set \
    --name chinook-dashboard \
    --resource-group chinook-rg \
    --settings \
        ASPNETCORE_ENVIRONMENT=Production \
        ApplicationInsights__ConnectionString="InstrumentationKey=xxx" \
        Htmx__Timeout=30000 \
        FeatureFlags__EnableSwagger=false
```

#### Key Vault References

For sensitive values, use Key Vault references instead of plain text:

```bash
# Store secret in Key Vault
az keyvault secret set \
    --vault-name chinook-vault \
    --name "AppInsightsKey" \
    --value "your-instrumentation-key"

# Reference in App Settings
az webapp config appsettings set \
    --name chinook-dashboard \
    --resource-group chinook-rg \
    --settings \
        ApplicationInsights__ConnectionString="@Microsoft.KeyVault(SecretUri=https://chinook-vault.vault.azure.net/secrets/AppInsightsKey/)"
```

#### Slot Settings

Some settings should differ between production and staging slots. Mark them as slot settings:

```bash
# Make setting "sticky" to its slot
az webapp config appsettings set \
    --name chinook-dashboard \
    --resource-group chinook-rg \
    --slot-settings \
        IsStaging=false

az webapp config appsettings set \
    --name chinook-dashboard \
    --resource-group chinook-rg \
    --slot staging \
    --slot-settings \
        IsStaging=true
```

### 23.3.4 Custom Domain and SSL

Add a custom domain to your App Service.

#### Prerequisites

1. Own a domain (e.g., `chinook.example.com`)
2. Access to DNS management for that domain

#### DNS Configuration

Add a CNAME record pointing to your App Service:

| Type | Name | Value |
|------|------|-------|
| CNAME | chinook | chinook-dashboard.azurewebsites.net |

Or for apex domains (example.com without subdomain), use an A record with Azure's IP and a TXT record for verification.

#### Complete Custom Domain Setup Script

**infrastructure/setup-domain.sh**

```bash
#!/bin/bash

# Configuration
RESOURCE_GROUP="chinook-rg"
WEB_APP_NAME="chinook-dashboard"
CUSTOM_DOMAIN="chinook.example.com"

echo "Setting up custom domain: $CUSTOM_DOMAIN"
echo "========================================="

# ============================================
# 1. Add Custom Domain
# ============================================
echo "Adding custom domain to Web App..."
echo "NOTE: Ensure DNS CNAME record exists first!"
echo "  CNAME: chinook -> ${WEB_APP_NAME}.azurewebsites.net"
echo ""

read -p "DNS configured? (y/n): " DNS_READY
if [ "$DNS_READY" != "y" ]; then
    echo "Please configure DNS first, then re-run this script."
    exit 1
fi

az webapp config hostname add \
    --webapp-name $WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --hostname $CUSTOM_DOMAIN \
    --output none

echo "✓ Custom domain added"

# ============================================
# 2. Create Managed SSL Certificate
# ============================================
echo "Creating managed SSL certificate..."

az webapp config ssl create \
    --name $WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --hostname $CUSTOM_DOMAIN \
    --output none

echo "✓ SSL certificate created (may take a few minutes to provision)"

# ============================================
# 3. Bind SSL Certificate
# ============================================
echo "Binding SSL certificate to domain..."

# Get the certificate thumbprint
THUMBPRINT=$(az webapp config ssl list \
    --resource-group $RESOURCE_GROUP \
    --query "[?subjectName=='$CUSTOM_DOMAIN'].thumbprint" \
    --output tsv)

if [ -z "$THUMBPRINT" ]; then
    echo "Certificate not ready yet. Check Azure Portal in a few minutes."
else
    az webapp config ssl bind \
        --name $WEB_APP_NAME \
        --resource-group $RESOURCE_GROUP \
        --certificate-thumbprint $THUMBPRINT \
        --ssl-type SNI \
        --output none
    
    echo "✓ SSL certificate bound"
fi

# ============================================
# 4. Enforce HTTPS
# ============================================
echo "Enforcing HTTPS redirect..."

az webapp update \
    --name $WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --https-only true \
    --output none

echo "✓ HTTPS-only mode enabled"

# ============================================
# Summary
# ============================================
echo ""
echo "========================================"
echo "Custom Domain Setup Complete!"
echo "========================================"
echo ""
echo "Your site is now available at:"
echo "  https://$CUSTOM_DOMAIN"
echo ""
echo "Note: SSL certificate provisioning may take up to 15 minutes."
echo "Check the Azure Portal if the certificate doesn't appear immediately."
```

#### Enforcing HTTPS in Application

Even with Azure's HTTPS-only setting, add middleware for defense in depth:

```csharp
// In Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
```

Configure HSTS properly:

```csharp
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});
```

With the Azure resources provisioned and the application configured for production, you're ready to set up automated deployments with GitHub Actions in the next section.

## 23.4 GitHub Actions CI/CD Pipeline

Automated deployment pipelines eliminate manual deployment steps, reduce errors, and enable rapid iteration. GitHub Actions integrates directly with your repository to build, test, and deploy on every commit.

### 23.4.1 Understanding GitHub Actions

GitHub Actions uses YAML files in the `.github/workflows` directory to define automation workflows.

#### Workflow Structure

```yaml
name: Workflow Name              # Display name in GitHub UI

on:                              # Triggers
  push:
    branches: [main]
  pull_request:
    branches: [main]

env:                             # Environment variables for all jobs
  DOTNET_VERSION: '8.0.x'

jobs:                            # One or more jobs
  build:                         # Job ID
    runs-on: ubuntu-latest       # Runner type
    
    steps:                       # Sequential steps
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
```

#### Trigger Types

**push**: Runs when commits are pushed to specified branches
```yaml
on:
  push:
    branches: [main, develop]
    paths-ignore:
      - '**.md'
      - 'docs/**'
```

**pull_request**: Runs when PRs target specified branches
```yaml
on:
  pull_request:
    branches: [main]
```

**workflow_dispatch**: Allows manual triggering from GitHub UI
```yaml
on:
  workflow_dispatch:
    inputs:
      environment:
        description: 'Target environment'
        required: true
        default: 'staging'
```

#### GitHub Secrets

Store sensitive values in repository or organization secrets:

1. Navigate to repository Settings → Secrets and variables → Actions
2. Add secrets like `AZURE_CREDENTIALS`, `SQL_CONNECTION_STRING`
3. Reference in workflows: `${{ secrets.AZURE_CREDENTIALS }}`

#### Environments for Deployment Approvals

Create environments with protection rules:

1. Settings → Environments → New environment
2. Add required reviewers for production deployments
3. Reference in workflow: `environment: production`

### 23.4.2 Basic Build and Test Workflow

Start with a workflow that builds and tests on every push and pull request.

**.github/workflows/build-test.yml**

```yaml
name: Build and Test

on:
  push:
    branches: [main, develop]
    paths-ignore:
      - '**.md'
      - 'docs/**'
      - '.github/ISSUE_TEMPLATE/**'
  pull_request:
    branches: [main, develop]

env:
  DOTNET_VERSION: '8.0.x'
  SOLUTION_PATH: 'ChinookDashboard.sln'
  TEST_PROJECT_PATH: 'ChinookDashboard.Tests/ChinookDashboard.Tests.csproj'

jobs:
  build:
    name: Build
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore dependencies
        run: dotnet restore ${{ env.SOLUTION_PATH }}

      - name: Build
        run: dotnet build ${{ env.SOLUTION_PATH }} --no-restore --configuration Release

      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        with:
          name: build-output
          path: |
            **/bin/Release/net8.0/
            !**/bin/Release/net8.0/publish/
          retention-days: 1

  test-unit:
    name: Unit Tests
    needs: build
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore dependencies
        run: dotnet restore ${{ env.TEST_PROJECT_PATH }}

      - name: Run unit tests
        run: |
          dotnet test ${{ env.TEST_PROJECT_PATH }} \
            --no-restore \
            --configuration Release \
            --filter "FullyQualifiedName~Unit" \
            --logger "trx;LogFileName=unit-test-results.trx" \
            --results-directory ./TestResults

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: unit-test-results
          path: ./TestResults/*.trx
          retention-days: 7

  test-integration:
    name: Integration Tests
    needs: build
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore dependencies
        run: dotnet restore ${{ env.TEST_PROJECT_PATH }}

      - name: Run integration tests
        run: |
          dotnet test ${{ env.TEST_PROJECT_PATH }} \
            --no-restore \
            --configuration Release \
            --filter "FullyQualifiedName~Integration" \
            --logger "trx;LogFileName=integration-test-results.trx" \
            --results-directory ./TestResults

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: integration-test-results
          path: ./TestResults/*.trx
          retention-days: 7

  test-browser:
    name: Browser Tests
    needs: build
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore dependencies
        run: dotnet restore ${{ env.TEST_PROJECT_PATH }}

      - name: Build test project
        run: dotnet build ${{ env.TEST_PROJECT_PATH }} --no-restore --configuration Release

      - name: Install Playwright browsers
        run: pwsh ChinookDashboard.Tests/bin/Release/net8.0/playwright.ps1 install --with-deps chromium

      - name: Run browser tests
        run: |
          dotnet test ${{ env.TEST_PROJECT_PATH }} \
            --no-build \
            --configuration Release \
            --filter "FullyQualifiedName~Browser" \
            --logger "trx;LogFileName=browser-test-results.trx" \
            --results-directory ./TestResults

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: browser-test-results
          path: ./TestResults/*.trx
          retention-days: 7

      - name: Upload Playwright traces
        uses: actions/upload-artifact@v4
        if: failure()
        with:
          name: playwright-traces
          path: '**/TestResults/Traces/'
          retention-days: 7
```

### 23.4.3 Deployment Workflow

To deploy to Azure, create a service principal and store its credentials as a GitHub secret.

#### Creating Azure Service Principal

```bash
#!/bin/bash
# create-service-principal.sh

SUBSCRIPTION_ID=$(az account show --query id -o tsv)
RESOURCE_GROUP="chinook-rg"
APP_NAME="chinook-dashboard"

# Create service principal with contributor access to resource group
az ad sp create-for-rbac \
    --name "github-actions-$APP_NAME" \
    --role contributor \
    --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
    --sdk-auth

# Output looks like:
# {
#   "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
#   "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
#   "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
#   "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
#   ...
# }
#
# Copy this entire JSON output and save as GitHub secret: AZURE_CREDENTIALS
```

Save the JSON output as a GitHub secret named `AZURE_CREDENTIALS`.

#### Basic Deployment Workflow

**.github/workflows/deploy.yml**

```yaml
name: Deploy to Azure

on:
  workflow_dispatch:
    inputs:
      environment:
        description: 'Deployment target'
        required: true
        default: 'staging'
        type: choice
        options:
          - staging
          - production

env:
  DOTNET_VERSION: '8.0.x'
  AZURE_WEBAPP_NAME: chinook-dashboard
  AZURE_RESOURCE_GROUP: chinook-rg
  PROJECT_PATH: 'ChinookDashboard/ChinookDashboard.csproj'

jobs:
  deploy:
    name: Deploy to ${{ github.event.inputs.environment }}
    runs-on: ubuntu-latest
    environment: ${{ github.event.inputs.environment }}
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore dependencies
        run: dotnet restore ${{ env.PROJECT_PATH }}

      - name: Build
        run: dotnet build ${{ env.PROJECT_PATH }} --no-restore --configuration Release

      - name: Publish
        run: |
          dotnet publish ${{ env.PROJECT_PATH }} \
            --no-build \
            --configuration Release \
            --output ./publish

      - name: Login to Azure
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Deploy to Staging Slot
        if: github.event.inputs.environment == 'staging'
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          slot-name: staging
          package: ./publish

      - name: Deploy to Production
        if: github.event.inputs.environment == 'production'
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          package: ./publish

      - name: Logout from Azure
        if: always()
        run: az logout
```

### 23.4.4 Complete CI/CD Pipeline

Combine building, testing, and deployment into a single pipeline with proper stage dependencies and approvals.

**.github/workflows/azure-deploy.yml**

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [main, develop]
    paths-ignore:
      - '**.md'
      - 'docs/**'
  pull_request:
    branches: [main]
  workflow_dispatch:

env:
  DOTNET_VERSION: '8.0.x'
  AZURE_WEBAPP_NAME: chinook-dashboard
  AZURE_RESOURCE_GROUP: chinook-rg
  PROJECT_PATH: 'ChinookDashboard/ChinookDashboard.csproj'
  TEST_PROJECT_PATH: 'ChinookDashboard.Tests/ChinookDashboard.Tests.csproj'

# Prevent concurrent deployments to same environment
concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: false

jobs:
  # ==========================================
  # BUILD
  # ==========================================
  build:
    name: Build Application
    runs-on: ubuntu-latest
    
    outputs:
      artifact-name: ${{ steps.set-artifact-name.outputs.name }}
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore dependencies
        run: dotnet restore

      - name: Build solution
        run: dotnet build --no-restore --configuration Release

      - name: Publish application
        run: |
          dotnet publish ${{ env.PROJECT_PATH }} \
            --no-build \
            --configuration Release \
            --output ./publish

      - name: Set artifact name
        id: set-artifact-name
        run: echo "name=app-${{ github.run_number }}-${{ github.sha }}" >> $GITHUB_OUTPUT

      - name: Upload application artifact
        uses: actions/upload-artifact@v4
        with:
          name: ${{ steps.set-artifact-name.outputs.name }}
          path: ./publish
          retention-days: 3

  # ==========================================
  # TEST
  # ==========================================
  test:
    name: Run Tests
    needs: build
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore test project
        run: dotnet restore ${{ env.TEST_PROJECT_PATH }}

      - name: Build test project
        run: dotnet build ${{ env.TEST_PROJECT_PATH }} --no-restore --configuration Release

      - name: Run unit tests
        run: |
          dotnet test ${{ env.TEST_PROJECT_PATH }} \
            --no-build \
            --configuration Release \
            --filter "FullyQualifiedName~Unit" \
            --logger "trx;LogFileName=unit-tests.trx" \
            --results-directory ./TestResults

      - name: Run integration tests
        run: |
          dotnet test ${{ env.TEST_PROJECT_PATH }} \
            --no-build \
            --configuration Release \
            --filter "FullyQualifiedName~Integration" \
            --logger "trx;LogFileName=integration-tests.trx" \
            --results-directory ./TestResults

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: ./TestResults/*.trx
          retention-days: 7

  # ==========================================
  # DEPLOY TO STAGING
  # ==========================================
  deploy-staging:
    name: Deploy to Staging
    needs: [build, test]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main' || github.ref == 'refs/heads/develop'
    environment:
      name: staging
      url: https://${{ env.AZURE_WEBAPP_NAME }}-staging.azurewebsites.net
    
    steps:
      - name: Download application artifact
        uses: actions/download-artifact@v4
        with:
          name: ${{ needs.build.outputs.artifact-name }}
          path: ./publish

      - name: Login to Azure
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Deploy to staging slot
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          slot-name: staging
          package: ./publish

      - name: Verify staging deployment
        run: |
          echo "Waiting for staging to be ready..."
          sleep 30
          
          HEALTH_URL="https://${{ env.AZURE_WEBAPP_NAME }}-staging.azurewebsites.net/health"
          HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)
          
          if [ $HTTP_STATUS -eq 200 ]; then
            echo "✓ Staging deployment healthy"
          else
            echo "✗ Staging health check failed: HTTP $HTTP_STATUS"
            exit 1
          fi

      - name: Logout from Azure
        if: always()
        run: az logout

  # ==========================================
  # DEPLOY TO PRODUCTION
  # ==========================================
  deploy-production:
    name: Deploy to Production
    needs: [build, deploy-staging]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    environment:
      name: production
      url: https://${{ env.AZURE_WEBAPP_NAME }}.azurewebsites.net
    
    steps:
      - name: Login to Azure
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Swap staging to production
        run: |
          az webapp deployment slot swap \
            --name ${{ env.AZURE_WEBAPP_NAME }} \
            --resource-group ${{ env.AZURE_RESOURCE_GROUP }} \
            --slot staging \
            --target-slot production

      - name: Verify production deployment
        run: |
          echo "Waiting for production to stabilize..."
          sleep 30
          
          HEALTH_URL="https://${{ env.AZURE_WEBAPP_NAME }}.azurewebsites.net/health"
          HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)
          
          if [ $HTTP_STATUS -eq 200 ]; then
            echo "✓ Production deployment healthy"
          else
            echo "✗ Production health check failed: HTTP $HTTP_STATUS"
            echo "Consider rolling back with slot swap"
            exit 1
          fi

      - name: Logout from Azure
        if: always()
        run: az logout

  # ==========================================
  # ROLLBACK (Manual trigger only)
  # ==========================================
  rollback:
    name: Rollback Production
    runs-on: ubuntu-latest
    if: github.event_name == 'workflow_dispatch'
    environment:
      name: production
    
    steps:
      - name: Login to Azure
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Swap production back to staging
        run: |
          echo "Rolling back production..."
          az webapp deployment slot swap \
            --name ${{ env.AZURE_WEBAPP_NAME }} \
            --resource-group ${{ env.AZURE_RESOURCE_GROUP }} \
            --slot production \
            --target-slot staging
          echo "✓ Rollback complete"

      - name: Logout from Azure
        if: always()
        run: az logout
```

### 23.4.5 Database Migrations in CI/CD

Apply database migrations during deployment using generated SQL scripts.

#### Migration Step in Build Job

Add migration script generation to the build job:

```yaml
# Add to the build job after 'Publish application' step

      - name: Install EF Core tools
        run: dotnet tool install --global dotnet-ef

      - name: Generate migration script
        run: |
          dotnet ef migrations script \
            --idempotent \
            --project ${{ env.PROJECT_PATH }} \
            --output ./publish/migrations.sql
        env:
          # Use a dummy connection string for script generation
          ConnectionStrings__ChinookConnection: "Server=.;Database=Dummy;Trusted_Connection=True;"

      - name: Upload migration script
        uses: actions/upload-artifact@v4
        with:
          name: migration-script
          path: ./publish/migrations.sql
          retention-days: 7
```

#### Migration Job

Add a separate job to apply migrations before deployment:

```yaml
  # Add after 'test' job, before 'deploy-staging'
  
  migrate-staging:
    name: Apply Migrations (Staging)
    needs: [build, test]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main' || github.ref == 'refs/heads/develop'
    environment: staging
    
    steps:
      - name: Download migration script
        uses: actions/download-artifact@v4
        with:
          name: migration-script
          path: ./migrations

      - name: Login to Azure
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Apply migrations to staging database
        run: |
          # Get connection string from Key Vault
          CONNECTION_STRING=$(az keyvault secret show \
            --vault-name chinook-vault \
            --name ChinookConnection \
            --query value -o tsv)
          
          # Extract server, database, user, password from connection string
          SERVER=$(echo $CONNECTION_STRING | grep -oP 'Server=tcp:\K[^,]+')
          DATABASE=$(echo $CONNECTION_STRING | grep -oP 'Initial Catalog=\K[^;]+')
          USER=$(echo $CONNECTION_STRING | grep -oP 'User ID=\K[^;]+')
          PASSWORD=$(echo $CONNECTION_STRING | grep -oP 'Password=\K[^;]+')
          
          # Apply migrations using sqlcmd
          sqlcmd -S $SERVER -d $DATABASE -U $USER -P "$PASSWORD" -i ./migrations/migrations.sql
          
          echo "✓ Migrations applied successfully"

      - name: Logout from Azure
        if: always()
        run: az logout
```

#### Rollback Considerations

For migration rollbacks, maintain down migration scripts or use point-in-time restore:

```bash
# Generate down migration script (if needed)
dotnet ef migrations script \
    CurrentMigration \
    PreviousMigration \
    --output rollback.sql

# Or use Azure SQL point-in-time restore
az sql db restore \
    --dest-name ChinookDb-Restored \
    --name ChinookDb \
    --resource-group chinook-rg \
    --server chinook-sql \
    --time "2024-01-15T10:00:00Z"
```

---

## 23.5 htmx-Specific Deployment Considerations

htmx applications have unique deployment considerations around caching, error handling, and health checks.

### 23.5.1 Caching Strategies for Partial Responses

Not all htmx responses should be cached. Consider the content type:

**Cache these (static or slowly changing):**
- Genre dropdown options
- Navigation menus
- Static lookup lists
- Rarely updated reference data

**Don't cache these:**
- User-specific content
- Search results
- Real-time data (stats, counts)
- Form submissions
- Anything with user context

#### HtmxResponseCachingMiddleware

**Middleware/HtmxResponseCachingMiddleware.cs**

```csharp
using Microsoft.Extensions.Options;

namespace ChinookDashboard.Middleware;

public class HtmxCacheOptions
{
    public Dictionary<string, CacheProfile> Profiles { get; set; } = new();
    public int DefaultMaxAge { get; set; } = 0;
    public bool VaryByHxRequest { get; set; } = true;
}

public class CacheProfile
{
    public int MaxAge { get; set; }
    public bool IsPrivate { get; set; }
    public bool NoStore { get; set; }
    public string[]? VaryByQueryKeys { get; set; }
}

public class HtmxResponseCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HtmxCacheOptions _options;
    private readonly ILogger<HtmxResponseCachingMiddleware> _logger;

    public HtmxResponseCachingMiddleware(
        RequestDelegate next,
        IOptions<HtmxCacheOptions> options,
        ILogger<HtmxResponseCachingMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process htmx requests
        if (!IsHtmxRequest(context))
        {
            await _next(context);
            return;
        }

        // Get cache profile for this handler
        var profile = GetCacheProfile(context);

        // Add Vary header for htmx requests
        if (_options.VaryByHxRequest)
        {
            context.Response.Headers.Append("Vary", "HX-Request");
        }

        if (profile != null)
        {
            SetCacheHeaders(context, profile);
        }
        else
        {
            // Default: no caching for htmx responses
            SetNoCacheHeaders(context);
        }

        await _next(context);
    }

    private static bool IsHtmxRequest(HttpContext context)
    {
        return context.Request.Headers.ContainsKey("HX-Request");
    }

    private CacheProfile? GetCacheProfile(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var handler = context.Request.Query["handler"].FirstOrDefault()?.ToLower();

        // Check for specific handler profiles
        if (!string.IsNullOrEmpty(handler))
        {
            var key = $"{path}:{handler}";
            if (_options.Profiles.TryGetValue(key, out var profile))
            {
                return profile;
            }
        }

        // Check for path-level profiles
        if (_options.Profiles.TryGetValue(path, out var pathProfile))
        {
            return pathProfile;
        }

        return null;
    }

    private void SetCacheHeaders(HttpContext context, CacheProfile profile)
    {
        if (profile.NoStore)
        {
            SetNoCacheHeaders(context);
            return;
        }

        var cacheControl = profile.IsPrivate ? "private" : "public";
        cacheControl += $", max-age={profile.MaxAge}";

        context.Response.Headers["Cache-Control"] = cacheControl;

        if (profile.VaryByQueryKeys?.Length > 0)
        {
            context.Response.Headers.Append("Vary", string.Join(", ", profile.VaryByQueryKeys));
        }

        _logger.LogDebug(
            "Set cache headers for htmx request: {Path}, Cache-Control: {CacheControl}",
            context.Request.Path,
            cacheControl);
    }

    private static void SetNoCacheHeaders(HttpContext context)
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
    }
}

public static class HtmxResponseCachingExtensions
{
    public static IServiceCollection AddHtmxResponseCaching(
        this IServiceCollection services,
        Action<HtmxCacheOptions> configure)
    {
        services.Configure(configure);
        return services;
    }

    public static IApplicationBuilder UseHtmxResponseCaching(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<HtmxResponseCachingMiddleware>();
    }
}
```

**Configuration in Program.cs:**

```csharp
// Configure htmx caching profiles
builder.Services.AddHtmxResponseCaching(options =>
{
    options.VaryByHxRequest = true;
    options.DefaultMaxAge = 0;

    // Cache genre list for 1 hour
    options.Profiles["/artists:genrelist"] = new CacheProfile
    {
        MaxAge = 3600,
        IsPrivate = false
    };

    // Cache navigation for 5 minutes
    options.Profiles["/shared:navigation"] = new CacheProfile
    {
        MaxAge = 300,
        IsPrivate = false
    };

    // Don't cache search results
    options.Profiles["/artists:list"] = new CacheProfile
    {
        NoStore = true
    };
});

// In middleware pipeline (after routing, before endpoints)
app.UseHtmxResponseCaching();
```

### 23.5.2 CDN Configuration for Static Assets

Serve static assets from Azure CDN for better global performance.

#### Azure CDN Setup Script

**infrastructure/setup-cdn.sh**

```bash
#!/bin/bash

# Configuration
RESOURCE_GROUP="chinook-rg"
CDN_PROFILE_NAME="chinook-cdn"
CDN_ENDPOINT_NAME="chinook-assets"
WEB_APP_NAME="chinook-dashboard"
LOCATION="eastus"

echo "Setting up Azure CDN..."

# Create CDN profile
az cdn profile create \
    --name $CDN_PROFILE_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku Standard_Microsoft \
    --output none

echo "✓ CDN profile created"

# Create CDN endpoint pointing to App Service
az cdn endpoint create \
    --name $CDN_ENDPOINT_NAME \
    --profile-name $CDN_PROFILE_NAME \
    --resource-group $RESOURCE_GROUP \
    --origin "${WEB_APP_NAME}.azurewebsites.net" \
    --origin-host-header "${WEB_APP_NAME}.azurewebsites.net" \
    --enable-compression true \
    --content-types-to-compress \
        "text/html" \
        "text/css" \
        "application/javascript" \
        "text/javascript" \
        "application/json" \
    --output none

echo "✓ CDN endpoint created"

# Add caching rules
az cdn endpoint rule add \
    --name $CDN_ENDPOINT_NAME \
    --profile-name $CDN_PROFILE_NAME \
    --resource-group $RESOURCE_GROUP \
    --order 1 \
    --rule-name "CacheStaticAssets" \
    --match-variable UrlFileExtension \
    --operator Contains \
    --match-values "js" "css" "woff2" "woff" "ttf" \
    --action-name CacheExpiration \
    --cache-behavior Override \
    --cache-duration "365.00:00:00" \
    --output none

echo "✓ Caching rule added for static assets"

# Add rule to bypass cache for htmx requests
az cdn endpoint rule add \
    --name $CDN_ENDPOINT_NAME \
    --profile-name $CDN_PROFILE_NAME \
    --resource-group $RESOURCE_GROUP \
    --order 2 \
    --rule-name "BypassHtmxRequests" \
    --match-variable RequestHeader \
    --selector "HX-Request" \
    --operator Any \
    --action-name CacheExpiration \
    --cache-behavior BypassCache \
    --output none

echo "✓ Bypass rule added for htmx requests"

# Output CDN URL
CDN_URL="https://${CDN_ENDPOINT_NAME}.azureedge.net"
echo ""
echo "CDN setup complete!"
echo "CDN URL: $CDN_URL"
echo ""
echo "Update your application to use CDN for static assets:"
echo "  <script src=\"$CDN_URL/lib/htmx/htmx.min.js\"></script>"
```

#### Cache Invalidation

Purge CDN cache after deployments:

```bash
# Purge specific paths
az cdn endpoint purge \
    --name chinook-assets \
    --profile-name chinook-cdn \
    --resource-group chinook-rg \
    --content-paths "/css/*" "/js/*" "/lib/*"

# Or purge everything
az cdn endpoint purge \
    --name chinook-assets \
    --profile-name chinook-cdn \
    --resource-group chinook-rg \
    --content-paths "/*"
```

Add to deployment workflow:

```yaml
      - name: Purge CDN cache
        run: |
          az cdn endpoint purge \
            --name chinook-assets \
            --profile-name chinook-cdn \
            --resource-group ${{ env.AZURE_RESOURCE_GROUP }} \
            --content-paths "/css/*" "/js/*" "/lib/*" \
            --no-wait
```

### 23.5.3 Health Checks and Monitoring

Health checks enable Azure to detect unhealthy instances and route traffic appropriately.

#### DatabaseHealthCheck Implementation

**Health/DatabaseHealthCheck.cs**

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using ChinookDashboard.Data;

namespace ChinookDashboard.Health;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ChinookContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        ChinookContext context,
        ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple connectivity check
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                _logger.LogWarning("Database health check failed: Cannot connect");
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // Query check - verify we can read data
            var artistCount = await _context.Artists
                .CountAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "artistCount", artistCount },
                { "timestamp", DateTime.UtcNow }
            };

            _logger.LogDebug("Database health check passed: {ArtistCount} artists", artistCount);

            return HealthCheckResult.Healthy("Database is responsive", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check exception");

            return HealthCheckResult.Unhealthy(
                "Database check failed",
                exception: ex);
        }
    }
}
```

#### Complete Health Check Registration

**Health/HealthCheckExtensions.cs**

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace ChinookDashboard.Health;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddChinookHealthChecks(
        this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "sql" })
            .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"),
                tags: new[] { "self" });

        return services;
    }

    public static IApplicationBuilder UseChinookHealthChecks(
        this IApplicationBuilder app)
    {
        // Simple health endpoint for load balancers
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("self"),
            ResponseWriter = WriteMinimalResponse
        });

        // Detailed health endpoint for monitoring
        app.UseHealthChecks("/health/detail", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedResponse
        });

        // htmx-compatible health endpoint
        app.UseHealthChecks("/health/htmx", new HealthCheckOptions
        {
            ResponseWriter = WriteHtmxResponse
        });

        return app;
    }

    private static Task WriteMinimalResponse(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "text/plain";
        return context.Response.WriteAsync(report.Status.ToString());
    }

    private static async Task WriteDetailedResponse(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                data = e.Value.Data,
                exception = e.Value.Exception?.Message
            })
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
    }

    private static async Task WriteHtmxResponse(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "text/html";

        var statusClass = report.Status switch
        {
            HealthStatus.Healthy => "text-green-600",
            HealthStatus.Degraded => "text-yellow-600",
            _ => "text-red-600"
        };

        var html = $@"
<div id=""health-status"" class=""p-4 rounded border"">
    <div class=""flex items-center gap-2 mb-2"">
        <span class=""font-bold {statusClass}"">{report.Status}</span>
        <span class=""text-gray-500 text-sm"">({report.TotalDuration.TotalMilliseconds:F0}ms)</span>
    </div>
    <ul class=""text-sm space-y-1"">
        {string.Join("\n", report.Entries.Select(e => $@"
        <li class=""flex justify-between"">
            <span>{e.Key}</span>
            <span class=""{GetStatusClass(e.Value.Status)}"">{e.Value.Status}</span>
        </li>"))}
    </ul>
    <div class=""text-xs text-gray-400 mt-2"">
        Last checked: {DateTime.UtcNow:HH:mm:ss} UTC
    </div>
</div>";

        await context.Response.WriteAsync(html);
    }

    private static string GetStatusClass(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "text-green-600",
        HealthStatus.Degraded => "text-yellow-600",
        _ => "text-red-600"
    };
}
```

**Registration in Program.cs:**

```csharp
// Services
builder.Services.AddChinookHealthChecks();

// Middleware (after routing)
app.UseChinookHealthChecks();
```

### 23.5.4 Error Handling in Production

Production error handling should return appropriate responses for htmx requests versus full page requests.

#### ProductionExceptionMiddleware

**Middleware/ProductionExceptionMiddleware.cs**

```csharp
using System.Net;

namespace ChinookDashboard.Middleware;

public class ProductionExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProductionExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ProductionExceptionMiddleware(
        RequestDelegate next,
        ILogger<ProductionExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // Handle status code errors (404, etc.)
            if (context.Response.StatusCode >= 400 && !context.Response.HasStarted)
            {
                await HandleStatusCodeAsync(context);
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.TraceIdentifier;
        var isHtmxRequest = context.Request.Headers.ContainsKey("HX-Request");

        _logger.LogError(
            exception,
            "Unhandled exception. RequestId: {RequestId}, Path: {Path}, IsHtmx: {IsHtmx}",
            requestId,
            context.Request.Path,
            isHtmxRequest);

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        if (isHtmxRequest)
        {
            await WriteHtmxErrorAsync(context, 500, requestId);
        }
        else
        {
            await WriteFullPageErrorAsync(context, 500, requestId);
        }
    }

    private async Task HandleStatusCodeAsync(HttpContext context)
    {
        var isHtmxRequest = context.Request.Headers.ContainsKey("HX-Request");
        var statusCode = context.Response.StatusCode;
        var requestId = context.TraceIdentifier;

        if (isHtmxRequest)
        {
            await WriteHtmxErrorAsync(context, statusCode, requestId);
        }
        else if (statusCode == 404)
        {
            await WriteFullPageErrorAsync(context, 404, requestId);
        }
    }

    private async Task WriteHtmxErrorAsync(
        HttpContext context,
        int statusCode,
        string requestId)
    {
        context.Response.ContentType = "text/html; charset=utf-8";

        // Set htmx headers to show error in appropriate location
        context.Response.Headers["HX-Retarget"] = "#error-container";
        context.Response.Headers["HX-Reswap"] = "innerHTML";

        var (title, message) = GetErrorContent(statusCode);
        var showDetails = _environment.IsDevelopment();

        var html = $@"
<div class=""bg-red-50 border border-red-200 rounded-lg p-4 my-4"" role=""alert"">
    <div class=""flex items-start gap-3"">
        <svg class=""w-5 h-5 text-red-600 mt-0.5"" fill=""currentColor"" viewBox=""0 0 20 20"">
            <path fill-rule=""evenodd"" d=""M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"" clip-rule=""evenodd""/>
        </svg>
        <div class=""flex-1"">
            <h3 class=""font-medium text-red-800"">{title}</h3>
            <p class=""text-sm text-red-700 mt-1"">{message}</p>
            {(showDetails ? $"<p class=\"text-xs text-red-500 mt-2\">Request ID: {requestId}</p>" : "")}
        </div>
        <button type=""button"" 
                class=""text-red-500 hover:text-red-700"" 
                onclick=""this.closest('[role=alert]').remove()"">
            <svg class=""w-4 h-4"" fill=""currentColor"" viewBox=""0 0 20 20"">
                <path fill-rule=""evenodd"" d=""M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"" clip-rule=""evenodd""/>
            </svg>
        </button>
    </div>
</div>";

        await context.Response.WriteAsync(html);
    }

    private async Task WriteFullPageErrorAsync(
        HttpContext context,
        int statusCode,
        string requestId)
    {
        context.Response.ContentType = "text/html; charset=utf-8";

        var (title, message) = GetErrorContent(statusCode);
        var showDetails = _environment.IsDevelopment();

        var html = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{title} - Chinook Dashboard</title>
    <link rel=""stylesheet"" href=""/css/site.min.css"" />
</head>
<body class=""bg-gray-50 min-h-screen flex items-center justify-center"">
    <div class=""max-w-md mx-auto text-center p-8"">
        <div class=""text-6xl font-bold text-gray-300 mb-4"">{statusCode}</div>
        <h1 class=""text-2xl font-bold text-gray-800 mb-2"">{title}</h1>
        <p class=""text-gray-600 mb-6"">{message}</p>
        <a href=""/"" class=""inline-flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"">
            <svg class=""w-4 h-4"" fill=""none"" stroke=""currentColor"" viewBox=""0 0 24 24"">
                <path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""2"" d=""M10 19l-7-7m0 0l7-7m-7 7h18""/>
            </svg>
            Back to Home
        </a>
        {(showDetails ? $"<p class=\"text-xs text-gray-400 mt-8\">Request ID: {requestId}</p>" : "")}
    </div>
</body>
</html>";

        await context.Response.WriteAsync(html);
    }

    private static (string Title, string Message) GetErrorContent(int statusCode)
    {
        return statusCode switch
        {
            400 => ("Bad Request", "The request could not be understood. Please check your input and try again."),
            401 => ("Unauthorized", "You need to sign in to access this resource."),
            403 => ("Forbidden", "You don't have permission to access this resource."),
            404 => ("Not Found", "The page you're looking for doesn't exist or has been moved."),
            408 => ("Request Timeout", "The request took too long to complete. Please try again."),
            500 => ("Server Error", "Something went wrong on our end. We've been notified and are working on it."),
            502 => ("Bad Gateway", "The server received an invalid response. Please try again later."),
            503 => ("Service Unavailable", "The service is temporarily unavailable. Please try again later."),
            _ => ("Error", "An unexpected error occurred. Please try again.")
        };
    }
}

public static class ProductionExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseProductionExceptionHandler(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<ProductionExceptionMiddleware>();
    }
}
```

#### Error Partial Views

For more complex error templates, use Razor partial views:

**Pages/Shared/_Error404.cshtml**

```html
@{
    Layout = null;
}
<div class="bg-yellow-50 border border-yellow-200 rounded-lg p-4" role="alert">
    <div class="flex items-center gap-3">
        <svg class="w-5 h-5 text-yellow-600" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
        </svg>
        <div>
            <h3 class="font-medium text-yellow-800">Not Found</h3>
            <p class="text-sm text-yellow-700">The requested resource could not be found.</p>
        </div>
    </div>
</div>
```

**Pages/Shared/_Error500.cshtml**

```html
@{
    Layout = null;
}
<div class="bg-red-50 border border-red-200 rounded-lg p-4" role="alert">
    <div class="flex items-center gap-3">
        <svg class="w-5 h-5 text-red-600" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
        </svg>
        <div>
            <h3 class="font-medium text-red-800">Server Error</h3>
            <p class="text-sm text-red-700">Something went wrong. Please try again later.</p>
        </div>
    </div>
</div>
```

**Registration in Program.cs:**

```csharp
// Use production exception handler early in pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseProductionExceptionHandler();
}
else
{
    app.UseDeveloperExceptionPage();
}
```

#### Error Container in Layout

Add an error container to your layout for htmx error responses:

```html
<!-- In _Layout.cshtml, before main content -->
<div id="error-container" class="container mx-auto px-4">
    <!-- htmx errors will be swapped here -->
</div>

<main class="container mx-auto px-4 py-8">
    @RenderBody()
</main>
```

This ensures htmx error responses have a consistent location to display, while maintaining the ability to show inline errors when appropriate.

## 23.6 Monitoring and Troubleshooting

Production applications need visibility into performance, errors, and usage patterns. Azure Application Insights provides deep monitoring for ASP.NET Core applications.

### 23.6.1 Application Insights Setup

Install the Application Insights package:

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

**Basic Configuration in Program.cs:**

```csharp
// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    options.EnableAdaptiveSampling = true;
    options.EnableQuickPulseMetricStream = true; // Live metrics
});
```

#### HtmxTelemetryMiddleware

Track htmx-specific properties for better insights into partial request patterns.

**Middleware/HtmxTelemetryMiddleware.cs**

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;

namespace ChinookDashboard.Middleware;

public class HtmxTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<HtmxTelemetryMiddleware> _logger;

    public HtmxTelemetryMiddleware(
        RequestDelegate next,
        TelemetryClient telemetryClient,
        ILogger<HtmxTelemetryMiddleware> logger)
    {
        _next = next;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isHtmxRequest = context.Request.Headers.ContainsKey("HX-Request");
        var stopwatch = Stopwatch.StartNew();

        // Extract htmx headers
        var htmxTarget = context.Request.Headers["HX-Target"].FirstOrDefault();
        var htmxTrigger = context.Request.Headers["HX-Trigger"].FirstOrDefault();
        var htmxTriggerName = context.Request.Headers["HX-Trigger-Name"].FirstOrDefault();
        var htmxCurrentUrl = context.Request.Headers["HX-Current-URL"].FirstOrDefault();
        var handler = context.Request.Query["handler"].FirstOrDefault();

        // Add properties to the current request telemetry
        var requestTelemetry = context.Features.Get<RequestTelemetry>();
        if (requestTelemetry != null)
        {
            requestTelemetry.Properties["IsHtmxRequest"] = isHtmxRequest.ToString();
            requestTelemetry.Properties["RequestType"] = isHtmxRequest ? "htmx-partial" : "full-page";

            if (!string.IsNullOrEmpty(htmxTarget))
                requestTelemetry.Properties["HX-Target"] = htmxTarget;

            if (!string.IsNullOrEmpty(htmxTrigger))
                requestTelemetry.Properties["HX-Trigger"] = htmxTrigger;

            if (!string.IsNullOrEmpty(htmxTriggerName))
                requestTelemetry.Properties["HX-Trigger-Name"] = htmxTriggerName;

            if (!string.IsNullOrEmpty(handler))
                requestTelemetry.Properties["Handler"] = handler;
        }

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Track custom metrics for htmx requests
            if (isHtmxRequest)
            {
                var metricName = $"HtmxResponse/{handler ?? "default"}";
                
                _telemetryClient.TrackMetric(new MetricTelemetry
                {
                    Name = "HtmxResponseTime",
                    Sum = stopwatch.ElapsedMilliseconds,
                    Count = 1,
                    Properties =
                    {
                        ["Handler"] = handler ?? "none",
                        ["Target"] = htmxTarget ?? "none",
                        ["StatusCode"] = context.Response.StatusCode.ToString()
                    }
                });

                // Track response size for htmx responses
                if (context.Response.ContentLength.HasValue)
                {
                    _telemetryClient.TrackMetric(new MetricTelemetry
                    {
                        Name = "HtmxResponseSize",
                        Sum = context.Response.ContentLength.Value,
                        Count = 1,
                        Properties =
                        {
                            ["Handler"] = handler ?? "none"
                        }
                    });
                }
            }

            // Log slow requests
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning(
                    "Slow request: {Path} took {Duration}ms (htmx: {IsHtmx}, handler: {Handler})",
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds,
                    isHtmxRequest,
                    handler);
            }
        }
    }
}

public static class HtmxTelemetryMiddlewareExtensions
{
    public static IApplicationBuilder UseHtmxTelemetry(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HtmxTelemetryMiddleware>();
    }
}
```

**Registration in Program.cs:**

```csharp
// After UseRouting, before UseEndpoints
app.UseHtmxTelemetry();
```

### 23.6.2 Logging Best Practices

Serilog provides structured logging with multiple sinks for different environments.

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.ApplicationInsights
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Expressions
```

**Complete Serilog Configuration in Program.cs:**

```csharp
using Serilog;
using Serilog.Events;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithProperty("Application", "ChinookDashboard"));

    // ... rest of configuration

    var app = builder.Build();

    // Add request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("IsHtmxRequest", httpContext.Request.Headers.ContainsKey("HX-Request"));
            
            var handler = httpContext.Request.Query["handler"].FirstOrDefault();
            if (!string.IsNullOrEmpty(handler))
                diagnosticContext.Set("Handler", handler);
        };
        
        // Don't log health checks
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (httpContext.Request.Path.StartsWithSegments("/health"))
                return LogEventLevel.Verbose;
            
            return elapsed > 1000 ? LogEventLevel.Warning : LogEventLevel.Information;
        };
    });

    // ... rest of app configuration

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

**appsettings.Production.json Serilog Section:**

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.ApplicationInsights",
      "Serilog.Sinks.File"
    ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "ApplicationInsights",
        "Args": {
          "connectionString": "SET_IN_AZURE",
          "telemetryConverter": "Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter, Serilog.Sinks.ApplicationInsights"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "/home/LogFiles/Application/chinook-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Filter": [
      {
        "Name": "ByExcluding",
        "Args": {
          "expression": "RequestPath like '/health%'"
        }
      },
      {
        "Name": "ByExcluding",
        "Args": {
          "expression": "@m like '%password%' or @m like '%token%' or @m like '%secret%'"
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithEnvironmentName"
    ]
  }
}
```

### 23.6.3 Performance Monitoring

#### Key Metrics for htmx Applications

Track these metrics in Application Insights:

- **Response Time**: Average and P95 for htmx vs full page requests
- **Throughput**: Requests per second by handler
- **Error Rate**: Failed requests percentage
- **Response Size**: Average partial response size

#### Custom Application Insights Queries

Query htmx-specific performance data in Log Analytics:

```kusto
// Average response time by handler for htmx requests
requests
| where customDimensions.IsHtmxRequest == "True"
| summarize 
    AvgDuration = avg(duration),
    P95Duration = percentile(duration, 95),
    RequestCount = count()
    by Handler = tostring(customDimensions.Handler)
| order by RequestCount desc

// Slow htmx requests (> 500ms)
requests
| where customDimensions.IsHtmxRequest == "True"
| where duration > 500
| project 
    timestamp,
    name,
    duration,
    Handler = customDimensions.Handler,
    Target = customDimensions["HX-Target"],
    resultCode
| order by duration desc

// htmx vs full page request comparison
requests
| summarize 
    AvgDuration = avg(duration),
    Count = count()
    by RequestType = tostring(customDimensions.RequestType)

// Error rate by handler
requests
| where customDimensions.IsHtmxRequest == "True"
| summarize 
    TotalRequests = count(),
    FailedRequests = countif(toint(resultCode) >= 400)
    by Handler = tostring(customDimensions.Handler)
| extend ErrorRate = (FailedRequests * 100.0) / TotalRequests
| order by ErrorRate desc
```

#### Azure Monitor Alerts

Create alerts for critical conditions:

```bash
# Alert for high error rate
az monitor metrics alert create \
    --name "HighErrorRate" \
    --resource-group chinook-rg \
    --scopes "/subscriptions/{sub}/resourceGroups/chinook-rg/providers/Microsoft.Web/sites/chinook-dashboard" \
    --condition "avg requests/failed > 10" \
    --window-size 5m \
    --evaluation-frequency 1m \
    --action-group chinook-alerts \
    --description "High error rate detected"

# Alert for slow response times
az monitor metrics alert create \
    --name "SlowResponses" \
    --resource-group chinook-rg \
    --scopes "/subscriptions/{sub}/resourceGroups/chinook-rg/providers/Microsoft.Web/sites/chinook-dashboard" \
    --condition "avg HttpResponseTime > 2000" \
    --window-size 5m \
    --evaluation-frequency 1m \
    --action-group chinook-alerts \
    --description "Response times exceeding 2 seconds"
```

### 23.6.4 Debugging Production Issues

#### Azure Kudu Console

Access the Kudu console at `https://{app-name}.scm.azurewebsites.net`:

- **Debug Console**: Browse files, run commands
- **Process Explorer**: View running processes
- **Log Stream**: Real-time log viewing
- **Environment**: Check environment variables

#### Log Streaming via CLI

```bash
# Stream logs in real-time
az webapp log tail \
    --name chinook-dashboard \
    --resource-group chinook-rg

# Download logs
az webapp log download \
    --name chinook-dashboard \
    --resource-group chinook-rg \
    --log-file logs.zip
```

#### Common htmx Deployment Issues

**Anti-forgery Token Failures:**
```
System.InvalidOperationException: The antiforgery token could not be decrypted.
```
Solution: Configure data protection key storage (see Section 23.8.2).

**Missing Partial Views (Linux case sensitivity):**
```
InvalidOperationException: The view 'Artists/_ArtistRow' was not found.
```
Solution: Ensure file names match exactly, including case. `_artistRow.cshtml` won't be found if code references `_ArtistRow`.

**CORS Issues with CDN:**
```
Access to fetch at 'https://cdn...' from origin 'https://app...' has been blocked by CORS policy
```
Solution: Configure CORS headers on CDN or serve htmx responses from the origin.

#### Troubleshooting Checklist

| Issue | Check | Solution |
|-------|-------|----------|
| 500 errors | Application Insights exceptions | Review stack trace, fix code |
| Anti-forgery failures | Data protection key storage | Configure Azure Blob Storage for keys |
| Slow responses | Application Insights dependencies | Optimize database queries |
| Missing views | File names, case sensitivity | Match exact casing on Linux |
| htmx not working | Browser console, network tab | Check for JavaScript errors |
| SSL errors | Certificate binding | Verify SSL certificate status |

---

## 23.7 Scaling and Performance

### 23.7.1 Horizontal Scaling

Scale out to multiple instances for high traffic.

#### Auto-Scale Configuration

```bash
# Enable auto-scale
az monitor autoscale create \
    --resource-group chinook-rg \
    --name chinook-autoscale \
    --resource "/subscriptions/{sub}/resourceGroups/chinook-rg/providers/Microsoft.Web/serverFarms/chinook-plan" \
    --min-count 1 \
    --max-count 5 \
    --count 1

# Add scale-out rule (CPU > 70%)
az monitor autoscale rule create \
    --resource-group chinook-rg \
    --autoscale-name chinook-autoscale \
    --condition "CpuPercentage > 70 avg 5m" \
    --scale out 1

# Add scale-in rule (CPU < 30%)
az monitor autoscale rule create \
    --resource-group chinook-rg \
    --autoscale-name chinook-autoscale \
    --condition "CpuPercentage < 30 avg 10m" \
    --scale in 1

# Add memory-based rule
az monitor autoscale rule create \
    --resource-group chinook-rg \
    --autoscale-name chinook-autoscale \
    --condition "MemoryPercentage > 80 avg 5m" \
    --scale out 1
```

#### Session Affinity for htmx

htmx applications are typically stateless, making session affinity unnecessary. Disable it for better load distribution:

```bash
# Disable session affinity (ARR Affinity)
az webapp config set \
    --name chinook-dashboard \
    --resource-group chinook-rg \
    --generic-configurations '{"clientAffinityEnabled": false}'
```

Enable session affinity only if your application stores session state in memory (not recommended for production).

### 23.7.2 Vertical Scaling

| Tier | vCPUs | Memory | Best For |
|------|-------|--------|----------|
| B1 | 1 | 1.75 GB | Dev/Test |
| P1v3 | 2 | 8 GB | Small production |
| P2v3 | 4 | 16 GB | Medium production |
| P3v3 | 8 | 32 GB | High traffic |

```bash
# Scale up to P2v3
az appservice plan update \
    --name chinook-plan \
    --resource-group chinook-rg \
    --sku P2V3
```

#### Load Testing with k6

**loadtest.js:**

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '30s', target: 20 },  // Ramp up
        { duration: '1m', target: 20 },   // Stay at 20 users
        { duration: '30s', target: 50 },  // Ramp to 50
        { duration: '1m', target: 50 },   // Stay at 50
        { duration: '30s', target: 0 },   // Ramp down
    ],
};

const BASE_URL = 'https://chinook-dashboard.azurewebsites.net';

export default function () {
    // Full page load
    let res = http.get(`${BASE_URL}/Artists`);
    check(res, { 'page loaded': (r) => r.status === 200 });

    sleep(1);

    // htmx search request
    res = http.get(`${BASE_URL}/Artists?handler=List&search=AC`, {
        headers: {
            'HX-Request': 'true',
            'HX-Target': 'artist-list',
        },
    });
    check(res, { 'htmx search ok': (r) => r.status === 200 });

    sleep(0.5);

    // htmx edit form
    res = http.get(`${BASE_URL}/Artists?handler=Edit&id=1`, {
        headers: {
            'HX-Request': 'true',
            'HX-Target': 'artist-row-1',
        },
    });
    check(res, { 'htmx edit ok': (r) => r.status === 200 });

    sleep(1);
}
```

Run with: `k6 run loadtest.js`

### 23.7.3 Output Caching

ASP.NET Core 7+ includes built-in output caching.

**Output Caching Configuration:**

```csharp
// In Program.cs
builder.Services.AddOutputCache(options =>
{
    // Default policy: no caching
    options.AddBasePolicy(builder => builder.NoCache());

    // Cache static lookups for 1 hour
    options.AddPolicy("StaticLookup", builder => builder
        .Expire(TimeSpan.FromHours(1))
        .SetVaryByHeader("HX-Request")
        .Tag("lookup"));

    // Cache search results for 5 minutes, vary by search term
    options.AddPolicy("SearchResults", builder => builder
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByQuery("search", "page")
        .SetVaryByHeader("HX-Request")
        .Tag("search"));

    // No cache for user-specific content
    options.AddPolicy("NoCache", builder => builder.NoCache());
});

// For multi-instance, use Redis
builder.Services.AddStackExchangeRedisOutputCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "chinook:";
});

// In middleware pipeline
app.UseOutputCache();
```

**Applying Cache Policies to Handlers:**

```csharp
// In page model
[OutputCache(PolicyName = "StaticLookup")]
public async Task<IActionResult> OnGetGenreListAsync()
{
    var genres = await _genreService.GetAllAsync();
    return Partial("_GenreList", genres);
}

[OutputCache(PolicyName = "SearchResults")]
public async Task<IActionResult> OnGetListAsync(string? search, int page = 1)
{
    var artists = await _artistService.SearchAsync(search, page);
    return Partial("_ArtistList", artists);
}

[OutputCache(PolicyName = "NoCache")]
public async Task<IActionResult> OnGetEditAsync(int id)
{
    var artist = await _artistService.GetByIdAsync(id);
    return Partial("_ArtistEditForm", artist);
}
```

**Redis Cache Configuration:**

```bash
# Create Azure Cache for Redis
az redis create \
    --name chinook-cache \
    --resource-group chinook-rg \
    --location eastus \
    --sku Basic \
    --vm-size c0

# Get connection string
az redis list-keys \
    --name chinook-cache \
    --resource-group chinook-rg
```

### 23.7.4 Database Performance

#### Azure SQL Recommendations

- Enable **Query Store** for performance analysis
- Use **Automatic Tuning** for index recommendations
- Configure **Read Scale-Out** for read-heavy workloads

```bash
# Enable Query Store
az sql db update \
    --name ChinookDb \
    --resource-group chinook-rg \
    --server chinook-sql \
    --set requestedServiceObjectiveName="S1"

# Enable automatic tuning
az sql db update \
    --name ChinookDb \
    --resource-group chinook-rg \
    --server chinook-sql \
    --set automaticTuning="Auto"
```

#### Connection Resiliency

Already configured in Program.cs with `EnableRetryOnFailure`. For htmx endpoints with many small queries, connection pooling is critical:

```csharp
// In connection string
"Max Pool Size=100;Min Pool Size=10;Connection Timeout=30;"
```

---

## 23.8 Security Hardening

### 23.8.1 Security Headers

htmx requires specific CSP configuration to allow inline event handlers and dynamic content loading.

**Middleware/SecurityHeadersMiddleware.cs**

```csharp
namespace ChinookDashboard.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Content Security Policy - htmx compatible
        var csp = BuildContentSecurityPolicy();
        context.Response.Headers["Content-Security-Policy"] = csp;

        // Prevent MIME type sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Clickjacking protection
        context.Response.Headers["X-Frame-Options"] = _options.AllowFraming ? "SAMEORIGIN" : "DENY";

        // Referrer policy
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Permissions policy (formerly Feature-Policy)
        context.Response.Headers["Permissions-Policy"] = 
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

        // Remove server header
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        await _next(context);
    }

    private string BuildContentSecurityPolicy()
    {
        var directives = new List<string>
        {
            // Default: only same origin
            "default-src 'self'",

            // Scripts: self + htmx + hyperscript + unsafe-inline for htmx attributes
            // In production, consider using nonces instead of unsafe-inline
            $"script-src 'self' {(_options.CdnDomain != null ? _options.CdnDomain : "")} 'unsafe-inline' 'unsafe-eval'",

            // Styles: self + inline styles for htmx indicators
            $"style-src 'self' {(_options.CdnDomain != null ? _options.CdnDomain : "")} 'unsafe-inline'",

            // Images: self + data URIs + CDN
            $"img-src 'self' data: {(_options.CdnDomain != null ? _options.CdnDomain : "")}",

            // Fonts: self + CDN
            $"font-src 'self' {(_options.CdnDomain != null ? _options.CdnDomain : "")}",

            // Connect: same origin for htmx requests
            "connect-src 'self'",

            // Forms: same origin
            "form-action 'self'",

            // Frame ancestors: prevent embedding
            "frame-ancestors 'none'",

            // Base URI: same origin
            "base-uri 'self'",

            // Object/embed: none
            "object-src 'none'"
        };

        return string.Join("; ", directives);
    }
}

public class SecurityHeadersOptions
{
    public bool AllowFraming { get; set; } = false;
    public string? CdnDomain { get; set; }
    public bool UseStrictCsp { get; set; } = false;
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IServiceCollection AddSecurityHeaders(
        this IServiceCollection services,
        Action<SecurityHeadersOptions>? configure = null)
    {
        var options = new SecurityHeadersOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        return services;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetService<SecurityHeadersOptions>() 
            ?? new SecurityHeadersOptions();
        return app.UseMiddleware<SecurityHeadersMiddleware>(options);
    }
}
```

**Configuration in Program.cs:**

```csharp
// Services
builder.Services.AddSecurityHeaders(options =>
{
    options.CdnDomain = "https://chinook-assets.azureedge.net";
    options.AllowFraming = false;
});

// Middleware (early in pipeline)
if (!app.Environment.IsDevelopment())
{
    app.UseSecurityHeaders();
    app.UseHsts();
}
```

### 23.8.2 Anti-Forgery in Production

In multi-instance deployments, data protection keys must be shared across instances.

**Data Protection Configuration:**

```csharp
// In Program.cs
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;

// Configure data protection for production
if (builder.Environment.IsProduction())
{
    var blobServiceClient = new BlobServiceClient(
        new Uri("https://chinookstorage.blob.core.windows.net"),
        new DefaultAzureCredential());

    var containerClient = blobServiceClient.GetBlobContainerClient("data-protection-keys");

    builder.Services.AddDataProtection()
        .SetApplicationName("ChinookDashboard")
        .PersistKeysToAzureBlobStorage(containerClient, "keys.xml")
        .ProtectKeysWithAzureKeyVault(
            new Uri("https://chinook-vault.vault.azure.net/keys/DataProtectionKey"),
            new DefaultAzureCredential());
}
else
{
    builder.Services.AddDataProtection()
        .SetApplicationName("ChinookDashboard");
}
```

**Azure Resource Setup:**

```bash
# Create storage account for data protection keys
az storage account create \
    --name chinookstorage \
    --resource-group chinook-rg \
    --location eastus \
    --sku Standard_LRS

# Create container
az storage container create \
    --name data-protection-keys \
    --account-name chinookstorage \
    --auth-mode login

# Create Key Vault key for encryption
az keyvault key create \
    --vault-name chinook-vault \
    --name DataProtectionKey \
    --protection software

# Grant Web App access to storage
WEBAPP_IDENTITY=$(az webapp identity show --name chinook-dashboard --resource-group chinook-rg --query principalId -o tsv)

az role assignment create \
    --assignee $WEBAPP_IDENTITY \
    --role "Storage Blob Data Contributor" \
    --scope "/subscriptions/{sub}/resourceGroups/chinook-rg/providers/Microsoft.Storage/storageAccounts/chinookstorage"

# Grant access to Key Vault key
az keyvault set-policy \
    --name chinook-vault \
    --object-id $WEBAPP_IDENTITY \
    --key-permissions get unwrapKey wrapKey
```

### 23.8.3 Rate Limiting

Protect htmx endpoints from abuse with ASP.NET Core rate limiting.

```csharp
// In Program.cs
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    // Global rate limit
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            });
    });

    // Stricter limit for search (htmx debounced, but protect against abuse)
    options.AddPolicy("search", context =>
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            });
    });

    // Very strict for write operations
    options.AddPolicy("write", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            });
    });

    // Custom response for htmx requests
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.HttpContext.Request.Headers.ContainsKey("HX-Request"))
        {
            context.HttpContext.Response.ContentType = "text/html";
            await context.HttpContext.Response.WriteAsync(
                "<div class=\"text-red-600\">Too many requests. Please slow down.</div>",
                cancellationToken);
        }
        else
        {
            await context.HttpContext.Response.WriteAsync(
                "Rate limit exceeded. Please try again later.",
                cancellationToken);
        }
    };
});

// In middleware pipeline (after routing)
app.UseRateLimiter();
```

**Applying Rate Limits to Handlers:**

```csharp
// In page model
[EnableRateLimiting("search")]
public async Task<IActionResult> OnGetListAsync(string? search)
{
    // ...
}

[EnableRateLimiting("write")]
public async Task<IActionResult> OnPostCreateAsync()
{
    // ...
}
```

---

## 23.9 Cost Optimization

### 23.9.1 Right-Sizing Resources

Monitor actual usage before choosing tiers:

```bash
# View App Service metrics
az monitor metrics list \
    --resource "/subscriptions/{sub}/resourceGroups/chinook-rg/providers/Microsoft.Web/sites/chinook-dashboard" \
    --metric "CpuPercentage,MemoryPercentage" \
    --interval PT1H \
    --start-time 2024-01-01T00:00:00Z

# Get Azure Advisor recommendations
az advisor recommendation list \
    --resource-group chinook-rg \
    --category cost
```

### 23.9.2 Cost-Saving Strategies

| Strategy | Savings | Implementation |
|----------|---------|----------------|
| Auto-shutdown dev/test | 60-70% | Schedule via Azure Automation |
| Reserved Instances (1yr) | 30-40% | Commit to production workloads |
| Reserved Instances (3yr) | 50-60% | Long-term stable workloads |
| Azure Hybrid Benefit | 40% | Use existing Windows licenses |
| Blob Storage for static | Variable | Offload from App Service |
| Right-size SQL tier | 20-50% | Monitor DTU usage |

**Auto-Shutdown for Dev/Test:**

```bash
# Create automation account and schedule
# Or use built-in App Service feature for dev/test slots

# Stop staging slot during off-hours
az webapp stop \
    --name chinook-dashboard \
    --resource-group chinook-rg \
    --slot staging
```

**Estimated Monthly Costs (East US):**

| Component | Dev/Test | Production |
|-----------|----------|------------|
| App Service (B1/P1v3) | $13 | $145 |
| SQL Database (S0/S1) | $15 | $30 |
| Key Vault | $0.03/operation | $0.03/operation |
| Application Insights | Free tier | $2.30/GB |
| CDN | $0.08/GB | $0.08/GB |
| **Total (est.)** | **~$30** | **~$200** |

---

## 23.10 Summary

Deploying htmx applications to Azure requires attention to configuration, security, monitoring, and performance. This chapter covered the complete deployment lifecycle.

### Deployment Checklist

| Category | Item | Notes |
|----------|------|-------|
| **Configuration** | appsettings.Production.json | Environment-specific settings |
| **Configuration** | Connection strings in Key Vault | Never in source control |
| **Configuration** | Logging levels set | Warning for framework, Info for app |
| **Configuration** | Data protection keys configured | Azure Blob + Key Vault |
| **Assets** | Static files minified | htmx.min.js, site.min.css |
| **Assets** | Cache headers configured | 1 year for fingerprinted assets |
| **Assets** | CDN configured (optional) | For global distribution |
| **Performance** | Response compression enabled | Brotli + Gzip |
| **Performance** | Output caching configured | For cacheable htmx responses |
| **Performance** | Auto-scale rules set | CPU and memory thresholds |
| **Security** | HTTPS enforced | HSTS enabled |
| **Security** | Security headers set | CSP, X-Frame-Options |
| **Security** | Rate limiting configured | Protect htmx endpoints |
| **Monitoring** | Application Insights configured | htmx telemetry middleware |
| **Monitoring** | Health checks implemented | /health endpoint |
| **Monitoring** | Alerts configured | Error rate, response time |
| **CI/CD** | Build pipeline working | GitHub Actions |
| **CI/CD** | Test pipeline working | Unit + Integration + Browser |
| **CI/CD** | Deployment pipeline working | Staging → Production |
| **CI/CD** | Rollback procedure documented | Slot swap back |

### Key Azure Services

| Service | Purpose | Recommended Tier |
|---------|---------|------------------|
| Azure App Service | Application hosting | P1v3 for production |
| Azure SQL Database | Relational data | S1 for production |
| Azure Key Vault | Secrets management | Standard |
| Azure CDN | Static asset delivery | Standard Microsoft |
| Application Insights | Monitoring | Pay-as-you-go |
| Azure Monitor | Alerts | Included |
| Azure Blob Storage | Data protection keys | Standard LRS |
| Azure Cache for Redis | Distributed caching | Basic C0 |

### CI/CD Pipeline Flow

```
┌──────────────────────────────────────────────────────────────────────────┐
│                                                                          │
│  ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌──────────┐              │
│  │  Push   │ →  │  Build  │ →  │  Test   │ →  │ Staging  │              │
│  │ to main │    │         │    │         │    │ Deploy   │              │
│  └─────────┘    └─────────┘    └─────────┘    └────┬─────┘              │
│                                                     │                    │
│                                                     ▼                    │
│                                              ┌──────────┐                │
│                                              │  Health  │                │
│                                              │  Check   │                │
│                                              └────┬─────┘                │
│                                                   │                      │
│                                                   ▼                      │
│                                              ┌──────────┐   ┌─────────┐  │
│                                              │ Approval │ → │  Swap   │  │
│                                              │ (manual) │   │  Slots  │  │
│                                              └──────────┘   └─────────┘  │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

### htmx-Specific Deployment Tips

1. **Compression**: Enable Brotli compression for htmx partial responses. Even small fragments benefit from compression.

2. **Caching**: Use output caching selectively. Cache static lookups and navigation, but never user-specific content or form submissions.

3. **Error Handling**: Return HTML partials for htmx errors using HX-Retarget. Full error pages break the htmx UX.

4. **Health Checks**: Include an htmx-compatible health endpoint that returns HTML for dashboard displays.

5. **Telemetry**: Track htmx requests separately in Application Insights. Add HX-Target and handler as custom properties.

6. **Security Headers**: Configure CSP to allow htmx's inline event handlers (`unsafe-inline` or nonces).

7. **Rate Limiting**: Apply stricter limits to search endpoints even with client-side debouncing.

### Companion Code Files

```
chap23/
├── ChinookDashboard/
│   ├── appsettings.json
│   ├── appsettings.Production.json
│   ├── Program.cs
│   ├── Middleware/
│   │   ├── HtmxTelemetryMiddleware.cs
│   │   ├── HtmxResponseCachingMiddleware.cs
│   │   ├── ProductionExceptionMiddleware.cs
│   │   └── SecurityHeadersMiddleware.cs
│   ├── Health/
│   │   ├── DatabaseHealthCheck.cs
│   │   └── HealthCheckExtensions.cs
│   └── Pages/
│       └── Shared/
│           ├── _Error404.cshtml
│           └── _Error500.cshtml
├── .github/
│   └── workflows/
│       ├── build-test.yml
│       ├── deploy.yml
│       └── azure-deploy.yml
├── infrastructure/
│   ├── azure-setup.sh
│   ├── setup-cdn.sh
│   ├── setup-domain.sh
│   └── create-service-principal.sh
├── tests/
│   └── loadtest.js
├── bundleconfig.json
├── libman.json
└── README.md
```

With these configurations in place, your htmx application is ready for production traffic on Azure. The automated pipeline ensures consistent deployments, while monitoring and alerting provide visibility into application health. As traffic grows, the scaling configurations handle increased load automatically.
