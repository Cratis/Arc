# Cratis.Arc.Screenplay.Embedded

Build-time Screenplay documents and an opt-in, read-only event-model explorer for ASP.NET Core.

See [Browse your embedded event model](../../../Documentation/backend/csharp/embedded-event-model.md) for application setup, namespace conventions, authorization, and build controls.

## Build the viewer

The React application is colocated in this project. Its standalone npm lockfile keeps the board's React 19 dependencies separate from Arc's React 18 compatibility workspaces.

```bash
cd Source/DotNET/Screenplay.Embedded
npm ci --workspaces=false
npm run typecheck
npm test
npm run build
```

After building, run `node --test scripts/viewer-bundle.test.mjs` from the repository root to check every emitted JavaScript chunk against the default 500 kB budget, including the separate editor worker. Monaco dependency groups keep its core initialization intact while splitting editor contributions.

For an optional production-browser regression check, run `node scripts/viewer-browser.mjs` from the repository root with an existing Playwright installation and Chromium. If Playwright is not locally resolvable, set `PLAYWRIGHT_MODULE` to its module path; `PLAYWRIGHT_CHROMIUM_EXECUTABLE` can select an existing browser executable. The fixture serves only the built viewer on local port 19101 and checks nested hosting paths, feature selection, the real Monaco editor, and a worker response. It does not contact a running backend or install browser tooling.

On Node versions that expose experimental Web Storage, tests can run with `NODE_OPTIONS=--no-experimental-webstorage`. This disables Node's unsupported storage global, not warnings; jsdom still supplies `window.localStorage`. Do not apply this test-only setting to production.

Vite writes `wwwroot`; the .NET project embeds those files when compiled. Build the viewer before building or packing the .NET package. Consumers install the NuGet package and do not run a frontend build themselves.

From the repository root:

```bash
dotnet build Source/DotNET/Screenplay.Embedded/Screenplay.Embedded.csproj
dotnet test Source/DotNET/Screenplay.Embedded.Specs/Screenplay.Embedded.Specs.csproj
```

The MSBuild task lives in `../Tools/Screenplay.Embedded.Build` so the runtime assembly has no dependency on MSBuild. The NuGet package carries the task and its private dependencies under `tools/`, and imports its build target through `buildTransitive/`.

The backend compiles embedded `.play` text with Screenplay's visitor hooks to produce the public `@cratis/event-models` document. The browser fetches that result and mounts `EventModelBoard` read-only; it does not parse or infer Screenplay.
