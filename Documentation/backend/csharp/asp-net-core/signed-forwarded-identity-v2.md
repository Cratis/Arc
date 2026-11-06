---
title: Signed forwarded identity v2
description: ES256 wire contract for AuthProxy identity attestation, authenticated TLS, and replay-sensitive Arc requests.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

> **Status: Locked v1.** This wire contract is final; any change requires a new contract version.

## Concept and compatibility

AuthProxy signs the identity facts it forwards; Arc verifies them with an app-specific public key. This reference specifies the v2 wire contract, not an already released configuration API.

Published v1 remains unchanged: `v1.<kid>.<ts>.<nonce>.<mac>`, HMAC-SHA256, with a 32-byte nonce and process-local replay detection. Existing opt-in integrations and explicitly named local-development profiles may retain v1. The production target is v2-only. A v2-only backend rejects v1 and unsigned forwarding; it never retries another version or falls back to native authentication after a failed forwarded credential. This fail-closed request policy is intentionally stricter than v1's authentication-scheme composition.

Sign every identity-bearing producer before switching a backend: YARP requests, identity-details calls, and tenant-list calls. On every inbound request, the edge MUST remove every covered header (including all names under the two covered prefixes, every v2 context header, and the configured tenancy header) and every occurrence of `x-cratis-forwarded-identity-signature`, case-insensitively. It MUST then set the covered headers and envelope only from its own verified authentication, credential, catalog, routing and policy state. It MUST NOT preserve, merge, endorse or re-sign a caller's supplied values. Any downstream Authorization bearer must also come from that same verified credential/session state, not a copied caller header. If verification fails, it emits no signed downstream request. Production startup must refuse trusted forwarded headers without verification. Only an explicitly named local-development profile may relax that configuration rule.

## Install browser-session verification

For the supported public browser-session installer, receiving resource binding, persisted-person directory, and explicit nonce-store selection, see [Install verified callers](verified-callers.md). The acceptance sample in the repository's `Samples/VerifiedCallerAcceptance` folder exercises that flow over real HTTP without internal APIs. This does not install PAT, OAuth-grant, service-account, bearer-verifier, or on-behalf adapters; product authorization and live tenant membership still apply.

## Normative vocabulary

This contract depends on Identity's `identity-contract` version 1, Terms and §§2, 4–6. The relevant definitions below are reused verbatim.

| Term | Definition |
| --- | --- |
| Person | The person-facing identity within a realm, identified by its exact realm-issued `(issuer, subject)` pair; not a claim of deduplication across realms. |
| Account | Identity's realm-local record for a person, with an immutable subject, lifecycle state, linked upstream keys and protected profile. |
| Realm | An issuer trust boundary with its own accounts and signing keys, independent of process placement. |
| Issuer | The trusted authority issuing an identity or token; the realm issuer scopes a person subject, while an upstream issuer scopes a linked key. |
| Subject | The opaque, pseudonymous, immutable, never-reassigned account id issued within a realm; never email, login or upstream id. |
| Resource | A registered protected API whose catalog entry binds a resource indicator, audience and scope prefix; detailed in Arc scope spec. |
| Resource indicator | The absolute URL identifying the intended protected resource in an OAuth request, not a permission or an issuer. |
| Audience | The resource identifier a token verifier MUST match to its own catalog entry; this profile permits one audience per access token. |
| Tenant | A product-owned workspace with product-owned live membership and roles; not an organisation or storage namespace. |
| Organisation | Access's commercial and administrative unit for cross-product policy and confirmed tenant bindings, not workspace membership. |
| Environment | A registered deployment context constraining a credential and tenant binding; never inferred from a caller-supplied label; detailed in Arc verified-caller spec. |
| Actor | The current agent or supporter acting for the person, without replacing the person's subject; detailed in Arc delegation spec. |
| Credential kind | The verified credential classification determining validation and caller construction, not a role; detailed in Arc verified-caller spec. |
| Assurance (`acr`/`amr`) | Connection-mapped authentication strength (`acr`) and verified methods (`amr`), never authority inferred from untrusted upstream labels; detailed in Arc verified-caller spec. |
| Policy epoch | A monotonically increasing organisation-policy revision forcing stale credentials to be re-evaluated against current policy; detailed in Arc verified-caller spec. |

