# Verified caller acceptance sample

This sample uses only public Arc APIs to receive signed v2 **browser-session** callers on a capability listener. It has no PAT, OAuth-grant, service-account, on-behalf, Chronicle, or database setup.

`Program.Build` installs one exact resource/audience/environment binding, public P-256 verification material, a persisted-person directory, and explicit process-local replay protection. The product owns editor roles (`ProductRoles`) and live tenant membership (`ProductMembership`); a signed tenant header alone grants no access. Role enrichment preserves the verified subject on every authenticated identity it adds.

| Listener | Operation | Requirements |
| --- | --- | --- |
| Capabilities, port 8081 | `POST /articles/publish` | Authenticated editor, `content:write`, membership in `tenant-a` |
| Capabilities, port 8081 | `GET /articles` | Authenticated person, `read`, membership in `tenant-a` |
| Default, port 8080 | Capability routes | Not exposed |

The command returns the subject from `ICurrentPrincipalAccessor`, the identity source Arc also uses for causation. `InvocationCounts` makes dispatch observable to the specs without adding an unprotected diagnostic endpoint.

## Run the proof

From the repository root:

```sh
dotnet test Samples/VerifiedCallerAcceptance.Specs/VerifiedCallerAcceptance.Specs.csproj -c Release
```

The specs start one host, send real signed HTTP requests, and cover successful command/query dispatch, missing product roles, missing scopes, same-subject scope narrowing, different-subject refusal, missing people, replay, unknown keys, audience mismatch, unsigned forwarding, tenant non-membership, and listener isolation. They are included in CI shard `specs-3`.

To run the host separately, supply a **public-only** P-256 PEM file:

```sh
dotnet run --project Samples/VerifiedCallerAcceptance -- path/to/public-key.pem
```

The default key identifier is `sample`. A trusted proxy must sign requests for `example-api`, with the custom `x-example-tenant` header covered by the signature. The backend never needs the proxy's private signing key.

## Deliberate sample boundaries

- The directory contains two synthetic persisted accounts; it does not provision people from headers.
- `ProductHeader` is a deliberately insecure, **sample-only** stand-in for another authentication scheme. It proves that a same-subject product principal cannot widen the signed credential ceiling and a different subject is refused. Never authenticate production users from `X-Product-User`.
- Process-local nonces are suitable only for this single-process proof. A fleet must install a product-owned `IForwardedIdentityNonceStore` with atomic, shared reservations and replay retention.
- The specs keep a sample-local subset of the existing public, synthetic v2 fixture: only its first test key and `valid-v2` case. Test signing material stays in the specs' output; it is not referenced by the host project and must never be used outside tests.
