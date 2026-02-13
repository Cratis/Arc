# Code Analysis Rules

This section documents the code analysis rules provided by the Arc.Core code analyzer for .NET.

Arc.Core includes Roslyn analyzers that validate Arc constructs at compile time to catch errors early and enforce best practices.

All rules follow the identifier format `ARC####` where the numbers are sequential without gaps.

## Rules Overview

| Rule ID | Title | Severity | Description |
| --- | --- | --- | --- |
| [ARC0001](ARC0001.md) | Incorrect query method signature on ReadModel | Error | ReadModel query methods must return the ReadModel type or an allowed wrapper of it. |
| [ARC0002](ARC0002.md) | Missing [Command] attribute on command-like type | Warning | Command-like types must be marked with `[Command]` to be recognized as commands. |

## Quick Fixes

No automatic code fixes are currently provided for these rules.

## Installation

The analyzer is automatically included when you reference Arc.Core in your project. No additional configuration is required.

```xml
<ItemGroup>
    <ProjectReference Include="../Arc.Core/Arc.Core.csproj" />
</ItemGroup>
```