The account key for a person is the exact realm-issued `(issuer, subject)` pair, not an upstream subject or a subject considered across all realms. An audience (`aud`) is a catalog slug such as `direct-api`, not the RFC 8707 resource indicator URL. A trusted catalog maps exactly one registered resource indicator to exactly one audience and scope prefix. Resolve this mapping and the environment/installation binding from trusted state before signing or verification; do not infer them from Host, `X-Forwarded-*` or caller labels.

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

Unknown suffixes of either prefix are covered, and the edge strips them all before constructing its own verified set. The signature header is excluded from canonical header pairs even if configured as a tenancy header, but is always stripped from inbound requests and freshly generated by the edge. Authorization is bound only through the covered token hash described below, not as a canonical header pair. Cookies, Host and unrelated headers are not covered; they cannot supply trusted context fields.

`aud` is a canonical frame from the backend's trusted resource catalog configuration, not a header. In v2 it is exactly one 1–64-character ASCII slug matching `^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$`. Compare it exactly without normalization. This narrows v2's audience value grammar; it does not alter v1's existing audience grammar. The synthetic vectors bind `https://api.example.test` to `example-api` in their trusted catalog. The resource indicator URL is never substituted into the `aud` frame.

v2 additionally covers these exact header names. These names and value rules are part of the contract:

| Header | Meaning and representation |
| --- | --- |
| `x-cratis-credential-kind` | Required. Exact wire values for the Identity credential kinds: `browser-session`, `oauth-grant`, `pat`, `on-behalf-session`, `machine-principal`, `workload`. An access token is an OAuth credential, not an additional credential kind or a role. |
| `x-cratis-credential-id` | Required nonempty, non-secret identifier of the verified credential record. It is not the secret and is not overloaded with a grant id; a token `jti` alone is not a grant id. |
| `x-cratis-issuer` | Required for a person, absent for a service caller. Exact `accountKey.issuer`: allowlisted realm issuer HTTPS URL, following Identity §2.2; scopes the person's subject. Never infer it from legacy provider/display fields. |
| `x-cratis-subject` | Required for a person, absent for a service caller. Exact lowercase UUID-v4 `accountKey.subject`, following Identity §2.1; must equal the legacy tuple's decoded subject, without normalization. |
| `x-cratis-idp` | Required `assurance.idp` for a person. Exact upstream issuer/authority from the trusted connection or verified credential-creation evidence, not a provider display name or the realm issuer by assumption. |
| `x-cratis-auth-time` | Required `assurance.auth_time` for a person. Actual verified person-authentication time, preserved through refresh/exchange, encoded as canonical integer NumericDate (Unix seconds, signed 64-bit decimal, no `+`, leading zeros or `-0`). No fractional value. Not `ts`, token `iat`, refresh time or current time. |
| `x-cratis-acr` | Required `assurance.acr` for a person. Connection-mapped `acr` string; unknown or unmapped evidence MUST be `basic`, never elevated from an untrusted label. Vocabulary beyond `basic` requires review. |
| `x-cratis-amr` | Optional `assurance.amr`: standard-base64-encoded UTF-8 JSON array of verified, connection-mapped `amr` strings. Omit when unknown; an empty array conveys no method evidence. |
| `x-cratis-act` | Required when acting for another. Standard-base64-encoded UTF-8 JSON object with current actor's registered `iss` and `sub`, and optional nested `act` for earlier actors (maximum eight nested prior actors in this wire profile). Top-level person subject stays unchanged; prior actors are informational. |
| `x-cratis-grant-id` | Required with `act`; otherwise optional when a bounded per-work grant applies. Nonempty, non-secret reference to the authoritative bounded grant/support record, distinct from the credential id and token/proof `jti`. Grant lineage and revocation stay server-side. |
| `x-cratis-policy-epoch` | Required when organisation policy applies; otherwise absent. Canonical nonnegative decimal integer in the `ts` integer range: a monotonically increasing organisation-policy revision. Require a policy projection at or after it; stale credentials need re-evaluation. |
| `x-cratis-sender-binding` | Required. Exact `none` for an unbound credential, or standard-base64-encoded UTF-8 JSON object containing only verified DPoP `jkt` (canonical base64url 32-byte thumbprint) and accepted proof `jti` (nonempty string). The edge verifies key, target, method, time, token hash and replay before emitting it. A bound credential never falls back to `none`; failed required proof emits no signed request. No raw proof is forwarded. |
| `x-cratis-forwarded-token-sha256` | Present exactly when the edge forwards an Authorization bearer. Canonical unpadded base64url of the 32-byte SHA-256 digest of the exact forwarded token bytes; covered by the envelope. It is not an OAuth token claim or an independent credential. |

