---
applyTo: "Source/DotNET/Arc.Core.Generators/**,Source/DotNET/Shared/**,Source/DotNET/Screenplay/Screenplay.csproj"
paths:
  - "Source/DotNET/Arc.Core.Generators/**"
  - "Source/DotNET/Shared/**"
  - "Source/DotNET/Screenplay/Screenplay.csproj"
---

## Generator diagnostics live in the shared descriptor class

Declare every `DiagnosticDescriptor` that `Arc.Core.Generators` reports in `Source/DotNET/Shared/ArcGeneratorDiagnostics.cs`, and list it in `Source/DotNET/Arc.Core.Generators/AnalyzerReleases.Unshipped.md`.

- `Screenplay.csproj` compiles the shared files and links the generator's `AnalyzerReleases.*.md` for release tracking. A rule listed there but declared only inside the generator fails the Screenplay build with RS2002.
- A separate shared file holding only the new descriptor fails with MA0182 (unused type), because Screenplay does not use it. `ArcGeneratorDiagnostics` is already used through `ModelBoundQueryMethodSymbols`, so new descriptors belong in it.
- Use the next free `ARC####` identifier and add its page under `Documentation/backend/csharp/code-analysis/`.
