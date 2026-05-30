---
title: Arc
description: The full-stack application framework for Cratis — CQRS commands and queries with end-to-end type safety, from C# to React.
---

Arc is the full-stack application framework. It turns **commands** and **queries** into a CQRS application, wires them to [Chronicle](/chronicle/) event sourcing, and **generates TypeScript proxies** so your React frontend stays in lockstep with your C# backend — no hand-written API client, no DTO duplication.

[![NuGet](https://img.shields.io/nuget/v/Cratis.Arc?logo=nuget)](http://nuget.org/packages/Cratis.Arc)
[![NPM](https://img.shields.io/npm/v/@cratis/arc?label=@cratis/arc&logo=npm)](https://www.npmjs.com/package/@cratis/arc)

## Why it exists

Building a modern full-stack app normally means writing the same plumbing over and over: controllers, DTOs, validation duplicated on both sides, and a fetch layer that drifts from the backend. Arc removes that plumbing with conventions — a command is a record with a `Handle()` method, a query is a method on a read model, and the typed client is generated for you. You spend your time on behavior, organized in [vertical slices](./vertical-slices.md) where everything for one feature — backend and frontend — lives together.

→ Read [Why Arc](./why-arc.md) for the full rationale.

## Start here

- **New to Arc?** Build a backend slice in [Your first command and query](./backend/getting-started/your-first-command.md), then wire a UI in the [frontend getting started](./frontend/getting-started.md).
- **Coming from MediatR or ASP.NET MVC?** The [bridge guide](./coming-from-mediatr-and-mvc.md) maps what you know onto Arc.
- **Want the whole picture?** [Build a full-stack feature](/build-a-full-app/) takes one slice from command to React screen.

## Explore

- [Backend](./backend/) — commands, queries, validation, identity, tenancy, proxy generation, persistence.
- [Frontend](./frontend/) — React integration, command forms, observable queries, and the MVVM option.
- [General](./general/) — cross-cutting topics.

Arc is designed to sit on top of Chronicle, but you can adopt it incrementally — see [Why Cratis](/why-cratis/) for how the pieces fit.
