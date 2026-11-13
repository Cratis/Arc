# OAuth/PAT caller acceptance

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

An external application using only Arc's public APIs. `Program.Build` configures one PAT installation with opt-in `AllowOAuthBearerCallers<OAuthVerifier>()`, signed v2 forwarding, independent token verification, persisted-person lookup, current tenant membership, and capability-gated MCP (`/mcp`) and REST (`/rest`). The separate `/issue` route uses the public PAT service and signed browser issuance; browser sessions cannot dispatch capability operations. OAuth grants cannot issue PATs in this composition: `/issue` returns a generic refusal without generating or persisting a PAT. MCP exposes only the read tool, not issuance.

Run the paired executable spec project from the repository root:

```sh
dotnet build Samples/OAuthPatCallerAcceptance.Specs/OAuthPatCallerAcceptance.Specs.csproj -c Release -f net11.0 -warnaserror
dotnet Samples/OAuthPatCallerAcceptance.Specs/bin/Release/net11.0/OAuthPatCallerAcceptance.Specs.dll
```

The tests supply ephemeral synthetic TLS certificates, an independent P-256 envelope signer, and an in-memory introspection authority. They call MCP tools over HTTP, not a filter in isolation. Their refusal assertions count source reads and operation dispatches on both transports. `ResolveCallerBeforeGate` deliberately caches callers before Arc's startup filter to exercise post-resolution admission. No `InternalsVisibleTo`, source linking, or internal Arc caller construction is used.

## Matrix ownership

The same scenarios run against REST and MCP. Successful calls dispatch once per transport; refusals assert zero dispatch and no credential-source fallback.

| Row | Executable coverage |
| --- | --- |
| 1 | Covered independently verified OAuth, including a pre-gate cached caller |
| 2 | OAuth refused by the PAT-only MCP profile and unmarked REST endpoint |
| 3 | Raw OAuth without signed coverage; selected verifier is not called |
| 4 | Corrupt signature, unknown key, wrong/missing hash, hash without Authorization, issuer/subject/credential disagreement |
| 5 | Replay refused independently on REST and MCP |
| 6 | Non-null and empty confirmation evidence refused |
| 7 | Signed sender binding refused for OAuth/PAT, including cached PAT |
| 8 | Introspection exception/cancellation, expired/revoked token, scope/client disagreement |
| 9 | Machine and workload signed kinds refused, including cached workload |
| 10 | On-behalf and actor contexts refused for OAuth/PAT, including cached actors |
| 11 | Browser session without bearer refused, including cached browser |
| 12 | Browser session with bearer refused |
| 13 | Publicly issued covered PAT accepted; PAT namespace mislabeled OAuth refused |
| 14 | Retired prefix in raw authorization, signed envelope, and URI refused |
| 15 | Duplicate authorization, raw bearer plus session cookie, URI credential refused |
| 16 | Missing persisted person, signed tenant outside membership, withdrawn membership, and separate unsigned tenant override refused |
| 17 | OAuth opt-in absent; PAT-only endpoint refusal |
| 18 | Existing raw PAT admission and OAuth admission preserved; malformed PAT and namespace routing assert no fallback |
| 19 | Forged forwarding context from a peer without the required certificate cannot dispatch |

There are 62 paired host scenarios with 304 facts, plus six refusal-body safety facts and five REST issuance-refusal facts: **315 facts** in total. Refusals check equivalent status/error meaning, source counts, and zero dispatch on both transports. Each transport keeps its native response shape: REST problem details and MCP errors are checked against exact allowlists that exclude token, hash, claim, and verifier details. Replay refusals receive the same body checks; transport-level TLS refusal has no HTTP body.

Optional signed client/scope metadata is tested as absent, empty, duplicate, malformed, and mismatched. General signed v2 semantics and vectors are unchanged. This is an in-process public-host acceptance harness, **not** an AuthProxy-to-Arc end-to-end interoperability run. That cross-repository check has not been performed.

## Not production adapters

The authority, PAT persistence, recovery policy, certificate generator, process-local nonce store, and request signer are synthetic test infrastructure. They are not a production identity provider, durable/recovery-fenced token store, fleet nonce store, or replacement for AuthProxy. Never log bearer tokens, verifiers, or presentation objects. Production adapters must independently enforce current token/account status, revocation, expiry, direct-person identity, audience, and absence of delegation/confirmation evidence; see the [OAuth/PAT guide](../../Documentation/backend/csharp/asp-net-core/oauth-and-pat-callers.md).
