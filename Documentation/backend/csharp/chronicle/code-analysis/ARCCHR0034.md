---
title: "ARCCHR0034: Unprotected method does not opt out read-model parameters"
description: Method advisory allowances start after parameter resolution.
---

## Rule

**Info**, in Measure or On. A method is `[Unprotected("why")]` but has a
non-advisory read-model parameter. The method attribute covers the body only;
parameters are resolved before that body starts.

## Resolve the diagnostic

If the parameter is intentionally advisory, annotate that parameter or the
read-model type with `[Unprotected("why")]`. Otherwise keep the protected
parameter and acknowledge the informational diagnostic with a justification.

## Boundaries

Off and parameters already declared advisory are excluded. The analyzer only
recognizes source-visible Chronicle-backed models. No code fix is offered.
