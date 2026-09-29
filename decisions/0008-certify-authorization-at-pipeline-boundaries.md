---
id: 0008-certify-authorization-at-pipeline-boundaries
title: Certify authorization at pipeline boundaries instead of policing identity changes made by trusted code
status: accepted
stage: implemented
class: contract
reversibility: costly
decided: 2026-09-29
decider: Sindre Alstad Wilting (delegated to the maintainer's AI orchestrator session)
applies-to:
  - Source/DotNET/Arc.Core/Authorization/**
  - Source/DotNET/Arc.Core/Commands/**
  - Source/DotNET/Arc.Core/Queries/**
  - Source/DotNET/Arc/Authorization/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

Decisions 0004 and 0006 treat application code in the Arc pipeline — filters, validators, hooks, policies, interceptors and emission callbacks — as trusted. Arc checks the caller's identity after policy and legacy evaluator callbacks, between individual policies (#2871), and immediately before invoking a command handler or query performer. Decision 0006 lists what those checks do not cover and says that "a complete certificate and lease model for those cases is tracked separately".

That model was planned in issue #2830 as six behavior changes: certify roles-only and plain `[Authorize]` execution (#2861), bind the resolved authentication-scheme sequence into the verdict (#2862), make Arc's and ASP.NET Core's principal a single channel (#2863), reject identity changes after the tenant and services are bound (#2864), guard deferred query execution (#2865), and check identity on every observable emission (#2866), on top of shared certificate and lease primitives (#2860).

Each gap is real in the code, but every one of them needs trusted application code to change the caller's identity after authorization. None lets a remote caller bypass authorization. Without a ruling, each future review rediscovers the same gaps and proposes the same model again.

## Decision

Arc certifies authorization at pipeline boundaries: when policies are evaluated, between policies, after application callbacks that run during evaluation, and immediately before the handler or performer is invoked. Arc does not police identity changes that trusted application code makes outside those boundaries, and it does not adopt the certificate and execution-lease model. Changes that keep behavior identical for existing applications and add no per-item cost on hot paths remain welcome. Observable subscriptions already hold a copy of the caller taken at admission rather than the request's live principal.

## Options considered

- **Certify at pipeline boundaries and document the limits (chosen).** Keeps the existing trust model, costs nothing on hot paths, breaks no application, and states the residual limits in the authorization documentation.
- **Adopt the certificate and execution-lease model (#2860–#2866).** Rejected. Five of the six behavior changes would be major releases; they would fingerprint the caller on the most common protected paths (every `[Authorize]` request and every observable emission); and #2864 would deny a pattern that works today and is covered by a specification — enriching the caller in a command scope before the verdict. The protection they buy is against mistakes in trusted code, not against attackers.
- **Adopt only the cheap parts behind an opt-in switch.** Rejected for now. An opt-in integrity mode doubles the authorization paths that must be reasoned about and tested, for a threat that the trust model already places with the application.

## Default if unanswered

The certificate and lease plan stays open under a closed parent issue, and future reviews keep proposing major releases for gaps that the trust model already accepts.

## Timeline and scope

Holds until Arc hosts code it does not trust inside the pipeline — for example plugins or tenant-supplied extensions — or until a remote caller is shown to exploit one of these gaps. Either event reopens the question. In scope: identity continuity within Arc command, query and observable-query pipelines. Out of scope: authentication itself, ASP.NET Core MVC authorization outside Arc's pipelines, and permission revocation during long-running subscriptions, which remains the job of emission guards under decision 0004.

## Verification

- **Done when:** decision 0006 points to this record instead of a separately tracked model; the authorization documentation lists the residual limits; and issues #2860, #2861, #2862, #2863, #2864, #2865 and #2866 are closed with a reference to this record.
- **Verify by:** reading `decisions/0006-opt-in-anonymous-authorization-policy-evaluation.md` and `Documentation/backend/csharp/core/authorization.md`, and checking the state of those issues on GitHub.

## Consequences

The authorization pipeline stays as simple and as fast as it is today, and no application has to change. Applications that mutate the caller after authorization — replacing `HttpContext.User` inside an operation, enriching claims after the tenant is resolved, or changing the principal an observable subscription holds — remain responsible for the result. Reopening the question means writing a new record that supersedes this one.
