---
title: "ARCCHR0035: Protected aggregate is committed too early"
description: Complete the protected unit only after the outer decision is finished.
---

## Rule

**Warning**, under On. `aggregate.Commit()` is followed by a read or append in
the same straight-line block, or the command is statically invoked through
`ICommandPipeline.Execute` from another On command. Commit completes the unit;
a nested command must leave completion to its owner.

## Resolve the diagnostic

Read before committing and return Commit last in the outer command. Pass values
to nested commands rather than committing there.

## Boundaries

Top-level pipeline calls, Off/Measure commands and mutually exclusive branches
are not reported. The analyzer does not infer arbitrary control flow, opaque
helpers or runtime command dispatch. Runtime completion checks still apply.
No code fix is offered.
