---
title: Signed forwarded identity v2
description: Draft ES256 wire contract for AuthProxy identity attestation, authenticated TLS, and replay-sensitive Arc requests.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

> **Draft — contract not final until owner sign-off**

## Concept and compatibility

AuthProxy signs the identity facts it forwards; Arc verifies them with an app-specific public key. This reference specifies v2, not an already released configuration API. Signer and verifier owners must approve the contract before implementing it.

Published v1 remains unchanged: `v1.<kid>.<ts>.<nonce>.<mac>`, HMAC-SHA256, with a 32-byte nonce and process-local replay detection. Existing opt-in integrations and explicitly named local-development profiles may retain v1. The production target is v2-only. A v2-only backend rejects v1 and unsigned forwarding; it never retries another version or falls back to native authentication after a failed forwarded credential. This fail-closed request policy is intentionally stricter than v1's authentication-scheme composition.

Sign every identity-bearing producer before switching a backend: YARP requests, identity-details calls, and tenant-list calls. Strip inbound identity and signature headers at the edge and emit one complete signed set. Production startup must refuse trusted forwarded headers without verification. Only an explicitly named local-development profile may relax that configuration rule.

## Envelope grammar

Send exactly one host-observed `x-cratis-forwarded-identity-signature` value, without surrounding whitespace:

```text
v2.<kid>.<ts>.<nonce>.<sig>
```

| Component | Exact representation |
| --- | --- |
| Entire envelope | At most 256 ASCII bytes; exactly five dot-separated segments; version is lowercase `v2` |
| `kid` | Case-sensitive `[A-Za-z0-9_-]{1,64}`; selects exactly one configured P-256 public key for this audience |
| `ts` | UTC Unix seconds, unsigned invariant decimal, no leading zeros except `0`; 1–19 digits, at most `9223372036854775807` |
| `nonce` | 16 cryptographically random bytes (128 bits), canonical unpadded base64url: exactly 22 characters; fresh for every outbound attempt |
| `sig` | ES256: ECDSA P-256 with SHA-256 over the canonical bytes; 64 bytes, the 32-byte unsigned big-endian `r` followed by 32-byte unsigned big-endian `s`, canonical unpadded base64url: exactly 86 characters |

Base64url uses only `A-Z a-z 0-9 _ -`, no padding. Decode and re-encode to require canonical unused bits. Reject DER signatures: ASN.1 DER is a variable-length integer structure, not this fixed-width interoperable `r || s` format. In .NET the corresponding signature format is `DSASignatureFormat.IeeeP1363FixedFieldConcatenation`.

Accept both low-S and high-S signatures when mathematically valid (`1 <= r,s < n`, where `n` is the P-256 group order). Do not require low-S normalization. Replay identity is the signed nonce tuple, never signature bytes, so changing S cannot create another use of a reserved nonce. No algorithm field exists: version and trusted key configuration fix ES256. Never negotiate or infer an algorithm from caller data.

## Covered headers

Retain all v1 coverage: every present name starting with `x-ms-client-principal` or `x-cratis-on-behalf`, plus these exact names:

- `x-cratis-token-client-id` and `x-cratis-token-scope`
- `x-cratis-tenant-id` and `tenant-id`
- `x-cratis-microservice` and `service-id`
- The host's configured tenancy header, if different.

Unknown suffixes of either prefix are covered. The signature header is excluded even if configured as a tenancy header. Authorization, cookies, Host, and unrelated headers are not covered. Audience is a canonical frame from trusted backend configuration, not another header and never a value inferred from Host or `X-Forwarded-*`. Retain v1's audience constraints: at most 128 printable ASCII characters, not empty or all whitespace; preserve its exact case and spaces.

v2 additionally covers these exact header names. These names and value rules are draft choices requiring owner sign-off:

