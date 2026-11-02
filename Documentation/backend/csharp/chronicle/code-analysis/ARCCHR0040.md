<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# ARCCHR0040 — Prefer a read-model parameter

| Property | Value |
| --- | --- |
| Severity | Info |
| Code fix | None |

## What it means

A public instance `Handle()` on a recognized command takes `IDecisionReads` or `DecisionRead<T>` directly. Prefer declaring the read model as a `Handle` parameter for supported acquisition shapes. `IDecisionReads` is the reader service; `DecisionRead<T>` is an issued version-token wrapper, not a reader service.

A command's `Handle()` may take any service dependencies. These two dependencies are almost never needed for ordinary model acquisition, but remain available. The analyzer checks real framework type identities, not short names. It does not police other services, non-commands, `Provide()`, other methods, generated code or the Chronicle runtime implementation.

## What to do

Use a plain model parameter only when the existing binding expresses the key, optionality and route you need. Preserve read protection: plain models under Off or Measure are not guarded reads. ARCCHR0011 and ARCCHR0012 are unchanged. No automatic rewrite is offered because replacing an explicit token can change concurrency semantics.

If a real acquisition gap requires the reader or token, suppress ARCCHR0040 locally with a reason beside the declaration. Do not relocate it to `Provide()` just to silence this guidance. [Read models as command state inputs](../read-models/command-state-inputs.md) lists supported shapes, unresolved gaps and a reasoned suppression example.

ARCCHR0040 adds advice only: it changes no runtime behavior. ARCCHR0039 remains retired.
