# Validation Results

Validation failures are returned in command/query results using a shared validation-result shape.

## Result Item Shape

```typescript
interface ValidationResult {
    severity: ValidationResultSeverity;
    message: string;
    members: string[];
    state: any;
}
```

Example:

```typescript
{
    severity: ValidationResultSeverity.Error,
    message: 'Email address is required',
    members: ['email'],
    state: null
}
```

## Severity Levels

- `Error`
- `Warning`
- `Information`
- `Unknown`

For command execution filtering by severity, see [Severity Filtering](./severity-filtering.md).

## Related

- [Command Result](../commands/command-result.md)
- [Validation](../commands/validation.md)