Requiredness is evaluated against the verified credential kind and authoritative policy, never caller-supplied applicability flags. Missing assurance is not elevated evidence. The old assurance, actor and DPoP-result header spellings are not aliases for these fields. All present metadata values are nonempty after OWS trimming. Base64 JSON is transport encoding only: sign its exact bytes without parsing and reserialization, then validate its structure after signature verification. Opaque non-ASCII data uses these encodings, not literal Unicode.

The legacy principal tuple remains covered transport, not the Identity access-token claim schema. For a person, its decoded `userId`/principal-id must be the exact lowercase UUID-v4 subject from Identity §2.1, bound to `x-cratis-issuer`; never lowercase, trim or repair it. Legacy roles/profile fields do not establish live membership or add claims to an OAuth access token. Machine principals and workloads produce service callers with no person/account key; their legacy transport id is not converted into a person UUID. A separate verified per-work grant is required for any person's authority in workload operations. The existing tenant headers carry the product tenant target, not organisation, environment or storage namespace; `tid` remains the Identity access-token tenant claim, not a new alias. Existing token-scope data is a catalog-valid, space-delimited ceiling, not roles or membership. A public OAuth client id remains declared, not authenticated merely because it is forwarded.

Take one immutable snapshot of host-observed names and values. Require covered names to be valid ASCII HTTP tokens. Match names case-insensitively, lowercase them invariantly, and sort by ordinal lowercase name. Every covered name and the signature must have exactly one host-observed value, including across casing variants. A comma in one value is data, not a duplicate separator. Managed HttpListener can discard earlier repeated wire values; the backend cannot recover them. Require the edge and ingress to strip repetitions rather than relying on application detection of information the host discarded.

Trim only outer HTTP OWS (SP and HTAB) on covered values. Preserve internal printable ASCII, commas, base64 padding, serialized whitespace, and percent-escape spelling in header values. Reject remaining controls, DEL, and non-ASCII values. Do not decode or reserialize values before signing. An absent header contributes no name/value pair; a present empty v1 header contributes `F(name) F("")`, so absence and empty differ. The new headers above reject empty values. The principal, principal-id, and principal-name tuple is always present and nonempty.

## Verified caller construction and requiredness

These headers map to the exact logical field names in the Arc verified-caller v1 contract; they do not define new OAuth claims or rename its record. Recognized envelope wire version `v2` selects a reviewed local verification profile; the caller record's `contractVersion` is `"1"`, not `"2"`. Unknown profiles are refused. `credentialKind` and non-secret `credentialId` come from the verified credential record. The product resolves `person` from the confirmed `accountKey` mapping; it never manufactures a product person from a display name. A service caller instead uses an allowlisted `servicePrincipal`, not a fabricated person or Actor.

