# Code Analysis Rules

This section documents the code analysis rules provided by the Chronicle code analyzer for .NET.

Chronicle includes Roslyn analyzers that validate aggregate root event handler signatures at compile time to catch errors early and enforce best practices.

All rules follow the identifier format `ARCCHR####` where the numbers are sequential without gaps.

## Rules Overview

| Rule ID | Title | Severity | Description |
| --- | --- | --- | --- |
| [ARCCHR0001](ARCCHR0001.md) | Incorrect aggregate root event handler signature | Error | Aggregate root event handlers must follow allowed `On` method signatures. |

## Quick Fixes

No automatic code fixes are currently provided for these rules.

## Installation

The analyzer is automatically included when you reference Chronicle in your project. No additional configuration is required.

```xml
<ItemGroup>
    <ProjectReference Include="../Chronicle/Chronicle.csproj" />
</ItemGroup>
```
