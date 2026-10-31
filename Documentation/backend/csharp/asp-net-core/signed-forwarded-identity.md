# Signed forwarded identity

Signed forwarding lets a provisioned ingress attest the identity headers it sends to Arc, using HMAC-SHA256. It is opt-in, server-side only, and disabled by default on both ASP.NET Core and HttpListener.

## Configure a verifier

Enable both `Cratis:Arc:TrustForwardedIdentityHeaders` and `Cratis:Arc:ForwardedIdentity:Enabled`. Set `ForwardedIdentity:Audience` to your integration's audience and provision `ForwardedIdentity:Keys` with entries containing `KeyId` and `SecretBase64`. Each secret is exactly 32 freshly generated random bytes, standard-base64 encoded. Configure secrets through your secret provider, for example the configuration key `Cratis__Arc__ForwardedIdentity__Keys__0__SecretBase64`; never check them into configuration files. Do not reuse OIDC, cookie, or on-behalf keys.

You may configure 1–8 keys with distinct, case-sensitive IDs. Invalid enabled configuration fails forwarded authentication. With signing enabled there is no unsigned fallback: missing, malformed, expired, replayed, or incorrectly signed credentials do not establish forwarded identity, name, roles, or actor. Without signing enabled, existing forwarded-header behavior is unchanged, including the default distrust of forwarded identity headers.

**Deploy a compatible signer before enabling this mode.** Automatic AuthProxy signing is a later slice. Current unmodified AuthProxy signs neither YARP forwards nor its direct identity-details and tenant-list HTTP calls for this contract. Those workflows are incompatible with signed-only Arc targets until all relevant producers support signing.

Use TLS and restrict backend ingress. A shared-key verifier can also forge identities; provision service-specific secrets with least-privilege access. Synchronize clocks. This attests identity and bindings, not tenant existence, tenant membership, request-body integrity, or authorization.

## Authentication and request policy

A rejected forwarded credential is not an unconditional HTTP veto. Existing handler/scheme ordering and endpoint policy still apply. An earlier successful native authentication retains its own identity without forwarded augmentation. Anonymous endpoints remain accessible without promotion from invalid forwarding. A protected endpoint without successful authentication returns 401; a valid signed identity lacking a required role receives 403. Signature validity grants no additional roles.

On-behalf forwarding still requires the separate enabled RS256 assertion and actor checks. When both features are enabled, both envelopes must validate. Successful complete authentication is cached for an unchanged request only, including the attached actor. Reentry does not consume either replay token again. Mutating consumed input or relevant configuration invalidates reuse; a different request cannot reuse the success. Returned principals and actors are independent copies. Authorization is not cached.

Custom HTTP hosts must implement `ICanReadForwardedIdentityRequest`, preserving raw header cardinality and the raw origin-form request target. Flattened header dictionaries and decoded paths are insufficient. Both built-in hosts supply this capability.

## Wire contract v1

Send exactly one `x-cratis-forwarded-identity-signature` field value:

```text
v1.<kid>.<ts>.<nonce>.<mac>
```

The envelope is at most 256 ASCII bytes. `kid` matches `[A-Za-z0-9_-]{1,64}` and is case-sensitive. `ts` is unsigned invariant decimal Unix seconds with no leading zeros except zero. `nonce` is unpadded base64url of 32 cryptographically random bytes, freshly generated for every outbound attempt. `mac` is unpadded base64url of the 32-byte HMAC-SHA256 result. No algorithm negotiation, extra fields, padding, unknown versions, or repeated signature values are accepted.

### Covered headers

Include every present header whose lowercase name starts with `x-ms-client-principal` or `x-cratis-on-behalf`, plus:

- `x-cratis-token-client-id`
- `x-cratis-token-scope`
- `x-cratis-tenant-id` and `tenant-id`
- `x-cratis-microservice` and `service-id`
- The host's configured `Tenancy.HttpHeader`, if different.

Exclude the signature itself, Authorization, cookies, and unrelated headers. Names are deduplicated case-insensitively, then sorted by lowercase ordinal name. Every covered name must have exactly one raw field value. Repeated field lines are rejected; a comma in one legitimate name is not itself a duplicate. Unknown identity/on-behalf suffixes are covered too. Optional empty values are included; absent and empty differ. The principal, principal-id, and principal-name tuple must be present and nonempty.

Strip only outer HTTP OWS (SP/HTAB) from values. Preserve internal printable ASCII, commas, base64 padding, percent-escape spelling, and serialized whitespace. Reject remaining control characters and non-ASCII wire values. Unicode identities use existing base64/RFC 8187 encodings. After authenticating the bytes, the decoded subject must be nonempty and equal the RFC 8187-decoded principal-id. Invalid UTF-8, malformed principals, null claims/roles, and invalid extended IDs are rejected. The principal remains authoritative for provider claims.

### Method and path

Bind the exact uppercase HTTP method token and the final escaped path sent to the backend, including path base. Extract the path from the raw origin-form target before `?`. Require a leading `/`, valid percent triplets, and no fragment, control, or non-ASCII characters. Normalize only percent-escape hex digits to uppercase. Never URL-decode, resolve dot segments, collapse slashes, or Unicode-normalize. `/a%2fb` and `/a%2Fb` match; `/a/b` and `/a%252Fb` do not match them.

