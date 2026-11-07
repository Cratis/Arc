# Personal access token audit with Chronicle

`Cratis.Arc.PersonalAccessTokens.Chronicle` adds audit events and an owner-listing
projection to the resource-local personal access token module. It is optional;
the core module does not depend on Chronicle.

Chronicle is **audit-only, not credential or revocation authority**. Neither its
event log nor its projections may be used to accept a token, establish current
authorization, or acknowledge a revocation. Verification must read the
resource-local authority and current policy and recovery epoch, failing closed
when those are unavailable. A delayed audit event does not delay revocation.

The internal `IPatAuthority` and `IPatRecoveryEpoch` adapters must provide
linearizable reads ordered against revocation and recovery-epoch advancement.
A request after an acknowledged revocation or epoch advancement must not accept
stale active state. Bypassing a positive cache alone is not sufficient: adapters
that cannot guarantee this ordering must fail closed.

## Credential boundaries

`IPersonalAccessTokens.Revoke` accepts only PAT credentials owned by the current
verified person and tenant, and requires a caller authenticated with a PAT.
Other caller credential kinds and unknown credential identifiers are refused.

The retired ASCII format markers `dpat` and `drun` are always refused when
followed by an underscore, case-insensitively. This includes the public shapes
`dpat_<32 lowercase hex>_<64 lowercase hex>` and `drun_<opaque>`, even though
neither is in Arc's `<segment>_pat_` namespace. The refusal applies before
verification or fallback to Authorization values under any scheme, credential
query values, and the custom credential envelope header inspected by Arc.
A name such as `dpatient` does not match this boundary.

These defaults cannot be disabled. Hosts can add retired prefixes, including
the trailing underscore, through `PatOptions.ReservedLegacyPrefixes`.
Direct's existing `dpat` tokens are not accepted by the Arc module; they must be
migrated separately.

## Audit content

`PatIssued` and `PatRevoked` record the credential identifier, resource,
environment, tenant, person, creation-time scope ceiling, expiry, upstream
creation-time assurance, restore epoch, and the authority's operation timestamp.
`VerifierId` is a domain-separated SHA-256 hash of the verifier, not the full
verifier. It cannot be used for credential lookup or authentication.

The audit snapshot excludes the token, secret, full verifier, upstream account
key, HTTP headers, and request bodies. Do not add those values to events,
projection fields, tags, error messages, or logs. Scope and assurance fields
record issuance context; they are not grants of current authorization.

## Owner listings

`PatAuditProjection` folds these facts into `PatAuditEntry`, with issuance and
revocation timestamps. Include the event and projection assembly in the
application's Chronicle artifact discovery, using the application's normal
Chronicle read-model storage configuration.

A listing must filter by the verified owner's resource, environment, tenant,
and person. Apply application authorization before returning entries. The
projection is eventually consistent: an absent revocation timestamp means only
that no revocation fact has been projected yet, not that the token is active.
Replay rebuilds audit state only; it must never recreate or reactivate credentials.

## Internal outbox delivery

The core currently supplies no durable authority adapter or outbox implementation.
Installing either package alone does not enable issuance or start a background
delivery worker. The internal `IPatAuditOutbox` port is a delivery seam, not a
public authoritative-lookup API.

An audit-enabled authority adapter must capture an immutable, allowlisted
snapshot in the same local transaction as issuance or revocation. Use stable,
globally unique intent identifiers and preserve commit order for a credential.
Repeated owner revocation must not enqueue another intent. Never reconstruct a
pending snapshot from mutable credential state when delivering it. The metadata
factory requires a committed restore epoch and copies scope and assurance
collections before hashing the verifier.

`PatAuditOutboxDrainer` delivers one bounded batch with Chronicle's durable
idempotent append API. It acknowledges an intent only after a successful append.
A lost acknowledgement can be retried by another drainer with the same intent
identifier without appending a duplicate fact. Unsupported idempotent appends or
an append failure leave delivery pending; exceptions propagate to the worker for
retry. There is no fallback to a non-idempotent append or to Chronicle-based
verification. The authority's successful local revocation commit remains the
revocation fence regardless of audit delivery availability.
