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

## Testing with NativeAOT and Full Trimming

The project has `<IsAotCompatible>true</IsAotCompatible>` set globally, which enables AOT and trim analyzers during every `dotnet build`. Analyzer warnings (`IL2026`, `IL3050`) are currently suppressed in `<NoWarn>` — remove those entries from the project file when you want to see every incompatibility Arc and its dependencies have with trimming.

`dotnet build` always uses Mono for fast iteration. Only `dotnet publish` produces a NativeAOT binary with full trimming applied. You must target a single architecture — publishing without `-r` produces a universal binary (x64 + arm64) that fails because the `deps.json` differs between the two slices:

```bash
# macOS — publish with NativeAOT + full trimming (Apple Silicon)
dotnet publish TestApps/Maui/Maui.csproj -f net10.0-maccatalyst -r maccatalyst-arm64

# macOS — publish with NativeAOT + full trimming (Intel)
dotnet publish TestApps/Maui/Maui.csproj -f net10.0-maccatalyst -r maccatalyst-x64
```

After publishing, run the `.app` bundle from the `bin/Release/net10.0-maccatalyst/<rid>/publish/` directory and open `http://localhost:5001/.cratis/queries` to observe which Arc features survive full trimming and NativeAOT compilation.

### Known incompatibility: `ProjectReferencedAssemblies` requires `DependencyContext`

Under NativeAOT, `/.cratis/queries` returns `[]` and every API request falls through to the SPA fallback, because no commands or queries are ever registered.

**Root cause:** `Cratis.Fundamentals.Types.ProjectReferencedAssemblies.Initialize()` calls `DependencyContext.Load(Assembly.GetEntryAssembly())` to enumerate project assemblies. Under NativeAOT the entire app is compiled into a single native binary — there is no `deps.json` file in the bundle, so `DependencyContext.Load` returns `null`. The whole discovery block is skipped, `_assemblies` stays empty, and every `IInstancesOf<T>` / `FindMultiple<T>` call returns nothing.

**Where to fix:** `Cratis.Fundamentals` → `ProjectReferencedAssemblies.Initialize()`. When `DependencyContext.Load` returns `null`, fall back to `AppDomain.CurrentDomain.GetAssemblies()` filtered to non-system assemblies and collect their `DefinedTypes`. This makes type discovery work in both Mono and NativeAOT contexts.

## Architecture

```text
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

`dotnet build` uses the Mono runtime for fast iteration. `dotnet publish` produces a NativeAOT binary where the trimmer removes all statically unreachable code and the AOT compiler compiles everything ahead-of-time to native arm64 or x64.

**Mono builds (`dotnet build`):** The `IncludeDepsJsonInBundle` MSBuild target copies `Maui.deps.json` into the app bundle's `Contents/MonoBundle/` directory. `DependencyContext.Default` reads this file to discover project assemblies — without it Arc finds no commands or queries and every API request falls through to the SPA fallback.

**NativeAOT builds (`dotnet publish`):** There is no separate `Maui.deps.json` in the bundle; all assemblies are compiled into a single native binary. Arc's reflection-based type scanning (`IImplementationsOf<T>`, `DependencyContext`) does not work in this mode without additional `[DynamicDependency]` annotations or trimmer root descriptors — surfacing exactly the incompatibilities this test app exists to investigate.

## Troubleshooting

**All API requests return `index.html`**
The `deps.json` copy into the app bundle may have failed. Check for `[Maui] Copied deps.json into app bundle` in the build output. Rebuild with `dotnet build` to trigger the `IncludeDepsJsonInBundle` target.

**`actool exited with code 255` / "plugin failed to load"**
Xcode plugin support was not initialized. Run `xcodebuild -runFirstLaunch` and retry.

**Frontend changes not reflected after `yarn build`**
The running app serves files from the compiled app bundle, not from the source `wwwroot/` directory. Rebuild the .NET project (`dotnet build`) so the updated frontend files are copied into the bundle.
