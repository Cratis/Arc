---
id: 0018-verified-caller-is-the-authorization-principal
title: Use the verified caller as the authorization principal on capability ports
status: accepted
stage: implemented
decided: 2026-11-06
decider: woksin (via L0)
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Arc.Core/Identity/**
  - Source/DotNET/Arc/Capabilities/**
  - Source/DotNET/Arc.Core/Authorization/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

A request admitted on a capability port carries a verified caller, but Arc authorization, the current principal accessor and Chronicle causation read the request's own authenticated user. The two could describe different subjects, and a delegated credential's scope ceiling was not applied to the operations it reached.

## Decision

- The proof credential is the v2 browser envelope, verified with a signer that lives in the repository for tests.
- On a capability-port request the verified caller becomes the principal through an internal Arc authentication scheme. Its claims are a fixed set derived from the caller's fields only.
- Roles and tenant membership stay owned by the product and apply to the verified caller's subject.
- The scope ceiling is enforced once, in Arc's authorization evaluator. An operation declares the scopes it needs with `[RequiresScope("…")]`, and every declared scope must be held by the caller. The ceiling reads the verified caller, never claims, so a claims transformation cannot widen it. A principal without a verified caller is not narrowed.
- A request already authenticated as a different or unidentifiable subject is refused with 403 rather than merged.
- Chronicle causation flows through `ICurrentPrincipalAccessor`. A verified caller is recorded as its own identity: the verified subject, marked as a verified caller by name and by a user name carrying the credential kind. It is never mapped to a user identity of the request. A delegated caller is recorded as a chain, the actor first and the verified subject as the identity it acts on behalf of.
- The scope ceiling also applies to MVC controller actions, through an authorization filter.
- When a verified caller is present, a principal that also carries another authenticated identity with a missing or different subject is refused.
- A minimal public installation API ships only together with an acceptance sample that uses the public API alone.

## Options considered

- **Per-operation `[RequiresScope]` attribute (chosen).** Joins the existing attribute-based declaration path and expresses mixed scopes on one surface.
- **Group-level scope.** Too coarse for surfaces that mix read and write scopes.
- **A new capability descriptor type.** A second declaration path and a new concept.
- **Closing the identity's claim mutators.** Fragile and no gain, because authority never derives from claims.

## Default if unanswered

The request's own user keeps driving authorization on capability ports, and delegated scope ceilings are not enforced.

## Timeline and scope

Apply on the verified-caller branch. Scope-ceiling enforcement and the principal integration are internal; the attribute and `AuthorizationDeclaration.RequiredScopes` are the only public additions in this step.

## Verification

**Done when:** a verified caller is the principal Arc and Chronicle see, an operation requiring a scope the caller lacks is refused, and a different-subject request is refused.

**Verify by:** the `for_AuthorizationEvaluator`, `for_ScopeCeiling`, `for_VerifiedCallerIdentity`, `for_CapabilityGateMiddleware` and `for_CapabilityHosting` specs.

## Consequences

- Browser callers have an empty ceiling unless the credential carries a token scope, so any `[RequiresScope]` operation refuses plain browser sessions. This fails closed.
- When the verified-caller scheme is the only registered scheme, ASP.NET makes it the default; off the capability port it yields no result, which is harmless, and a challenge there answers with a plain 401.
- The public installation API is a minor-version addition.
