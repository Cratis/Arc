---
id: 0013-cap-caller-validation-severity
title: Cap HTTP caller validation severity at Warning unless the command or host opts in
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Arc.Core/Commands/**
  - Source/DotNET/Tools/ProxyGenerator/**
  - Documentation/backend/csharp/commands/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

Decision [0005](0005-commands-declare-a-blocking-validation-severity.md) makes a command's blocking policy authoritative. The HTTP caller threshold also needs a default when a command declares no blocking policy. Callers can relax warnings, not errors, unless the command or host explicitly opts in.

## Decision

Cap the model-bound command HTTP caller severity at `Warning` by default, before built-in or custom pipeline dispatch. Ignore missing, non-numeric and out-of-range values. An inherited `[AllowCallerValidationSeverity]` replaces the host maximum; the host's `IgnoreCallerAllowedSeverity` takes precedence over both. Declared blocking policies and the read-protection-On floor remain authoritative in the built-in pipeline. A command's own assembly may declare a blocking policy, after the command/base-type chain and before the read-protection floor. Generated proxies resolve that assembly fallback too. This supplements, rather than supersedes, decision 0005.

## Options considered

- **HTTP boundary cap with explicit opt-in (chosen).** Covers custom pipelines without changing trusted in-process calls.
- **Cap in the built-in pipeline.** Changes in-process behavior and misses custom pipeline implementations.
- **Require a blocking declaration on every command.** Repeats configuration for the default case.

## Default if unanswered

Keep the previous caller-controlled behavior for commands without a policy. Applications must declare a blocking policy wherever errors must reject them. This costs repeated declarations and leaves the HTTP default different from the intended warning-confirmation workflow.

## Timeline and scope

Apply this contract with the next release that introduces the options and attribute; retain it for subsequent releases unless superseded. Scope is C# model-bound command HTTP severity and assembly blocking-policy projection. In-process `ICommandPipeline.Execute/Validate` semantics, controller validation, other language hosts, and atomic read consistency are unchanged.

## Verification

**Done when:** Error-severity results reject default HTTP execution and validation even with header `3`, the handler does not run, and an opted-in command can deliberately allow them without removing declared blocking policies or the read-protection-On floor.

**Verify by:** Run the caller-severity and endpoint specs in Arc.Core.Specs, the command scenarios in Chronicle.Specs, and the HTTP and assembly-policy scenarios in ProxyGenerator.Specs.

## Consequences

Applications intentionally using HTTP caller severity to relax errors must opt in on a command or configure the host maximum. Operators can ignore the header globally. Programmatic callers keep their current semantics. Severity filtering remains separate from authorization and atomic consistency enforcement.
