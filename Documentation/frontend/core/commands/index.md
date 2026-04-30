# Commands

Core commands in Arc are the low-level TypeScript/JavaScript primitives used to execute state-changing operations.

This section focuses on command contracts and runtime behavior in `@cratis/arc`. For React-specific ergonomics, see [React Commands](../../react/commands/index.md).

## Capabilities

| Capability | What It Covers | Learn More |
| ---------- | -------------- | ---------- |
| Command contracts | `ICommand`, route metadata, execution shape, and command state model | [Command Contracts](./contracts.md) |
| Runtime configuration | Microservice routing, API base path, and global Arc configuration | [Configuration](./configuration.md) |
| Validation and results | Client-side validation, `CommandResult`, and error categories | [Validation And Results](./validation-and-results.md) |
| Backend integration | Controller-based/model-bound backend mapping and proxy generation | [Backend Integration](./integration.md) |

## Related Documentation

- [CommandResult](../command-result.md)
- [Command Validation](../command-validation.md)
- [Validation](../validation/index.md)
- [Backend Commands](../../../backend/commands/index.md)
