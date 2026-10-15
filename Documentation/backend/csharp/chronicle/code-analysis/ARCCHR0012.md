---
title: "ARCCHR0012: Immediate append bypasses protected decision"
description: Stage events for the owning command's guarded commit.
---

## Rule

**Info** for an explicit protected decision; **Warning** under On. A command
immediately appends through `IEventLog`. Immediate writes cannot be rolled back
after a decision conflict, and On refuses them even in Unprotected method bodies.

## Resolve the diagnostic

Return events from Handle or use `IEventLog.Transactional`. For a stream decision,
return `StreamDecision.Append`. A method-level advisory allowance does not permit
immediate writes.

## Boundaries

Transactional appends and ordinary Off commands without protected decisions are
excluded. The analyzer checks recognizable calls in command bodies, not arbitrary
aliases or helper dataflow. No code fix is offered.