| Header | Meaning and representation |
| --- | --- |
| `x-cratis-credential-kind` | Required; lowercase ASCII token `[a-z][a-z0-9-]{0,63}` naming the authenticated credential class. `access-token` is a fixture example, not a closed registry. |
| `x-cratis-credential-id` | Required; opaque, nonempty credential identifier, or grant identifier for a delegated credential. This is an identifier, never the credential secret. The kind determines its interpretation. |
| `x-cratis-auth-time` | Optional `auth_time`: canonical unsigned decimal Unix seconds, in the same integer range as `ts`. Authentication time, not envelope issue time; no implicit freshness or assurance is inferred. |
| `x-cratis-assurance` | Optional nonempty opaque assurance evidence identifier; policy interprets it, rather than ordering its string value. |
| `x-cratis-actor` | Optional nonempty current actor identifier, using the existing RFC 8187 transport encoding when needed. It does not independently authorize delegation or replace on-behalf assertion validation. |
| `x-cratis-policy-epoch` | Optional canonical unsigned decimal policy epoch, in the same integer range as `ts`; policy checks its relevance and currency. |
| `x-cratis-dpop-result` | Optional `verified` or `not-applicable`. AuthProxy emits `verified` only after proof validation and binding to the credential and request. A failed required proof produces no signed request. Absence is unknown, not proof success. |

Credential kind and ID are required together. The other five headers are absent when the fact is unavailable or inapplicable; a backend policy may require them. Missing evidence never acquires a permissive default. Nonempty optional values and required values must satisfy their value grammar after OWS trimming. For opaque values needing non-ASCII characters, use the existing base64/RFC 8187 transport conventions rather than literal Unicode.

Take one immutable snapshot of host-observed names and values. Require covered names to be valid ASCII HTTP tokens. Match names case-insensitively, lowercase them invariantly, and sort by ordinal lowercase name. Every covered name and the signature must have exactly one host-observed value, including across casing variants. A comma in one value is data, not a duplicate separator. Managed HttpListener can discard earlier repeated wire values; the backend cannot recover them. Require the edge and ingress to strip repetitions rather than relying on application detection of information the host discarded.

Trim only outer HTTP OWS (SP and HTAB) on covered values. Preserve internal printable ASCII, commas, base64 padding, serialized whitespace, and percent-escape spelling in header values. Reject remaining controls, DEL, and non-ASCII values. Do not decode or reserialize values before signing. An absent header contributes no name/value pair; a present empty v1 header contributes `F(name) F("")`, so absence and empty differ. The new headers above reject empty values. The principal, principal-id, and principal-name tuple is always present and nonempty.

## Canonical signing input

Use UTF-8 without BOM and LF (`0A`) only. The first line is the literal `Cratis.AuthProxy.ForwardedIdentity.v2\n`. The v1 frame structure is unchanged; v2 uses its own domain separator, its shorter nonce, and the expanded covered set:

```text
Cratis.AuthProxy.ForwardedIdentity.v2\n
F(audience) F(kid) F(ts) F(nonce) F(method) F(path) F(headerCount)
F(lowercaseHeaderName) F(value) [for every covered name in ordinal order]
```

The spaces and layout in the frame description are explanatory, not bytes. `F(s)` is the invariant decimal UTF-8 byte length, `:`, the exact string, and LF. `F("")` is `0:\n`; `F("a:b")` is `3:a:b\n`. No extra separator or blank line exists. `headerCount` is the canonical decimal count of present covered names, excluding the signature. Credential metadata travels only in the sorted header pairs, not in extra positional frames. Reject more than 64 covered names or more than 32,768 canonical bytes before decoding principal data. The fixture's `signingInput` strings contain the actual LF characters and are byte-exact examples.

Method and path follow v1 exactly. Require a nonempty uppercase HTTP token method, without case conversion. Use the final raw origin-form target sent to the backend, including path base. Require a leading `/`; reject spaces, controls, DEL, non-ASCII, or `#` anywhere in the raw target. Take the portion before the first `?`; require every percent escape in that path to be a valid triplet and uppercase only its two hex digits. Do not URL-decode, resolve dot segments, collapse slashes, or Unicode-normalize. `/a%2fb` and `/a%2Fb` match; `/a/b` and `/a%252Fb` do not match them. Query parameters do not contribute frames. Header percent escapes are not path-normalized.

After verifying the signature, strictly decode the base64/UTF-8 principal and validate its structure, nonnull roles and claims. Its nonblank subject must equal the RFC 8187-decoded principal-id. The principal remains authoritative for provider claims. Existing on-behalf assertion and actor checks remain separate requirements whenever that material is present; an outer signature never substitutes for them.

## Keys, freshness, and rotation

