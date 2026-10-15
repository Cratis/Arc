---
title: "ARCCHR0032: Inert or redundant read-protection declaration"
description: Remove only declarations that do not change the resolved policy.
---

## Rule

**Info**. ReadProtection is declared on a sealed/static non-command type,
Unprotected is declared on a provably non-model parameter, On repeats a
ProtectedDecision declaration, or Off repeats an already-Off assembly policy.

## Resolve the diagnostic

Remove the inert declaration, or place it on the intended command, assembly,
read-model parameter or model. Non-sealed bases are allowed to carry an inherited
ReadProtection declaration even when the base itself has no Handle method.

Off on a ProtectedDecision command in an On assembly is **not** redundant:
it preserves the explicit protected-decision behavior instead of enforcing On.
No automatic removal is offered because changing rollout policy deserves review.
