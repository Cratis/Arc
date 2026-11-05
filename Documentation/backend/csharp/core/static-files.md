---
title: Cratis Arc.Core static file hosting
description: Serve public assets and a single-page application from the Cratis Arc.Core host, with explicit asset paths, GET fallback, and routing limits.
---

Cratis Arc.Core can serve public files and a single-page application (SPA) from the same `HttpListener` host as your commands and queries. Use `UseStaticFiles()` to serve assets and `MapFallbackToFile()` to send unmatched GET requests to the SPA's entry file, where its client-side router handles navigation.

Start with the [Core host setup](getting-started.md) and place your built frontend assets in `wwwroot` or another dedicated public directory. Arc serves those files; it does not build a frontend bundle. The examples use `using Cratis.Arc;`. Configuration fragments assume an existing builder or app; the complete SPA example includes both.

Only put public assets in a served directory. Static-file requests do not pass through command or query authorization checks.

## Basic Usage

Call `UseStaticFiles()` before starting the application to serve GET requests from the default `wwwroot` directory:

```csharp
var builder = ArcApplication.CreateBuilder(args);
builder.AddCratisArc();

var app = builder.Build();

// Enable static file serving from wwwroot
app.UseStaticFiles();

app.UseCratisArc();
await app.RunAsync();
```

Configure static files and fallback before `StartAsync()` or `RunAsync()`. `UseCratisArc()` schedules listener startup, so static-file configuration may come before or after that call. With `wwwroot/index.html` in place, a GET request to `/` serves that file. Other HTTP methods, including HEAD, are not handled by static-file serving.

## Configuration Options

Use `StaticFileOptions` to choose the directory and URL prefix independently. In this example, a request for `/static/styles.css` reads `styles.css` from `public`:

```csharp
app.UseStaticFiles(options =>
{
    // Serve files from a custom directory
    options.FileSystemPath = "public";
    
    // Add a URL prefix for static files
    options.RequestPath = "/static";
    
    // Enable/disable serving default files (index.html, etc.)
    options.ServeDefaultFiles = true;
    
    // Customize which files are considered "default" files
    options.DefaultFileNames = ["index.html", "default.html"];
    
    // Add custom MIME type mappings
    options.ContentTypeMappings["myext"] = "application/x-custom";
});
```

### StaticFileOptions Properties

| Property | Type | Default | Description |
| ---------- | ------ | --------- | ------------- |
| `FileSystemPath` | `string` | `"wwwroot"` | The directory to serve files from |
| `RequestPath` | `string` | `""` (root) | URL prefix for static file requests |
| `ServeDefaultFiles` | `bool` | `true` | Whether to look for a file from `DefaultFileNames` on directory requests |
| `DefaultFileNames` | `IList<string>` | `["index.html", "index.htm", "default.html", "default.htm"]` | Files to look for when requesting a directory |
| `EnableDirectoryBrowsing` | `bool` | `false` | Has no effect; directory listing is not implemented |
| `ContentTypeMappings` | `IDictionary<string, string>` | Empty | Custom file extension to MIME type mappings |

## File System Path Resolution

Arc.Core first checks whether the configured directory exists relative to `Directory.GetCurrentDirectory()`. If it does not, Arc resolves it relative to `AppContext.BaseDirectory`, the application binary directory. Absolute paths are used as supplied.

The working directory can differ between local development and deployment. Include the assets in your deployment and check which directory the process uses. Arc selects a directory before looking for a requested file; it does not search the application base directory for individual files missing from an existing working-directory tree.

### Example: Custom Static Directory

```csharp
// Serve from 'public' folder in your project directory
app.UseStaticFiles(options =>
{
    options.FileSystemPath = "public";
});

// Serve from an absolute path
app.UseStaticFiles(options =>
{
    options.FileSystemPath = "/var/www/static";
});
```

## SPA Fallback with MapFallbackToFile

A browser that opens a client-side route such as `/dashboard/users/123` needs the SPA's entry HTML before its router can handle the URL. `MapFallbackToFile()` serves that entry file after both registered routes and static files fail to handle a GET request:

```csharp
var app = builder.Build();

// Serve static files
app.UseStaticFiles();

// Fallback to index.html for SPA routing
app.MapFallbackToFile();  // Defaults to "index.html"

// Or specify a custom file
app.MapFallbackToFile("app.html");

app.UseCratisArc();
await app.RunAsync();
```

The fallback uses the directory from the first `UseStaticFiles()` configuration. Register static files as well as the fallback, and keep the entry file in that directory. Calling `MapFallbackToFile()` again replaces the previous fallback filename; the example above ends with `app.html` as its fallback.