Provision a distinct P-256 private key and audience for each app. AuthProxy alone holds the private key through its secret provider; backends hold public keys only. Bind each configured `kid` to ES256, P-256, and that audience. Unknown IDs fail without probing fallback keys. Keep key provisioning and emergency compromise procedures separate from this wire format.

For planned rotation, provision the new public key on every backend replica first, switch AuthProxy to the new signing key, and accept both IDs during overlap. Keep the old public key for at least 90 seconds after the last old-key outbound signing attempt (60-second age plus 30-second clock allowance). Emergency revocation may intentionally stop old-key requests before then.

Accept exactly when `now - 60 <= ts <= now + 30`, inclusive, using UTC whole seconds and overflow-safe arithmetic. Synchronize clocks; there is no client-selected lifetime. A future-dated envelope accepted at the ahead boundary can remain acceptable for 90 more seconds. `auth_time` does not replace `ts` in this check.

## Authenticated TLS on every hop

AuthProxy verifies the backend certificate's trust, validity, and expected server identity. The backend accepts identity-bearing requests only on an authenticated TLS connection from an explicitly trusted AuthProxy client certificate, validating trust, validity, and expected client identity. Plain HTTP, missing client certificates, and unrelated trusted clients do not qualify. Trust `X-Forwarded-*` only from that authenticated connection.

TLS supplies query and body integrity in transit because the envelope does not sign either. A TLS terminator becomes part of the trusted path: authenticate and protect the next hop too, preserve the final method/path and signed values, and prevent direct backend ingress. An untrusted certificate-forwarding HTTP header is not client-certificate authentication. Transport refusal may end the connection before an HTTP response exists; the uniform 401 below applies when HTTP processing is reached.

## Replay classes and the nonce store

Classify flows in trusted backend routing/policy, never in a caller-selected header. Commands, write-risk MCP tools, on-behalf session starts, and approval consumption are replay-sensitive. Before dispatch, atomically reserve `(audience, kid, nonce)` in a fleet-wide store shared by all replicas and retained across restarts. Use Arc-enforced operation idempotency keys as well: a fresh envelope or retry nonce is not a transaction or exactly-once side-effect guarantee. Approval payload binding and idempotency wire formats are separate contracts.

Other flows, such as queries, tenant lists, and identity details, accept a bounded weaker guarantee: repeats can succeed within the freshness window, but only on the authenticated hop. A local bounded cache may further restrict repeats; this contract does not promise fleet-wide single use for these flows. The reference vectors exercise the no-local-cache variant.

Use a MongoDB collection behind the proposed Arc `IForwardedIdentityNonceStore` seam (not an existing API). Create a unique compound index on `(audience, kid, nonce)` with binary, case-sensitive comparison and a TTL index on a BSON date `expiresAt` with `expireAfterSeconds: 0`. Reserve by one atomic insert, not read-then-insert. Duplicate key means replay. Require majority acknowledgment and durable writes so primary failover does not lose acknowledged reservations; an uncertain write result is a failure, not permission to dispatch.

Retain each reservation through `ts + 60` inclusive. Set `expiresAt` to at least `ts + 61` seconds UTC; allow an additional margin for store/verifier clock differences. This can require 91 whole-second buckets from the earliest accepted instant, so a fixed 60-second TTL from insertion is insufficient. TTL cleanup is asynchronous: a lingering record can conservatively reject a repeat and must not be deleted early by application code. Enforce bounded capacity operationally and fail closed rather than evict live entries. All replicas use the same store and indexes; fail closed if required indexes, capacity, or the store are unavailable for replay-sensitive flows. Reserve after cryptographic and identity checks and before dispatch. Do not undo reservations when authorization, dispatch, or a response later fails.

MongoDB provides an atomic single-document insert and native TTL cleanup, and reuses deployments that already use it with Arc and Chronicle; Arc itself does not require Chronicle or MongoDB. Redis was rejected to avoid requiring another deployed service. An append-only event log is unsuitable for short-lived nonce records. An EF/SQL implementation can follow for EF-only deployments, preserving the same atomicity, durability, expiry, and outage semantics. An in-memory implementation is allowed only in explicitly named local development, never as an automatic production fallback.

## Verification order and failures

At a v2-only backend, apply these checks before endpoint execution:

