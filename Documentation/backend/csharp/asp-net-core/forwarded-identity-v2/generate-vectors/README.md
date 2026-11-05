---
title: Generate and verify the v2 vectors
description: Regenerate and independently verify the public test-only ES256 forwarded identity fixtures with a standalone .NET tool.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Requires .NET 10. This is a BCL-only file-based app; it has no NuGet package dependencies. Its local `Directory.Build.props` isolates it from host repository package references. Native AOT publishing is disabled because this is a managed test tool, not a native deployment artifact; compiler and applicable SDK analyzer warnings remain errors. All keys in the adjacent JSON fixture are public **TEST-ONLY** keys, generated once and retained there. Never use them in production. The verifier imports only public PEMs. Regeneration imports the fixed private PEMs and reuses the specified request data, timestamps, and nonces. Changed canonical inputs require new randomized ECDSA signatures; cases with unchanged names, signing input and expected result retain their existing envelopes. Keys are never replaced when regenerating an existing fixture.

From the repository's `Documentation/backend/csharp/asp-net-core/forwarded-identity-v2/generate-vectors/` directory:

```bash
dotnet run generate.cs
```

With an existing fixture this verifies every expected outcome without changing it. To explicitly verify (and fail if the file is missing):

```bash
dotnet run generate.cs -- --verify ../forwarded-identity-v2-vectors.json
```

To deliberately regenerate changed inputs, preserving existing keys and unchanged cases' signatures:

```bash
dotnet run generate.cs -- --regenerate ../forwarded-identity-v2-vectors.json
```

Initial creation generates P-256 keys once. Keep the resulting fixture: deleting it before regeneration also replaces its keys. Request nonces and timestamps are deterministic test data, not a production randomness example. The regenerated fixture is immediately verified before the process returns success.

The signer and reference consumer independently implement the same coverage set, raw ASCII header-name validation, duplicate rejection, OWS trimming, ordinal sorting, request grammar, bounds, path normalization and framing. The producer uses LINQ/regexes and string frames; the consumer uses a separately declared set, character validation and streaming frames, with no shared canonicalization/selection helpers. Every case compares canonical bytes or rejection reasons between both implementations with fixed frame metadata. Accepted cases also compare their byte-exact `signingInput` against the consumer's independent construction. Rejection cases preserve the originally signed input to make mutations inspectable. Single-case replay state is isolated; named `replayGroup` cases share state in file order. The in-memory model demonstrates the contract of a shared durable store, not a production implementation or MongoDB/TLS integration test. Simulated transport and store flags are injected outcomes, not network checks. Inner on-behalf cryptography and live policy are outside this minimal verifier.

The fixture runner refuses an empty/undersized or duplicate-named case set, prints a case count and mismatch count, and returns nonzero on any mismatch. Run the checker self-test to verify it accepts three controls and detects five planted violations: malformed envelope, wrong catalog audience, an unsigned bearer swap next to a valid signed hash, refused local PAT verification and a verified-bearer account-key mismatch:

```bash
dotnet run generate.cs -- --self-test ../forwarded-identity-v2-vectors.json
```

The fixture uses `inputs.aud` for the catalog audience slug (not a resource URL), the Identity credential-kind vocabulary, explicit signed account-key issuer/subject and `idp`/integer NumericDate `auth_time`/`acr` per person credential kind, optional `amr`, current `act`, a separate grant id, nonnegative policy epoch and typed sender binding. Machine callers omit human fields. Where Authorization is forwarded, the covered `x-cratis-forwarded-token-sha256` binds its exact token value; matching envelope/bearer pairs are one credential. The pairing and tamper/missing-required cases are included. PAT namespace recognition uses the Arc PAT specification's ASCII case-insensitive `^[a-z0-9-]+_pat_` rule, before OAuth dispatch and independently of valid PAT format. A PAT-namespace bearer with a non-`pat` envelope is refused. For a bound bearer, `inputs.verifiedBearer` supplies a simulated verifier result (`verifier`, `accountKey`, `accepted`), not identity inferred from the envelope. Missing/refused verification, a wrong verifier or an exact issuer/subject mismatch is refused. Accepted bound outcomes name only `pat-verifier` or `oauth-verifier` in `expected.callerSource`; the signed person/assurance fields are attestation to compare, not caller authority. Local PAT revocation, scope-ceiling and policy-epoch enforcement are prerequisites modeled by the supplied result, not implemented token checks. Synthetic evidence is not real credential/proof data.

The fixture lives beside the generator folder so documentation and future Arc/AuthProxy specs can consume the same file without copying it into a product assembly. This is a reference contract test, not verification of a released v2 implementation.

## Identity delivery

The Identity spec layout uses one JSON object per case with `name`, `description`, `given`, `when` and `then`. From the Arc repository root, export a data-only delivery set:

```bash
dotnet run Documentation/backend/csharp/asp-net-core/forwarded-identity-v2/generate-vectors/generate.cs -- --export-identity Documentation/backend/csharp/asp-net-core/forwarded-identity-v2/forwarded-identity-v2-vectors.json .ai-work/identity-delivery/Source/Identity.Specs/Vectors/arc-forwarded-identity-v2
```

Export first verifies the Arc fixture and canonicalization parity. Each exported case contains public verification keys only and explicit initial nonce reservations, so replay cases do not depend on filename order or another case's execution. No private PEM, real credential/access token, secret or raw DPoP proof is exported. Hash-binding cases use visibly synthetic, unusable TEST-ONLY token strings or the Arc PAT proposal's public zero-payload fixture. Accepted bearer-bound cases carry the verifier's `callerSource` in `then`. The Identity layout describes data/specs, not tooling; keep this generator in Arc and reference it from the delivery spec. The export is not a TLS/MongoDB or Identity-runner integration test.

The Arc solution lists projects explicitly; this folder has no project or solution entry. Its standalone build settings do not alter product builds. The documentation toolchain scans Markdown, skips README files during site synchronization, copies JSON as an asset, and ignores C# and MSBuild props files. No generated build output belongs in this folder.
