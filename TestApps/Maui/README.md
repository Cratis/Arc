# Maui Test App

A .NET MAUI application that hosts the [ArcCore](../ArcCore) React frontend inside a native WebView. The app runs an embedded Arc HTTP backend on `localhost:5001` and navigates the WebView to that address, giving the frontend the same origin as the API.

## Purpose

This test app exists to investigate how IL trimming and AOT compilation affect a running Cratis Arc solution on Mac Catalyst and Windows. It mirrors the setup you would use in a production MAUI app that embeds an Arc backend, so it surfaces any issues with reflection-based type discovery, route registration, and observable query delivery under trimming.

## Supported Platforms

| Platform | Target Framework |
|---|---|
| macOS (Mac Catalyst) | `net10.0-maccatalyst` |
| Windows | `net10.0-windows10.0.19041.0` |

## Prerequisites

### All Platforms

| Requirement | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.100+ | Install from [dot.net/download](https://dotnet.microsoft.com/download) |
| .NET MAUI workload | 10.0.20+ | See [Install workloads](#install-workloads) below |
| Node.js | 20+ | Required to build the ArcCore React frontend |
| Yarn | 4.x | `npm install -g yarn` |

### macOS (Mac Catalyst)

| Requirement | Version | Notes |
|---|---|---|
| macOS | 15.0 (Sequoia)+ | Minimum deployment target is macOS 15 |
| Xcode | 16+ | Must be installed and plugin support initialized |

After installing Xcode, run the following once to initialize plugin support (required for the MAUI build toolchain):

```bash
xcodebuild -runFirstLaunch
```

### Windows

| Requirement | Version | Notes |
|---|---|---|
| Windows | 10 build 19041+ | Windows 10 version 2004 or later |
| Windows App SDK | — | Installed automatically by the MAUI workload |

## Install Workloads

Install the MAUI workload for your platform:

```bash
# macOS
dotnet workload install maui-maccatalyst

# Windows
dotnet workload install maui-windows
```

Verify installation:

```bash
dotnet workload list
```

## Build and Run

The build automatically installs frontend dependencies and produces a production build of the ArcCore React frontend before compiling the .NET project. No manual frontend build step is required.

```bash
# macOS
dotnet build TestApps/Maui/Maui.csproj

# Run (opens a native Mac Catalyst window)
dotnet run --project TestApps/Maui/Maui.csproj

# Windows
dotnet build TestApps/Maui/Maui.csproj
dotnet run --project TestApps/Maui/Maui.csproj
```

The embedded Arc backend starts on `http://localhost:5001`. While the app is running you can also open that URL in a regular browser to interact with the frontend directly.

## Architecture

```
Maui.app
├── ArcHostService          — Starts Arc.Core HTTP backend on localhost:5001
│   ├── Serves REST/WebSocket API at /api/...
│   └── Serves ArcCore React SPA from wwwroot/
├── MainPage (WebView)      — Navigates to http://localhost:5001
└── Shared (C# project)     — Commands, queries, and read models shared with other test apps
```

### Frontend Bundling

The ArcCore React frontend (`TestApps/ArcCore`) is built by the `BuildFrontend` MSBuild target before every `dotnet build`. The compiled `wwwroot/` output is embedded in the app and served by Arc's static file middleware.

### Mac Catalyst Notes

The Mac Catalyst build uses the Mono interpreter (`UseInterpreter=true`) to preserve reflection-based type discovery that Arc relies on for scanning assemblies and registering routes. Without this, trimming removes the metadata Arc needs.

The build also copies `Maui.deps.json` into the app bundle's `Contents/MonoBundle/` directory. `DependencyContext.Default` reads this file to discover project assemblies. Without it, no commands or queries are registered and every API request falls through to the SPA fallback.

## Troubleshooting

**All API requests return `index.html`**
The `deps.json` copy into the app bundle may have failed. Check for `[Maui] Copied deps.json into app bundle` in the build output. Rebuild with `dotnet build` to trigger the `IncludeDepsJsonInBundle` target.

**`actool exited with code 255` / "plugin failed to load"**
Xcode plugin support was not initialized. Run `xcodebuild -runFirstLaunch` and retry.

**Frontend changes not reflected after `yarn build`**
The running app serves files from the compiled app bundle, not from the source `wwwroot/` directory. Rebuild the .NET project (`dotnet build`) so the updated frontend files are copied into the bundle.
