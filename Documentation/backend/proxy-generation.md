---
title: Proxy generation
description: Entry point to Arc's compiled-assembly TypeScript proxy generator.
---

Arc generates TypeScript command/query clients and model types from your compiled backend. It runs as a post-build executable, not as a Roslyn source generator, and does not require Chronicle.

## Overview

Start with the [proxy generation overview](proxy-generation/index.md), then [set up generation](proxy-generation/getting-started.md). The detailed reference lives in that section; this page remains as a compatibility entry point.

## Enum generation

Referenced C# enums become numeric TypeScript enums with camelCase members. See [enum generation and examples](proxy-generation/type-mapping.md#enum-generation).

### Flags enums

`[Flags]` enums also export an `all<EnumName>` bitwise-OR constant combining nonzero members. The [flags enum example](proxy-generation/type-mapping.md#flags-enums) preserves the complete `AnchorEdges` example and its zero-member behavior.

## Configuration

Use the [configuration reference](proxy-generation/Configuration/index.md) for output folders, route settings, and library generation. Read [output behavior](proxy-generation/Configuration/output-behavior.md) before enabling destructive cleanup.

## Usage

Inspect the generated [command API](proxy-generation/commands.md) and [query hook tuples](proxy-generation/queries.md), then use them in your frontend. Models default to classes with serialization metadata, not plain interfaces.

## See also

- [Backend commands](commands/index.md)
- [Backend queries](queries/index.md)
- [Validation extraction](proxy-generation/validation.md)