1. Admit only the authenticated AuthProxy TLS hop; validate local v2 key, audience, and replay-policy configuration.
2. Capture immutable host-observed input. Require exactly one signature; bound and parse the envelope, version, encodings, and raw signature size. Reject v1 here.
3. Check freshness, then select the exact configured key. Build bounded canonical bytes from the local audience and the observed covered headers, method, and path.
4. Verify ES256. Then validate the decoded principal/subject binding, new metadata grammar and applicable evidence policy, and any separate on-behalf assertion and actor bindings.
5. For replay-sensitive flows, perform the durable atomic nonce reservation. Only a confirmed new reservation permits dispatch. Reuse a complete authentication result only within the same unchanged request, not across new requests.
6. Run live membership and authorization checks. Authentication does not grant tenant access, roles, delegation authority, or approval consumption.

Every envelope/admission failure that reaches HTTP processing returns the same 401 status with the same generic response, no distinguishing body, challenge detail, header, or fallback path. Internal fixed reason codes may distinguish format, version, freshness, key, signature, identity, metadata, replay, store, or hop failures in protected logs only. Do not log envelopes, signatures, principal payloads, credential IDs, keys, or snapshots. No timing-equality guarantee is implied. A subsequent authorization denial for a valid authenticated caller is separate from envelope rejection.

## What the envelope does not attest

The envelope attests AuthProxy's identity facts and their binding to the method, escaped path, and configured audience. It does not attest tenant existence, live tenant membership, authorization, current policy projection state, or request-body/query content. Assurance and policy epoch are evidence to evaluate, not decisions. Payload-bound approvals require their own operation hash and policy checks. A valid signature does not prove application intent or eliminate the need to validate inputs.

## Pitfalls

- Sign the post-routing backend path, not the browser-visible path. A proxy rewrite after signing invalidates the binding.
- Preserve the snapshot through principal/actor construction; a second mutable header read defeats consistent interpretation.
- Preserve header whitespace and encodings except the specified outer OWS trim. Do not JSON-reserialize the principal or split a comma-valued header.
- Keep rotation overlap and nonce retention long enough for future-dated accepted requests. A signature cache keyed by signature bytes is not a nonce store.
- Review every route's replay class, including MCP tools, before rollout. Store outages must not switch replay-sensitive requests to an in-memory fallback.
- Keep all identity-bearing direct calls on the same signing and authenticated-TLS path as proxied requests.

## Testing with the shared vectors

The repository fixture `Documentation/backend/csharp/asp-net-core/forwarded-identity-v2/forwarded-identity-v2-vectors.json` contains public **TEST-ONLY** private/public P-256 key pairs. Never deploy them. Cases use the earlier fixture's name/value header array and PEM key representation, not its superseded compact JWS format. Each case supplies `inputs` (method, raw target as `path`, headers, audience, `now`, configured tenancy header, replay class, and simulated transport/store availability), `envelope`, and one `expected` result plus internal reason. `signingInput` records the bytes originally signed, so it intentionally differs from the recomputed input in tamper cases. The envelope is supplied separately rather than duplicated in the header array.

Use fresh replay state per case except when `replayGroup` is set: those cases run in file order against one shared store, across the simulated replica/restart labels. The reference store is an in-process model of the fleet-wide seam, not a MongoDB integration test. Transport flags model a completed hop admission check; they do not test certificates. The minimal verifier checks envelope binding, principal structure, and the metadata rules above; separate on-behalf cryptography, live authorization, DPoP proof validation, and production durability require their own integration tests. On-behalf actor/assertion tamper cases test byte coverage and reject before inner validation.

With .NET 10, run from the repository's `Documentation/backend/csharp/asp-net-core/forwarded-identity-v2/generate-vectors/` folder:

```bash
dotnet run generate.cs -- --verify ../forwarded-identity-v2-vectors.json
```

Success prints the case count and `0 mismatches` and exits zero. The signer and minimal reference verifier build canonical input independently. Consumers should additionally compare their canonical bytes with the valid cases' `signingInput`, and run every expected outcome. Positive normalization cases isolate replay state so they do not accidentally become replay tests.

Running `dotnet run generate.cs` verifies the existing fixture without rewriting randomized signatures. Use `dotnet run generate.cs -- --regenerate ../forwarded-identity-v2-vectors.json` deliberately to replace signatures while retaining the fixture's fixed keys. Initial creation generates those test keys once; retain the fixture as the key source for later regeneration.