| Verified credential kind | Signed person and assurance fields | Other required context |
| --- | --- | --- |
| Browser session or on-behalf session (`browser-session`, `on-behalf-session`) | `accountKey.issuer`/`subject`, `assurance.idp`, integer NumericDate `assurance.auth_time`, and `assurance.acr` are all required from the verified session's preserved authentication evidence. `amr` is optional. | `credentialId` and `senderBinding` are required. An on-behalf session also requires current `act` and the checked `grantId`. |
| OAuth bearer/grant (`oauth-grant`) | The same account key and assurance fields are required from validated token evidence. With a bound bearer, signed person/assurance fields are attestation to compare, not authority: only the OAuth verifier builds the caller, and the signed account key MUST equal the verified bearer's. Fields MUST NOT be invented from token issue time or current time. | `credentialId` and `senderBinding` are required. Delegated use additionally needs current `act` and the authoritative `grantId`. |
| PAT (`pat`) | The same account key and assurance fields are required from the protected PAT/credential-creation record. Signed person/assurance fields are attestation to compare, not authority; they MUST NOT replace local PAT verification or be invented from PAT labels or current time. With a bound PAT, only the local PAT verifier builds the caller, and the signed account key MUST equal the verified PAT's. | `credentialId` and `senderBinding` are required. Local verification MUST enforce revocation, the scope ceiling and applicable policy epoch. Delegated use additionally needs current `act` and the authoritative `grantId`. |
| Machine principal or workload (`machine-principal`, `workload`) | Account key, all human assurance fields and person-representing `act` MUST be absent, not null/empty placeholders. The legacy tuple id denotes only the verified service identity. | `credentialId` and `senderBinding` are required; explicit service permissions, target tenant and any per-work grant come from trusted mappings. No person promotion. |

There is no silent missing-value fallback for a person's `idp` or `auth_time`. Explicitly map unknown authentication strength to `acr=basic`, never elevated evidence. Absent optional fields contribute no canonical frames; present empty values are invalid. Omit unknown `amr` or encode a verified empty array; neither creates a method. The covered `senderBinding` value `none` is explicit absence of binding, never permission to ignore `cnf.jkt` or a required proof. Registered resource/environment, live tenant membership, organisation binding, current actor, grant bounds, policy epoch and service permissions still require their authoritative checks.

## Forwarded bearer binding and the credential gate

Cookie-session routes may continue forwarding the session's access token in `Authorization: Bearer <token>` because some backends call downstream APIs on the user's behalf. Dropping the token would break that capability. Instead, the edge selects the access token from the same verified session/credential and signs `x-cratis-forwarded-token-sha256`. It must not bind an unrelated caller-supplied token.

Compute SHA-256 over the token value after the scheme and its single ASCII SP, excluding the `Bearer` scheme and that SP. Token bytes must satisfy RFC 6750's bearer-token syntax; preserve their exact case and padding, with no decoding, reserialization or whitespace normalization. The scheme is case-insensitive; the token is not. Encode the digest as canonical unpadded base64url (43 characters). `Authorization` is not itself a canonical header pair; the signed hash is its binding.

After envelope verification and before caller construction or nonce reservation, the verifier MUST require exactly one host-observed Authorization value if that header is present, require the Bearer scheme, recompute the digest from the observed token and compare it to the signed hash. A missing hash with Authorization, a mismatch, a hash without a bearer, malformed hash, other scheme or repeated Authorization MUST reject with the same generic 401; never select another verifier. If no bearer is forwarded, both Authorization and the hash header are absent.

Only a matching bearer and verified envelope count as **one credential** for the verified-caller credential gate. This is not permission to accept two unrelated credentials: other simultaneous credentials and credentials in a URI query remain forbidden. There MUST be exactly one caller, built only by that bearer's own verifier (the local PAT verifier or the OAuth verifier). The envelope binds the bearer to the hop; it never substitutes for bearer verification. Its signed `accountKey.issuer` and `accountKey.subject` MUST exactly equal the verified bearer's account key; any mismatch or failed bearer verification MUST refuse the request with the same generic 401, without constructing a caller or trying another verifier.

Use the Arc PAT specification's **PAT namespace**: any credential value beginning with `<key>_pat_`, matched ASCII case-insensitively by `^[a-z0-9-]+_pat_`. Namespace recognition precedes OAuth verification and does not establish valid PAT format. A verified envelope covering a PAT-namespace bearer MUST be refused unless its credential kind is `pat`; there is no OAuth fallback. A **person envelope** means an envelope whose credential kind is not `pat`; AuthProxy MUST NOT attach one to a forwarded PAT. Even a `pat` envelope does not assert a verified PAT caller: local PAT verification, including revocation, the scope ceiling and applicable policy epoch, remains mandatory. Signed person/assurance fields in a `pat` envelope, and in a bearer-bound `oauth-grant` envelope, are attestation to compare against verified credential evidence, not authority to construct or elevate a caller.

