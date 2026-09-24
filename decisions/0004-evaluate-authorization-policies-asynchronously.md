---
id: 0004-evaluate-authorization-policies-asynchronously
title: Evaluate complete authorization requirements asynchronously without bypassing legacy denials
status: accepted
stage: verified
class: contract
reversibility: costly
decided: 2026-09-24
decider: Sindre Alstad Wilting
supersedes: 0001-enforce-aspnet-core-authorization-attributes
applies-to:
  - Source/DotNET/Arc.Core/Authorization/**
  - Source/DotNET/Arc.Core/Commands/**
  - Source/DotNET/Arc.Core/Queries/**
  - Source/DotNET/Arc/Authorization/**
  - Source/DotNET/Arc/Queries/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

Arc accepts policy and authentication-scheme settings on authorization attributes, but previously enforced only authentication and roles. ARC0021 made the missing behavior visible without enforcing it (#2736). Decision 0001 restored ASP.NET Core attribute support and explicitly reserved policy support for this follow-up. Completing it must not bypass custom authorization evaluators or execute work under a different principal from the one that was authorized.

## Decision

Arc evaluates named policies asynchronously inside command and query pipelines, including observable-query admission. Core provides named, scoped policies; the ASP.NET Core host also supports its registered policies and actually authenticates requested schemes. Unsupported schemes, unknown policies, ambiguous policy registrations, and malformed requirements fail closed. Startup validation completes before listeners start, with defensive runtime checks retained. Existing public synchronous evaluator and performer signatures remain available; they cannot silently ignore asynchronous requirements. Custom legacy denials remain authoritative. Both attribute families remain supported on the ASP.NET Core host, all stacked requirements apply, model-bound method declarations replace type declarations, and contradictory anonymous/restricted declarations remain rejected in either evaluator order. Authorized execution and streaming identity snapshots use the selected principal without retaining disposed request services.

## Options considered

- **Portable asynchronous policies plus an ASP.NET Core policy/scheme adapter (chosen).** Keeps Core usable without ASP.NET Core while honoring the host's real authentication machinery. Requires explicit scope, cancellation, and principal-lifetime handling.
- **ASP.NET Core-only policies.** Simpler integration, but Core would continue accepting a policy attribute it cannot enforce. Rejected because authorization belongs to the pipeline, not only an HTTP middleware boundary.
- **Block synchronous evaluator calls on asynchronous work.** Rejected: it blocks execution threads, obscures cancellation and lifetime, and cannot safely preserve asynchronous host behavior.
- **Keep warnings and require application filters.** Rejected: an accepted authorization attribute would still promise a restriction it does not enforce.

## Default if unanswered

Policies and schemes remain unenforced, with ARC0021 warning about the gap. Applications can mistake authentication alone for their declared policy. Existing synchronous contracts do not justify that silent partial check.

## Timeline and scope

This contract applies to the policy-support release and subsequent extensions. Preserve source and binary signatures where an additive path is sufficient. Explicitly unsupported configurations are rejected rather than assigned permissive defaults. Ordinary C# calls outside Arc pipelines and ASP.NET Core middleware's own MVC composition remain outside this decision. Observable policies gate subscription admission; ongoing revocation belongs to emission guards. Existing command context initialization and execution-scope ordering are not silently rearranged; pre-authorization hooks must not treat their initial principal as the final authorized identity.

## Verification

- **Done when:** native and ASP.NET Core policies, explicit and policy-contributed schemes, custom legacy evaluators, and real streaming subscriptions enforce the same selected identity at their respective pipeline boundaries; invalid configuration prevents listener startup; cancellation never allows deferred work to execute.
- **Verify by:** affected Core, ASP.NET Core, and analyzer specifications in Debug and Release; real-host command, query, SSE, and WebSocket policy scenarios; regressions for custom-constructor denials, nested principal overrides, concurrent scoped policies, noncooperative cancellation, and startup ordering. Existing both-attribute-family contradiction specifications remain green. Required CI must pass before release.

## Consequences

Applications using formerly ignored policy or scheme settings now receive the restrictions they declared, or fail startup with an actionable configuration error. Core policy implementations resolve per executing scope. Scheme-restricted direct calls require a live ASP.NET Core HTTP context. Callers that need policy evaluation use the asynchronous pipeline instead of synchronous evaluator calls. Library APIs stay compatible where possible, without preserving the unsafe behavior of silently ignoring requirements.
