---
id: 0005-commands-declare-a-blocking-validation-severity
title: Let a model-bound command declare the validation severity that blocks it, without a caller opt-out
status: accepted
stage: verified
class: contract
reversibility: costly
decided: 2026-09-26
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Arc.Core/Commands/**
  - Source/DotNET/Tools/ProxyGenerator/**
  - Source/JavaScript/Arc/commands/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

Arc blocks a command only on `Error` validation results unless the caller sends an allowed severity (`X-Allowed-Severity`). Controller commands gained an action-level `treatWarningsAsErrors` in #21, but model-bound commands had no way to say that their own warning or information failures must reject them (#2760). Models that treat every failed rule as a rejection, such as Screenplay's, could not be realized on model-bound commands without silently executing commands that should have been rejected.

## Decision

A model-bound command may declare `[BlockOnValidationSeverity(severity)]`. The declared severity is the inclusive minimum that blocks: `Information` blocks every failure, `Warning` blocks warnings and errors, `Error` keeps today's behavior. The attribute is inherited by derived command records.

The effective threshold is the stricter of the declaration and the caller's allowed severity. It is computed once and stored on the command context, so HTTP execution, validation, `Provide()`, and in-process `ICommandPipeline` execution all apply the same threshold. A caller can tighten a declared policy but never loosen it. That deliberately differs from #21, where the caller could opt out: a declared policy describes what the command means, not a presentation preference.

For a command carrying the attribute, a failure of `Unknown` severity blocks, so an unclassified failure never lets the command run. Commands without the attribute keep the existing filtering unchanged, including how `Unknown` is treated.

Generated TypeScript proxies carry the declared severity and apply the same threshold on the client, while the server remains authoritative. An out-of-range declared severity fails proxy generation and fails the command at run time.

## Options considered

- **Declared minimum severity with no caller opt-out (chosen).** Client and server agree, and the command's meaning cannot be weakened by a request header.
- **Declared severity that the caller can override.** Matches #21, but a header could make a command execute although its model says it must be rejected. Rejected.
- **Rule-level severity flags instead of a command attribute.** Finer-grained, but it spreads one policy over every validator and still needs a command-level answer for `Provide()` and in-process execution. Deferred.

## Default if unanswered

Model-bound commands block only on `Error` unless the caller asks for more. Consumers that must reject warning- or information-level failures have to refuse such commands instead of generating them.

## Verification

- **Done when:** a command with the attribute is rejected for warning and information failures with no header and with a permissive header, the handler is never invoked, and each result keeps its original severity and message; commands without the attribute behave exactly as before.
- **Verify by:** Arc.Core and Arc specifications for the pipeline, `Provide()`, validation, HTTP and in-process paths, including derived commands and a caller stricter than the declaration; proxy generator specifications for the emitted policy and the out-of-range failure; TypeScript command specifications for the client threshold.

## Consequences

Callers can no longer loosen validation for commands that declare a policy. Older generated proxies keep their current client behavior, and the server still enforces the policy. The attribute has no effect on controller-action commands or on a custom `ICommandPipeline`.
