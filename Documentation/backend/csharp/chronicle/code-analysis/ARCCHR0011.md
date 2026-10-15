---
title: "ARCCHR0011: Plain read model in an Off command is unprotected"
description: Off state dependencies are hints, not guarded decisions.
---

## Rule

**Info**, under Off (including projects with no declared mode). An event-producing
command or its validator depends on Chronicle state without an explicit advisory
declaration. Plain reads under Off do not enroll a guard.

## Resolve the diagnostic

Enable `ReadProtection(On)` and take the model as a parameter. Prefer a
`CommandValidator<T>` constructor for state-dependent rules: under On its read
shares the command's protected unit. Under Off the same validator read is only a hint.
For intentional advisory context use `[Unprotected("why")]`.

## Boundaries

Measure and On are excluded, as are explicit decision-read parameters and
advisory declarations. Source-visible state dependencies and raw model reads are
recognized; external services and arbitrary helper dataflow are not proven.
No code fix is offered.
