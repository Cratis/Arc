---
title: "ARCCHR0022: Command-key aggregate on a generated append target"
description: Align the aggregate's source with the generated target used for returned or appended events.
---

## Rule

An automatically injected aggregate is rehydrated using the command's event source id.
A `[GeneratedValue]` Handle parameter does not change that id before dependencies resolve.
Returning or appending events for the generated id therefore does not make the injected
aggregate belong to that target. **Default severity: Warning.**

The rule conservatively recognizes commands with an `EventSourceId` or `EventSourceId<T>`
property, a concrete aggregate parameter, and a directly returned `EventForEventSourceId`
(or returned collection expression of wrappers) targeting a generated parameter. It also
recognizes direct `IEventSequence.Append*` calls with that generated parameter as their source.
It reports on the aggregate parameter.

## Fix

Decide which source the command acts on. To act on the command's existing source, return
or append events for that source. To act on the generated source, remove the automatically
injected aggregate and resolve the intended aggregate explicitly, or redesign creation so
it does not need existing state from the wrong stream. There is no automatic code fix.

## Boundaries

Commands with `Provide()` or `ICanProvideEventSourceId` are excluded: the analyzer cannot
prove their aggregate resolution. Keyed service injection is excluded too. Indirect targets,
wrapper aliases, helper calls, custom key-only properties, and unrecognized append surfaces
can be missed. A quiet analyzer is not proof that the aggregate and append source agree.

See [event source identity timing](../resolving-event-source-id.md) and
[generated values](../../commands/generated-values.md) before changing the persistence target.