The audience comes from configuration, never Host or X-Forwarded headers. Query and body are intentionally outside this identity-attestation contract. Changing `?x=1` to `?x=2` does not change the MAC; the complete-authentication cache may conservatively invalidate on that change.

### Canonical bytes

Encode UTF-8 without BOM, using LF only. Start with literal `Cratis.AuthProxy.ForwardedIdentity.v1\n`. Append frames in this order:

```text
F(audience) F(kid) F(ts) F(nonce) F(method) F(path) F(headerCount)
F(lowercaseHeaderName) F(value) [for each covered header in ordinal order]
```

`F(s)` is invariant decimal UTF-8 byte count, `:`, the exact string, and LF. Thus `F("")` is `0:\n`, `F("a:b")` is `3:a:b\n`, and `F("é")` is `2:é\n`. There are no spaces between frames beyond their own content. Do not reserialize principal JSON. Reject more than 64 covered headers or more than 32 KiB of canonical bytes before decoding principal data.

### Freshness, replay, and rotation

Accept iff `now - 60 <= ts <= now + 30`, inclusive, in UTC seconds. No client-selected lifetime or skew exists. Reserve `(audience, kid, nonce)` only after MAC and identity bindings succeed. The process-wide replay cache holds at most 100,000 live entries and retains each through `ts + 60` inclusive; full caches fail closed without evicting live entries. Invalid MACs do not consume nonces. A failed inner assertion never publishes a reusable complete success.

Replay detection is process-local: replicas and process restarts do not share history. A future-dated accepted envelope can remain valid for up to 90 seconds after first acceptance. This is not globally single-use delivery or a transaction signature. Fleet-wide replay enforcement needs a separately specified shared store.

For rotation, provision the new key to Arc first, switch the signer's active key, then retain the old key through the final old envelope's 60-second lifetime plus 30-second clock allowance. Remove it afterward. Unknown IDs never try a fallback key. Do not log secrets, envelopes, principal data, or request-cache snapshots; rejection reasons are fixed codes.

## Interoperability vector

All material in this section is **public test-only data**, never deployable configuration. The 32-byte key is hexadecimal `000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f`; audience `arc-test`, ID `k1`, verifier time `1700000000`. Principal JSON is exactly:

```json
{"identityProvider":"aad","userId":"u","userDetails":"n","userRoles":[],"claims":[]}
```

Its standard padded-base64 representation (no padding is needed for these particular bytes) is:

```text
eyJpZGVudGl0eVByb3ZpZGVyIjoiYWFkIiwidXNlcklkIjoidSIsInVzZXJEZXRhaWxzIjoibiIsInVzZXJSb2xlcyI6W10sImNsYWltcyI6W119
```

For `POST /base/a%2fb?x=1`, the exact canonical input follows. Every displayed line, including the last, ends in LF:

```text
Cratis.AuthProxy.ForwardedIdentity.v1
8:arc-test
2:k1
10:1700000000
43:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
4:POST
11:/base/a%2Fb
1:3
21:x-ms-client-principal
112:eyJpZGVudGl0eVByb3ZpZGVyIjoiYWFkIiwidXNlcklkIjoidSIsInVzZXJEZXRhaWxzIjoibiIsInVzZXJSb2xlcyI6W10sImNsYWltcyI6W119
24:x-ms-client-principal-id
1:u
26:x-ms-client-principal-name
1:n
```

Expected MAC: `jgVyScoemstnZgj25AE70omcBbrUuzl9bIoesbV_BHo`. Complete request (HTTP line endings are CRLF; canonical line endings above remain LF):

```http
POST /base/a%2fb?x=1 HTTP/1.1
Host: backend.example
x-ms-client-principal: eyJpZGVudGl0eVByb3ZpZGVyIjoiYWFkIiwidXNlcklkIjoidSIsInVzZXJEZXRhaWxzIjoibiIsInVzZXJSb2xlcyI6W10sImNsYWltcyI6W119
x-ms-client-principal-id: u
x-ms-client-principal-name: n
x-cratis-forwarded-identity-signature: v1.k1.1700000000.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA.jgVyScoemstnZgj25AE70omcBbrUuzl9bIoesbV_BHo
Content-Length: 0

```

Use isolated replay state when comparing equivalent representations. Nonce B is 32 `01` bytes: `AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEB AQ` with the single display space removed.

| Variation | Expected forwarded authentication |
| --- | --- |
| Base vector | Verified: subject `u`, name `n`, provider `aad` |
| Header order/case changed; path escape `2f` becomes `2F` | Same canonical bytes and MAC |
| Principal, ID, name, token, tenant, service, actor, or assertion changed after signing | Rejected |
| Method, audience, key ID, or escaped path changed | Rejected |
| Query `x=1` becomes `x=2` | MAC unchanged; no query-integrity claim |
| Any covered repeated field, extra envelope component, padding, or unknown version | Rejected |
| Timestamp `1699999940` or `1700000030`, freshly signed | Accepted freshness edge |
| Timestamp `1699999939` or `1700000031`, freshly signed | Rejected freshness edge |
| Same nonce on a distinct request in this process | Rejected after one acceptance |
| Invalid MAC followed by correctly signed same nonce | Invalid attempt does not poison nonce |
| Valid outer envelope with invalid/missing inner assertion or actor | Rejected when on-behalf material is present |
| Signing disabled, signature header present | Existing unsigned trust behavior unchanged |
