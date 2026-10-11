---
title: ARCCHR0019 — Read-model creator may be exposed as a query
description: Keep an unrecognized fold creator out of Arc query discovery.
---

## What triggers it

This warning reports a public or internal static `Create` on an Arc `[ReadModel]` when it returns that read model, `Task<T>`, or `ValueTask<T>`, has one or two parameters, and its first parameter is a non-primitive, non-concept class or interface.

There is no warning when the method has `[NotAQuery]`, a parameter is Chronicle's `EventContext`, or the first parameter's type or a base type has `[EventType]`. Arc already excludes those creators automatically.

## How to fix it

If this is a fold creator, mark it `[NotAQuery]` from `Cratis.Arc.Queries.ModelBound`, or add an `EventContext` parameter. This is useful for unmarked, explicitly registered events and polymorphic interface event parameters that query discovery cannot identify as events.

A real `Create` query taking a concrete service can also match this heuristic. Keep that query and suppress ARCCHR0019 with a justification instead of adding `[NotAQuery]`.

See [static query methods](../../queries/model-bound/static-methods.md#not-a-query) for the full discovery rule.

## Quick fix

**Add [NotAQuery]** adds the method-level opt-out without changing its signature or body. It removes the method from HTTP query discovery, generated query metadata and TypeScript proxies, query diagnostics, Screenplay query extraction, and `QueryScenario`. Direct C# calls are unchanged.
