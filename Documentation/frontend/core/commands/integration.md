---
title: Cratis Arc command proxy integration
description: Cratis Arc generates TypeScript command clients from recognized C# endpoints, with route and response metadata and optional React adapters.
---

A generated command proxy connects your TypeScript client to a recognized C# command endpoint in Cratis Arc. It lets you submit commands without hand-writing HTTP requests or duplicating request and response types. The generated class carries the endpoint's route and property metadata; the core `Command` runtime uses that metadata when you call `execute()`.

## Backend Command Styles

The generator discovers both of these backend styles:

- [Controller-based Commands](../../../backend/csharp/commands/controller-based.md), exposed through controller actions.
- [Model-bound Commands](../../../backend/csharp/commands/model-bound/index.md), with handling behavior on the command model.

Both styles can produce typed frontend command classes. Choose the backend style that fits your application; calling the generated command does not require a React component.

## Proxy Generation Benefits

The generated properties and response types give your editor completion and let the TypeScript compiler check how you use the backend contract. Routes, request serialization, and response conversion come from the proxy and core runtime, leaving application code to populate the command and handle its result.

With generation configured, the MSBuild target runs after the backend build. After changing a backend contract, regenerate the proxies and compile the frontend to find affected callers. Generation does not establish that a deployed backend matches your client, so keep those versions aligned. Keep handwritten application code outside generated files because regeneration can replace them.

Follow [Backend Proxy Generation](../../../backend/csharp/proxy-generation/index.md) to configure the output path and frontend dependencies.

## React Layer

Proxy generation includes React imports and a static `use()` method by default. For a client that does not use React, set `CratisProxiesFramework` to `none` in the backend project's proxy-generation configuration; the executable equivalent is `--framework=none`. Command classes still extend the core `Command` runtime, but omit the React adapter.

If you do use React, [React Commands](../../react/commands/index.mdx) explains the hook-based lifecycle. The validation and result handling in this core section also applies when you instantiate commands directly.
