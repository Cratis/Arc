---
id: 0001-enforce-aspnet-core-authorization-attributes
title: Enforce ASP.NET Core's authorization attributes on model-bound artifacts, alongside Arc's own
status: accepted
stage: implemented
decided: 2026-09-23
decider: Sindre Alstad Wilting
class: product
reversibility: costly
applies-to:
  - Source/DotNET/Arc.Core/Authorization/**
  - Source/DotNET/Arc/Authorization/**
  - Source/DotNET/Arc.Core.CodeAnalysis/AuthorizationAttributeAnalyzer.cs
---

## Context

Before v18.2.0, Arc authorized model-bound commands and read models from `Microsoft.AspNetCore.Authorization.AuthorizeAttribute`; Arc had no attribute of its own, and its `[Roles]` derived from Microsoft's. When Arc Core was separated from ASP.NET Core it gained `Cratis.Arc.Authorization`'s own `[Authorize]`, `[AllowAnonymous]` and `[Roles]`, and `AspNetAnonymousEvaluator` / `AspNetAuthorizationAttributeEvaluator` were added to keep honouring Microsoft's. They never did: inside `Cratis.Arc.Authorization` the unqualified names resolved to Arc's same-named attributes. From v18.2.0 a model-bound artifact protected only with Microsoft's `[Authorize]` was open to every caller (#2719).

## Decision

When Arc is hosted on ASP.NET Core through `Cratis.Arc`, its pipeline enforces Microsoft's `[Authorize]` and `[AllowAnonymous]` on model-bound commands and read models exactly as it enforces its own: authentication, and any roles named. Arc's own attributes remain the canonical, host-neutral choice. Every authorization attribute on a declaration applies, from either family, and a declaration that is both anonymous and restricted is rejected as ambiguous regardless of evaluator order. Analyzers report what is still not enforced: a Microsoft attribute in a project without `Cratis.Arc` (ARC0020), and a `Policy` or `AuthenticationSchemes` (ARC0021).

## Options considered

- **Enforce both families (chosen).** Restores pre-v18.2.0 behaviour, finishes what the Arc Core split set out to do, and matches what an ASP.NET Core developer will type. Cost: two attribute families to keep consistent.
- **Support only Arc's attributes; delete the ASP.NET Core evaluators and reject Microsoft's with an analyzer error.** One family, one meaning. Rejected: it leaves every application that upgraded across v18.2.0 with Microsoft attributes still unprotected until it notices, it turns a regression fix into a migration, and the evaluators exist precisely because the split intended to keep the Microsoft attributes working.
- **Keep ignoring them and only warn.** Rejected: fails open, which is the least safe outcome for an authorization attribute.

## Default if unanswered

The ASP.NET Core attributes stay ignored and warned about (ARC0020 as released in v22.20.0). Every model-bound artifact relying on them stays open.

## Timeline and scope

Holds until Arc Core defines its own policy model (#2736), which may revisit how the two families map onto it. In scope: model-bound commands and read models, over every route into the pipeline. Out of scope: MVC controllers, where ASP.NET Core MVC enforces its own attributes; named policies and authentication schemes (#2736).

## Verification

- **Done when:** a model-bound command marked only with Microsoft's `[Authorize]` rejects a caller with no identity through the real host, and stacked or contradictory attributes behave the same in either evaluator order.
- **Verify by:** `ProxyGenerator.Specs` `with_aspnet_authorize_and_no_user`, and `Arc.Specs` `for_AuthorizationEvaluator`, which runs every case with the evaluators in both orders.

## Consequences

Applications on v18.2.0–v22.20.0 that relied on Microsoft's `[Authorize]` become protected again on upgrade; one that was unintentionally depending on the open behaviour will see 403s. Two attribute families must stay semantically aligned, which the both-orders specs guard.
