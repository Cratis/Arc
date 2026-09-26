---
id: 0006-opt-in-anonymous-authorization-policy-evaluation
title: Opt named policies into evaluation for unauthenticated callers
status: accepted
stage: verified
class: contract
reversibility: costly
decided: 2026-09-26
decider: Sindre Alstad Wilting
amends: 0004-evaluate-authorization-policies-asynchronously
applies-to:
  - Source/DotNET/Arc.Core/Authorization/**
  - Source/DotNET/Arc.Core/Commands/**
  - Source/DotNET/Arc.Core/Queries/**
  - Source/DotNET/Arc/Authorization/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

Decision 0004 made an authenticated principal mandatory before evaluating every named policy. A policy that intentionally admits guests could not express that behavior on a model-bound command or query. An attribute-level opt-in would work only for Arc's attribute, not Microsoft's supported `[Authorize]` attribute.

## Decision

Policy registration can explicitly opt into evaluation for unauthenticated callers. Arc policies use `AddArcAuthorizationPolicy<T>(name, evaluatesAnonymous: true)`; ASP.NET Core-registered policies require a separate explicit `AddArcAnonymousAspNetAuthorizationPolicy(name)` opt-in and must not contain an authenticated-user requirement. Both authorization attribute families receive identical behavior. An absent or unauthenticated caller is presented to an opted-in policy as an empty unauthenticated `ClaimsPrincipal`; `AuthorizationPolicyContext.Principal` remains non-nullable. Every requirement in a declaration must be an opted-in policy with no role or scheme requirements before anonymous evaluation is possible. Requested schemes continue to require a successfully authenticated scheme principal. All other declarations still require authentication. Legacy custom evaluator denials remain authoritative.

## Options considered

- **Opt in at policy registration (chosen).** The policy owner declares whether guest evaluation is safe, regardless of which attribute family names it.
- **Add `RequireAuthenticated = false` to Arc's attribute.** Rejected because Microsoft attributes cannot carry the setting and two equivalent declarations would behave differently.
- **Automatically admit guests for ASP.NET Core policies without `RequireAuthenticatedUser`.** Rejected because an existing policy could unexpectedly begin running for unauthenticated callers.

## Default if unanswered

Every policy continues to require an authenticated caller, including policies whose own logic would accept a guest.

## Timeline and scope

This amends the authentication-required-for-policies consequence of decision 0004 only for explicitly opted-in, policy-only declarations. The rest of decision 0004, including asynchronous evaluation and scheme selection, remains in force. Commands, queries, and observable-query admission use the same rule; MVC authorization outside the Arc pipeline is unchanged.

## Verification

- **Done when:** a guest can run a command or query allowed by an opted-in policy, a rejecting policy still denies the guest, and defaults, roles, and schemes still reject unauthenticated callers in either attribute family.
- **Verify by:** affected Core and ASP.NET Core authorization specifications, Debug and Release builds, and documentation verification.

## Consequences

Policy owners may safely express public-or-member permissions without `[AllowAnonymous]` bypassing checks. Opting in does not grant access by itself: every policy still decides the verdict. Existing policy registrations and authentication requirements retain their previous behavior.