The hash neither grants permission nor replaces DPoP/credential validation; bound tokens still require verified sender binding and cannot fall back to plain bearer authentication. Include the exact Authorization snapshot in any request-local authentication-cache comparison; changing it invalidates reuse even if canonical headers are unchanged. Never log the token or digest.

## Canonical signing input

Use UTF-8 without BOM and LF (`0A`) only. The first line is the literal `Cratis.AuthProxy.ForwardedIdentity.v2\n`. The v1 frame structure is unchanged; v2 uses its own domain separator, its shorter nonce, and the expanded covered set:

```text
Cratis.AuthProxy.ForwardedIdentity.v2\n
F(aud) F(kid) F(ts) F(nonce) F(method) F(path) F(headerCount)
F(lowercaseHeaderName) F(value) [for every covered name in ordinal order]
```

The spaces and layout in the frame description are explanatory, not bytes. `F(s)` is the invariant decimal UTF-8 byte length, `:`, the exact string, and LF. `F("")` is `0:\n`; `F("a:b")` is `3:a:b\n`. No extra separator or blank line exists. `headerCount` is the canonical decimal count of present covered names, excluding the signature. Credential metadata travels only in the sorted header pairs, not in extra positional frames. Reject more than 64 covered names or more than 32,768 canonical bytes before decoding principal data. The fixture's `signingInput` strings contain the actual LF characters and are byte-exact examples.

Method and path follow v1 exactly. Require a nonempty uppercase HTTP token method, without case conversion. Use the final raw origin-form target sent to the backend, including path base. Require a leading `/`; reject spaces, controls, DEL, non-ASCII, or `#` anywhere in the raw target. Take the portion before the first `?`; require every percent escape in that path to be a valid triplet and uppercase only its two hex digits. Do not URL-decode, resolve dot segments, collapse slashes, or Unicode-normalize. `/a%2fb` and `/a%2Fb` match; `/a/b` and `/a%252Fb` do not match them. Query parameters do not contribute frames. Header percent escapes are not path-normalized.

After verifying the signature, strictly decode the base64/UTF-8 principal and validate its structure, nonnull roles and claims. Its subject must exactly equal the RFC 8187-decoded principal-id. Apply the person UUID/realm rules above, or construct a service caller without inventing a person. Do not interpret legacy provider claims as an allowlist for downstream OAuth token claims. Existing on-behalf assertion checks and current-actor/grant bindings remain separate requirements whenever that material is present; an outer signature never substitutes for them.

## Keys, freshness, and rotation

Provision a distinct P-256 private key for each app and bind its verifier to the app's registered catalog audience (`aud`). AuthProxy alone holds the private key through its secret provider; backends hold public keys only. Bind each configured `kid` to ES256, P-256, and that audience. Unknown IDs fail without probing fallback keys. Keep key provisioning and emergency compromise procedures separate from this wire format.

For planned rotation, provision the new public key on every backend replica first, switch AuthProxy to the new signing key, and accept both IDs during overlap. Keep the old public key for at least 90 seconds after the last old-key outbound signing attempt (60-second age plus 30-second clock allowance). Emergency revocation may intentionally stop old-key requests before then.

Accept exactly when `now - 60 <= ts <= now + 30`, inclusive, using UTC whole seconds and overflow-safe arithmetic. Synchronize clocks; there is no client-selected lifetime. A future-dated envelope accepted at the ahead boundary can remain acceptable for 90 more seconds. `auth_time` does not replace `ts` in this check.

## Authenticated TLS on every hop

AuthProxy verifies the backend certificate's trust, validity, and expected server identity. The backend accepts identity-bearing requests only on an authenticated TLS connection from an explicitly trusted AuthProxy client certificate, validating trust, validity, and expected client identity. Plain HTTP, missing client certificates, and unrelated trusted clients do not qualify. Trust `X-Forwarded-*` only from that authenticated connection.

TLS supplies query and body integrity in transit because the envelope does not sign either. A TLS terminator becomes part of the trusted path: authenticate and protect the next hop too, preserve the final method/path and signed values, and prevent direct backend ingress. An untrusted certificate-forwarding HTTP header is not client-certificate authentication. Transport refusal may end the connection before an HTTP response exists; the uniform 401 below applies when HTTP processing is reached.

