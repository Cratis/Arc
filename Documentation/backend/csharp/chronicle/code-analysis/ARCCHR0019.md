---
title: ARCCHR0019 - Read-model fold may be exposed as a query
description: Preview explicit endpoint selection when a conventional query resembles a fold.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## When this warning appears

A conventional `[ReadModel]` has a public/internal static `Create(event[, context])` or `Reduce(event, nullable current[, EventContext])` returning its model or `Task` of the model. The first parameter resembles an event: a class or interface other than a string or concept. This is a heuristic, not proof of event registration, and it does not change query discovery.

In `RequireQueryAttribute = true` mode, unmarked helpers produce no query diagnostics, including this warning. Arc does not globally reserve fold or callback names.

## Choose the endpoint surface

If the method is a helper, [opt the read model into explicit query selection](../../queries/model-bound/static-methods.md#choose-endpoints-explicitly-when-a-model-has-helpers). Add `[Query]` to intended queries, not to the fold. Preserve endpoint paths, HTTP methods, arguments, and authorization.

If the method is an intentional query, keep it and suppress this heuristic with a justification. Adding an event attribute or context parameter does not exclude it from conventional discovery.

## Quick fix

**Require [Query] and mark existing queries (preview endpoints)** changes the whole read model, including its partial declarations. It opts the type into explicit selection and marks eligible existing methods other than recognized fold-like shapes. Preview the complete endpoint diff: return shapes cannot distinguish an intentional query from every other helper. The fix is not offered for already-invalid explicit query declarations, and it has no unattended Fix All.

This warning does not implement Chronicle static reducers or complete fold/query role-conflict validation.
