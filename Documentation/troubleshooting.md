---
title: Troubleshooting
description: Fixes for the issues that come up most when building with Arc — proxies, authorization, validation, and stale types.
---

Most Arc snags come down to a handful of causes. Here they are. For event-store and read-model issues, see [Chronicle troubleshooting](/chronicle/troubleshooting/).

## My frontend can't find the generated proxy

Proxies are generated when the **backend builds**. If the import doesn't resolve:

- Run `dotnet build` on the backend and confirm it succeeds — no proxies are emitted until the C# compiles.
- Check the command/query is discoverable: a `[Command]` record with a `Handle()` method, or a static query method on a `[ReadModel]`.
- Make sure proxy generation is targeting the right output folder for your frontend (see [Proxy Generation](./backend/proxy-generation/)).

## I changed the C# but the TypeScript is stale

The proxies regenerate on build. Rebuild the backend; the frontend types update with it. If a renamed property doesn't surface, you've found the feature working — the old name should now fail to compile until you update the call site.

## My command's OK/Submit button stays disabled

The command isn't considered valid. The usual cause: a required value is being set in `onBeforeExecute` instead of `initialValues`. `onBeforeExecute` runs at execution time — too late to affect validity — so the form never becomes valid. Put injected required values (like a parent id) in `initialValues`; reserve `onBeforeExecute` for generated values that don't gate validity (like a new `Guid`). See [Building a form](/components/building-a-form/).

## My command returns 401 or 403

That's authorization. Check that:

- the caller is authenticated, and
- the caller has the role the command requires (`[Roles(...)]` on the command — or on the query method for a 403 on reads).

For local development you can generate a principal so you can exercise authorized endpoints without a full login — see [Identity](./backend/identity/) and [Authorizing commands and queries](./backend/authorizing-commands-and-queries.md).

## My validation isn't firing

Arc discovers a `CommandValidator<TCommand>` by convention. Confirm the validator's generic type matches the command exactly, and that any async rule that needs a dependency takes it via the validator's constructor. See [Commands](./backend/commands/).

## My query returns nothing

This is usually the read side, not Arc: the projection that builds the read model is [eventually consistent](/chronicle/read-models/), or it doesn't map the events you appended. Walk through [Chronicle troubleshooting → my read model is empty](/chronicle/troubleshooting/).

## See also

- [Backend](./backend/) and [Frontend](./frontend/) guides.
- [Chronicle troubleshooting](/chronicle/troubleshooting/) for event-store issues.