## Replay classes and the nonce store

Classify flows in trusted backend routing/policy, never in a caller-selected header. Commands, write-risk MCP tools, on-behalf session starts, and approval consumption are replay-sensitive. Before dispatch, atomically reserve `(aud, kid, nonce)` in a fleet-wide store shared by all replicas and retained across restarts. Use Arc-enforced operation idempotency keys as well: a fresh envelope or retry nonce is not a transaction or exactly-once side-effect guarantee. Approval payload binding and idempotency wire formats are separate contracts.

Other flows, such as queries, tenant lists, and identity details, accept a bounded weaker guarantee: repeats can succeed within the freshness window, but only on the authenticated hop. A local bounded cache may further restrict repeats; this contract does not promise fleet-wide single use for these flows. The reference vectors exercise the no-local-cache variant.

Use a MongoDB collection behind the proposed Arc `IForwardedIdentityNonceStore` seam (not an existing API). Create a unique compound index on `(aud, kid, nonce)` with binary, case-sensitive comparison and a TTL index on a BSON date `expiresAt` with `expireAfterSeconds: 0`. Reserve by one atomic insert, not read-then-insert. Duplicate key means replay. Require majority acknowledgment and durable writes so primary failover does not lose acknowledged reservations; an uncertain write result is a failure, not permission to dispatch.

Retain each reservation through `ts + 60` inclusive. Set `expiresAt` to at least `ts + 61` seconds UTC; allow an additional margin for store/verifier clock differences. This can require 91 whole-second buckets from the earliest accepted instant, so a fixed 60-second TTL from insertion is insufficient. TTL cleanup is asynchronous: a lingering record can conservatively reject a repeat and must not be deleted early by application code. Enforce bounded capacity operationally and fail closed rather than evict live entries. All replicas use the same store and indexes; fail closed if required indexes, capacity, or the store are unavailable for replay-sensitive flows. Reserve after cryptographic and identity checks and before dispatch. Do not undo reservations when authorization, dispatch, or a response later fails.

MongoDB provides an atomic single-document insert and native TTL cleanup, and reuses deployments that already use it with Arc and Chronicle; Arc itself does not require Chronicle or MongoDB. Redis was rejected to avoid requiring another deployed service. An append-only event log is unsuitable for short-lived nonce records. An EF/SQL implementation can follow for EF-only deployments, preserving the same atomicity, durability, expiry, and outage semantics. An in-memory implementation is allowed only in explicitly named local development, never as an automatic production fallback.

## Verification order and failures

At a v2-only backend, apply these checks before endpoint execution:

1. Admit only the authenticated AuthProxy TLS hop; validate local v2 key, exact catalog `aud`, trusted realm issuer and replay-policy configuration.
2. Capture immutable host-observed input. Require exactly one signature; bound and parse the envelope, version, encodings, and raw signature size. Reject v1 here.
3. Check freshness, then select the exact configured key. Build bounded canonical bytes from the local catalog `aud` and the observed covered headers, method, and path.
4. Verify ES256. Validate the explicit account key and legacy tuple binding, credential-kind-specific metadata and assurance, forwarded bearer/hash pairing and credential gate, and any separate on-behalf assertion, current-actor and grant bindings. Only then construct a caller.
5. For replay-sensitive flows, perform the durable atomic nonce reservation. Only a confirmed new reservation permits dispatch. Reuse a complete authentication result only within the same unchanged request, not across new requests.
6. Run live membership and authorization checks. Authentication does not grant tenant access, roles, delegation authority, or approval consumption.

Every envelope/admission failure that reaches HTTP processing returns the same 401 status with the same generic response, no distinguishing body, challenge detail, header, or fallback path. Internal fixed reason codes may distinguish format, version, freshness, key, signature, identity, metadata, credential binding, replay, store, or hop failures in protected logs only. Do not log envelopes, signatures, Authorization values, token digests, principal payloads, credential IDs, keys or snapshots. No timing-equality guarantee is implied. A subsequent authorization denial for a valid authenticated caller is separate from envelope rejection.

## What the envelope does not attest

