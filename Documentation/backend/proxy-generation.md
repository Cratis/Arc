# Proxy Generation

The TypeScript proxy generator automatically creates strongly-typed client code for your commands, queries, and types — ensuring compile-time type safety between your backend and frontend without any manual synchronization.

## Overview

The proxy generator analyzes your backend API at build time and generates:

- TypeScript interfaces for all request and response types
- Strongly-typed proxy classes for invoking commands and queries
- TypeScript enums for all C# enum types referenced by your API
- Full IntelliSense support in your IDE

## Enum Generation

C# enums referenced by any command, query, or type are automatically discovered and generated as TypeScript enums. Member names are converted to camelCase.

**C# source:**

```csharp
public enum ReadModelStatus
{
    Unknown = 0,
    Active = 1,
    Inactive = 2,
    Archived = 3
}
```

**Generated TypeScript:**

```typescript
export enum ReadModelStatus {
    unknown = 0,
    active = 1,
    inactive = 2,
    archived = 3,
}
```

### Flags Enums

Enums decorated with `[Flags]` receive special treatment: the generator uses a dedicated template that, in addition to the enum declaration, emits an `allXxx` constant combining every non-zero member with the bitwise OR operator. This constant is useful when you need to represent "all flags set" or build a full bitmask without repeating every member name by hand.

**C# source:**

```csharp
[Flags]
public enum AnchorEdges
{
    None = 0,
    Top = 1 << 0,
    Right = 1 << 1,
    Bottom = 1 << 2,
    Left = 1 << 3,
}
```

**Generated TypeScript:**

```typescript
export enum AnchorEdges {
    none = 0,
    top = 1,
    right = 2,
    bottom = 4,
    left = 8,
}

export const allAnchorEdges = AnchorEdges.top | AnchorEdges.right | AnchorEdges.bottom | AnchorEdges.left;
```

The `allAnchorEdges` constant excludes `none` (value `0`) because a zero-valued member contributes nothing to a bitwise OR expression. The name follows the convention `all<EnumName>` and is exported alongside the enum.

## Configuration

Configuration details for the proxy generator will be documented here.

## Usage

Usage examples and best practices will be provided in this section.

## See Also

- [Commands](./commands/index.md) - Learn about command patterns
- [Queries](./queries/index.md) - Learn about query patterns