An unmatched GET receives the fallback if that file exists. The fallback does not reserve an API prefix or exclude URLs with file extensions: a missing `/api/users` endpoint or `/missing.js` asset can receive the entry HTML too. Check those responses when diagnosing a client that expected JSON or JavaScript.

## Complete SPA Example

Place your SPA's built assets in `wwwroot`, including `index.html`, then use this host setup:

```csharp
using Cratis.Arc;

var builder = ArcApplication.CreateBuilder(args);
builder.AddCratisArc();

var app = builder.Build();

// Serve static files from wwwroot
app.UseStaticFiles();

// Enable SPA fallback routing
app.MapFallbackToFile();

// Configure Arc (maps API endpoints)
app.UseCratisArc();

await app.RunAsync();
```

With the corresponding files present, this configuration handles GET requests as follows:

- `/` → Serves `wwwroot/index.html`
- `/styles.css` → Serves `wwwroot/styles.css`
- `/js/app.js` → Serves `wwwroot/js/app.js`
- `/dashboard/users/123` → Serves `wwwroot/index.html` (SPA route)
- `/api/users` → Handled by a command or query only if one is mapped to that route; otherwise an unmatched GET also serves `wwwroot/index.html`

## Hosting under a path prefix

In the lightweight Arc.Core host, `app.UsePathBase("/workbench")` prefixes endpoints mapped after that call. Call it before `app.UseCratisArc()` to prefix Arc endpoints.

That setting neither prefixes static files nor limits `MapFallbackToFile()` to `/workbench`. Set `StaticFileOptions.RequestPath` explicitly for assets under that path. The fallback still handles unmatched GET requests outside the prefix, so a path base alone does not isolate the SPA. In the frontend, set `apiBasePath` for Arc requests and your router's `basename` for navigation; see [React provider configuration](../../../frontend/react/arc.md#hosting-under-a-path-prefix).

## Multiple Static File Configurations

Call `UseStaticFiles()` for each public directory you want to serve. Configurations are checked in registration order, so the first matching file wins. The fallback still uses only the first configuration's directory:

```csharp
// Serve from wwwroot at root
app.UseStaticFiles();

// Serve uploaded files from a different directory
app.UseStaticFiles(options =>
{
    options.FileSystemPath = "uploads";
    options.RequestPath = "/files";
});
```

## Middleware Order

The host checks registered routes first, static files second, and SPA fallback last. Moving these configuration calls around does not change that precedence. Unlike ASP.NET's middleware pipeline, their relative order does not let a static file override a registered route:

```csharp
var app = builder.Build();

// Configure static files (checked after registered routes)
app.UseStaticFiles();

// Configure SPA fallback (checked last)
app.MapFallbackToFile();

// Map Arc routes and schedule listener startup
app.UseCratisArc();

await app.RunAsync();
```

## MIME Types

Arc.Core sets the response's MIME type, the content type a browser uses to interpret a file, from its extension. Built-in mappings include:

| Category | Extensions |
| ---------- | ------------ |
| Text | `.txt`, `.html`, `.htm`, `.css`, `.csv`, `.xml` |
| JavaScript | `.js`, `.mjs`, `.jsx`, `.ts`, `.tsx` |
| JSON | `.json`, `.map` |
| Images | `.png`, `.jpg`, `.jpeg`, `.gif`, `.svg`, `.webp`, `.ico` |
| Fonts | `.woff`, `.woff2`, `.ttf`, `.otf`, `.eot` |
| Documents | `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx` |
| Audio | `.mp3`, `.wav`, `.ogg`, `.m4a` |
| Video | `.mp4`, `.webm`, `.avi`, `.mov` |
| Web | `.wasm`, `.webmanifest` |

Unknown extensions use `application/octet-stream`. A mapping only sets the content type: serving `.ts`, `.tsx`, or `.jsx` files does not compile them into browser-ready JavaScript.

### Adding Custom MIME Types

```csharp
app.UseStaticFiles(options =>
{
    options.ContentTypeMappings[".custom"] = "application/x-custom";
    options.ContentTypeMappings[".data"] = "application/octet-stream";
});
```

## Security

Keep secrets and private uploads outside every served directory. Static files and the SPA fallback do not inherit command or query role checks; serve private content through an authorized endpoint instead.

The implementation normalizes paths and checks whether the resolved path starts with the configured base path. That string check does not establish a filesystem or symlink security boundary. Check the deployed directory layout and permissions before exposing files.

For the host's route APIs, continue with [endpoint mapping](endpoint-mapping.md). For protected application operations, configure [authorization](authorization.md).
