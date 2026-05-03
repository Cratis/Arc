# Validation And Results

Commands return `CommandResult<TResponse>` and perform validation before execution.

## Client-Side Validation

Validation metadata is generated from backend FluentValidation rules by the proxy generator.

```typescript
const command = new CreateUserCommand();
command.email = '';
command.age = 15;

const result = await command.execute();
// result.isValid === false
// result.validationResults contains validation errors
```

For deeper validation behavior, see [Validation](../validation/index.md) and [Validation](./validation.md).

## Result Shape

A command result includes status and diagnostics such as:

- Success/failure status
- Validation details
- Response payload (when present)
- Exception details

For full `CommandResult` details, see [Command Result](./command-result.md).

## Error Categories

Typical failure categories include:

- Validation errors
- Network and timeout failures
- HTTP status failures
- Domain/application exceptions

## See Also

- [Command Result](./command-result.md)
- [Validation](./validation.md)
- [React Commands](../../react/commands/index.md)
