---
title: "ARCCHR0026: Chronicle model read through a MongoDB collection"
description: Identify collection dependencies whose model is recognizably backed by Chronicle.
---

## Rule

An injected `IMongoCollection<T>` reads the MongoDB sink directly. When `T` is
recognizably backed by Chronicle, this analyzer reports **Info** outside On and
**Warning** under On, where runtime parameter admission refuses the collection.
A read through this collection is **not proven protected**. The analyzer does not
inspect queries or prove that the result contributes to a command decision.

Recognition includes Chronicle model-bound projection metadata on the model or its
members, projection/reducer targets declared in the compilation, and recognizable
self-reductions over attributed event types. An ordinary MongoDB model is not reported.

## Resolve the diagnostic

For a command's keyed state, inject the plain read model rather than its MongoDB
collection. See [read-model injection](../read-models/injecting-into-commands.md) for
the lookup and decision-read boundaries. This replacement alone does not prove
that the command's decision is protected.

An invariant that must hold independently of a command's read belongs in a Chronicle
constraint. Outside On, intentional sink queries can retain the collection dependency
and suppress this advisory with a justification. Under On, inject an admitted model
instead; a suppression does not bypass runtime refusal. There is no automatic code fix.

## Boundaries

Constructor and ordinary method parameters are examined, including primary constructors.
Lambda and local-function parameters are not dependency injection sites and are ignored.
The analyzer matches the full `MongoDB.Driver.IMongoCollection<T>` identity, not just its
short name. It does not recognize collection fields, local variables, indirect service
lookups, external fluent projection/reducer artifacts, or runtime-only event registration.
It does not change MongoDB access or Chronicle runtime behavior.
