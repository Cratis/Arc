# Static Files

Arc.Core provides built-in support for serving static files, similar to the `UseStaticFiles()` middleware in ASP.NET Core. This is essential for hosting Single Page Applications (SPAs), serving assets like CSS, JavaScript, images, and other static content.

Except for the complete SPA example, code blocks below are configuration fragments for the [Core host](getting-started.md), with `using Cratis.Arc;` in scope. Supply the static asset files yourself; Arc does not build a frontend bundle.

## Basic Usage

To serve static files from the default `wwwroot` directory:

```csharp
var builder = ArcApplication.CreateBuilder(args);
builder.AddCratisArc();

var app = builder.Build();

// Enable static file serving from wwwroot
app.UseStaticFiles();

app.UseCratisArc();
await app.RunAsync();
```

> **Important**: Configure static files and fallback before `StartAsync()` or `RunAsync()`. `UseCratisArc()` schedules listener startup; it does not start the listener immediately. Static-file configuration may precede or follow that call.

## Configuration Options

You can customize static file serving using `StaticFileOptions`:

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
| `ServeDefaultFiles` | `bool` | `true` | Whether to serve index.html for directory requests |
| `DefaultFileNames` | `IList<string>` | `["index.html", "index.htm", "default.html", "default.htm"]` | Files to look for when requesting a directory |
| `EnableDirectoryBrowsing` | `bool` | `false` | Whether to allow directory listing (not yet implemented) |
| `ContentTypeMappings` | `IDictionary<string, string>` | Empty | Custom file extension to MIME type mappings |

## File System Path Resolution

Arc.Core resolves relative paths in the following order:

1. **Current Working Directory** - First checks if the path exists relative to `Directory.GetCurrentDirectory()`. This is typically your project directory when running with `dotnet run`.

2. **Application Base Directory** - Falls back to `AppContext.BaseDirectory` (the `bin/Debug/net10.0/` output folder) if not found in the current directory.

This behavior matches how ASP.NET Core resolves content root paths during development and ensures your `wwwroot` folder is found whether you're running from the project directory or from the compiled output.

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

For Single Page Applications that use client-side routing, you need to serve your `index.html` for any route that doesn't match a static file or API endpoint. Use `MapFallbackToFile()`:

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

The fallback file is served when:

1. No registered API route matches the request
2. No static file matches the request path
3. The request method is GET
4. The fallback file exists in the static files directory

## Complete SPA Example

Here's a complete example for hosting a React, Angular, or Vue SPA:

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

With this configuration:

- `/` → Serves `wwwroot/index.html`
- `/styles.css` → Serves `wwwroot/styles.css`
- `/js/app.js` → Serves `wwwroot/js/app.js`
- `/dashboard/users/123` → Serves `wwwroot/index.html` (SPA route)
- `/api/users` → Handled by your query endpoints

## Multiple Static File Configurations

You can call `UseStaticFiles()` multiple times to serve from different directories:

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

Request precedence is **registered routes → static files → SPA fallback**, regardless of the relative order of these configuration calls. This is not ASP.NET's ordered middleware pipeline:

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

Arc.Core includes built-in MIME type mappings for common file extensions:

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

For unknown extensions, `application/octet-stream` is returned.

### Adding Custom MIME Types

```csharp
app.UseStaticFiles(options =>
{
    options.ContentTypeMappings[".custom"] = "application/x-custom";
    options.ContentTypeMappings[".data"] = "application/octet-stream";
});
```

## Security

Serve only public assets from dedicated directories; do not place secrets or private uploads in a served tree. Static files are not command/query pipeline operations and do not inherit their role checks. The implementation normalizes paths and checks containment, but do not treat this as a security certification for every filesystem or symlink arrangement. Test your deployment and enforce private-file access through an authorized endpoint instead.
