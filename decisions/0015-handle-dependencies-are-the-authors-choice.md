---
id: 0015-handle-dependencies-are-the-authors-choice
title: Let command Handle methods take any dependency, with no diagnostic
status: accepted
stage: implemented
decided: 2026-11-02
decider: Einar Ingebrigtsen
class: product
reversibility: easy
supersedes: 0014-prefer-idiomatic-command-decisions-and-state-based-specs
applies-to:
  - Source/DotNET/Chronicle.CodeAnalysis/**
  - Documentation/backend/csharp/chronicle/code-analysis/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[0014](0014-prefer-idiomatic-command-decisions-and-state-based-specs.md) said `Handle(IDecisionReads)` and `Handle(DecisionRead<T>)` are supported advanced forms and that ARCCHR0039 gives Info-level guidance against them. The project owner has decided that Arc should not police which dependencies a command takes (Cratis/Arc#3618).

## Decision

A command's `Handle()` may take any dependencies its author wants. A read model is one kind of dependency, and so are `IDecisionReads` and `DecisionRead<T>`. No analyzer reports a Handle parameter because of its type.

ARCCHR0039 is removed. Its ID is retired and is never reused. The documentation may still describe plain read-model parameters as the simplest form, but it does not discourage the others.

This supersedes only the sentence in 0014 that assigns ARCCHR0039 Info-level guidance. The rest of 0014 remains in force, including idiomatic examples and state-based application specifications.

## Options considered

- **Remove the diagnostic (chosen).** Authors choose their dependencies, and runtime protection is unchanged.
- **Keep it as an opt-in or hidden diagnostic.** Still implies the forms are discouraged, and adds a rule to maintain.

## Default if unanswered

ARCCHR0039 would keep reporting advice on every `IDecisionReads` or `DecisionRead<T>` Handle parameter.

## Timeline and scope

Apply now to Cratis/Arc#3618. The change is limited to the analyzer and its documentation. Runtime behavior, ARCCHR0011 and ARCCHR0012 are unchanged.

## Verification

**Done when:** the Chronicle analyzer reports nothing for a Handle taking `IDecisionReads` or `DecisionRead<T>`, and ARCCHR0039 appears in no active list.

**Verify by:** the `for_CommandDecisionReadAnalyzer` specs and the documentation authoring check.

## Consequences

Authors no longer see advice against decision-reader parameters. A package consumer who suppressed ARCCHR0039 in an editorconfig can delete that entry.
