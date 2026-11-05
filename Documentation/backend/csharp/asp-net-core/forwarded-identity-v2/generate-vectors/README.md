---
title: Generate and verify the v2 vectors
description: Regenerate and independently verify the public test-only ES256 forwarded identity fixtures with a standalone .NET tool.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Requires .NET 10. This is a BCL-only file-based app; it has no NuGet package dependencies. Its local `Directory.Build.props` isolates it from host repository package references. Native AOT publishing is disabled because this is a managed test tool, not a native deployment artifact; compiler and applicable SDK analyzer warnings remain errors. All keys in the adjacent JSON fixture are public **TEST-ONLY** keys, generated once and retained there. Never use them in production. The verifier imports only public PEMs. Regeneration imports the fixed private PEMs and reuses the same request data, timestamps, and nonces. ECDSA signatures are randomized and change on deliberate regeneration.

From the repository's `Documentation/backend/csharp/asp-net-core/forwarded-identity-v2/generate-vectors/` directory:

```bash
dotnet run generate.cs
```

With an existing fixture this verifies every expected outcome without changing it. To explicitly verify (and fail if the file is missing):

```bash
dotnet run generate.cs -- --verify ../forwarded-identity-v2-vectors.json
```

To deliberately regenerate the fixture, preserving existing keys:

```bash
dotnet run generate.cs -- --regenerate ../forwarded-identity-v2-vectors.json
```

Initial creation generates P-256 keys once. Keep the resulting fixture: deleting it before regeneration also replaces its keys. Request nonces and timestamps are deterministic test data, not a production randomness example. The regenerated fixture is immediately verified before the process returns success.

The signer and reference consumer implement header selection, path normalization, and framing separately. Accepted cases also compare their byte-exact `signingInput` against the consumer's independent construction. Rejection cases preserve the originally signed input to make mutations inspectable. Single-case replay state is isolated; named `replayGroup` cases share state in file order. The in-memory model demonstrates the contract of a shared durable store, not a production implementation or MongoDB/TLS integration test. Simulated transport and store flags are injected outcomes, not network checks. Inner on-behalf cryptography and live policy are outside this minimal verifier.

The fixture runner refuses an empty/undersized or duplicate-named case set, prints a case count and mismatch count, and returns nonzero on any mismatch. Run the checker self-test to verify it accepts a control and detects deliberately planted format and binding violations:

```bash
dotnet run generate.cs -- --self-test ../forwarded-identity-v2-vectors.json
```

The fixture lives beside the generator folder so documentation and future Arc/AuthProxy specs can consume the same file without copying it into a product assembly. This is a reference contract test, not verification of a released v2 implementation.

The Arc solution lists projects explicitly; this folder has no project or solution entry. Its standalone build settings do not alter product builds. The documentation toolchain scans Markdown, skips README files during site synchronization, copies JSON as an asset, and ignores C# and MSBuild props files. No generated build output belongs in this folder.