The envelope attests AuthProxy's identity facts and their binding to the method, escaped path, and configured audience. It does not attest tenant existence, live tenant membership, authorization, current policy projection state, or request-body/query content. Connection-mapped `acr`/`amr`, sender binding, current `act` and policy epoch are evidence to evaluate, not authorization decisions. Payload-bound approvals require their own operation hash and policy checks. A valid signature does not prove application intent or eliminate the need to validate inputs.

## Pitfalls

- Sign the post-routing backend path, not the browser-visible path. A proxy rewrite after signing invalidates the binding.
- Preserve the snapshot through principal/actor construction; a second mutable header read defeats consistent interpretation.
- Preserve header whitespace and encodings except the specified outer OWS trim. Do not JSON-reserialize the principal or split a comma-valued header.
- Keep rotation overlap and nonce retention long enough for future-dated accepted requests. A signature cache keyed by signature bytes is not a nonce store.
- Review every route's replay class, including MCP tools, before rollout. Store outages must not switch replay-sensitive requests to an in-memory fallback.
- Keep all identity-bearing direct calls on the same signing and authenticated-TLS path as proxied requests.

## Testing with the shared vectors

The repository fixture `Documentation/backend/csharp/asp-net-core/forwarded-identity-v2/forwarded-identity-v2-vectors.json` contains public **TEST-ONLY** private/public P-256 key pairs. Never deploy them. Cases use the earlier fixture's name/value header array and PEM key representation, not its superseded compact JWS format. Each case supplies `inputs` (method, raw target as `path`, headers including any synthetic TEST-ONLY bearer, `aud`, `now`, configured tenancy header, trusted realm issuer, replay class, and simulated transport/store availability), `envelope`, and one `expected` result plus internal reason. `signingInput` records the bytes originally signed, so it intentionally differs from the recomputed input in tamper cases. The envelope is supplied separately rather than duplicated in the header array.

Use fresh replay state per case except when `replayGroup` is set: those cases run in file order against one shared store, across the simulated replica/restart labels. The reference store is an in-process model of the fleet-wide seam, not a MongoDB integration test. Transport flags model a completed hop admission check; they do not test certificates. The minimal verifier checks envelope binding, explicit account-key/legacy subject equality, trusted realm issuer equality, credential-kind-specific required assurance, integer NumericDate grammar, service-caller distinction, metadata structure, forwarded bearer/hash pairing, PAT-namespace/kind dispatch and account-key equality against a supplied verified-bearer result. Accepted bearer-bound outcomes name `pat-verifier` or `oauth-verifier` as the caller source. Supplied bearer results simulate completed local verification, not token validation by this tool. Actual credential, upstream connection/`idp`, current-actor registration, grant, catalog/environment, assurance mapping and policy-projection validation remain authoritative production checks; separate on-behalf cryptography, live authorization, DPoP proof validation and production durability require integration tests. On-behalf actor/assertion tamper cases test byte coverage and reject before inner validation.

With .NET 10, run from the repository's `Documentation/backend/csharp/asp-net-core/forwarded-identity-v2/generate-vectors/` folder:

```bash
dotnet run generate.cs -- --verify ../forwarded-identity-v2-vectors.json
```

Success prints the case count and `0 mismatches` and exits zero. The signer uses its own header selection, raw-name validation, OWS trimming, sorting, request validation, bounds and string framing; the consumer independently implements the same rules with its own coverage set and streaming frames. Neither calls the other's canonicalization or shares its header-selection helpers. Every case compares their canonical bytes or rejection reason using identical frame metadata, including invalid-name, duplicate, control, size and path cases. Accepted cases additionally compare consumer bytes to the actual `signingInput`. Consumers must run all expected outcomes; positive normalization cases isolate replay state so they do not accidentally become replay tests.

Running `dotnet run generate.cs` verifies the existing fixture without rewriting randomized signatures. Use `dotnet run generate.cs -- --regenerate ../forwarded-identity-v2-vectors.json` deliberately to regenerate changed canonical inputs while retaining the fixture's fixed keys and preserving unchanged cases' signatures. Initial creation generates those test keys once; retain the fixture as the key source for later regeneration.
