---
title: "ARCCHR0028: Chronicle read is not proven protected"
description: Raw reads and explicit allowances in On command bodies.
---

## Rule

**Warning**, under On. A command or its validator calls a raw Chronicle read,
`GetDetached`, or `AllowReads`. These calls are not proven to enroll the read
through Arc's command-aware reader.

## Resolve the diagnostic

Take a read model as a Handle/Provide parameter or validator constructor
parameter. For a computed key, use `IDecisionReads.Get` in Provide.
For intentional advisory context, mark the method `[Unprotected("why")]`.
An explicit `AllowReads` remains reported as a reviewable bypass.

## Boundaries

Off, Measure and advisory method-body reads are excluded. The analyzer recognizes
framework APIs by their declaring namespace and type; unrelated methods with the
same names are not reported. It does not prove transitive helper calls or dynamic
runtime behavior. No code fix is offered.
