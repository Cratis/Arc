# Validation Results

Validation failures are returned in command/query results using a shared validation-result shape.

## Result Item Shape

```typescript
interface ValidationResult {
    severity: ValidationResultSeverity;
    message: string;
    members: string[];
    state: unknown;
    reason: string;
    reasonDetail?: string;
}
```

Example:

```typescript
{
    severity: ValidationResultSeverity.Error,
    message: 'Email address is required',
    members: ['email'],
    state: null,
    reason: 'rule'
}
```

This is a structural reference fragment, not a replacement for importing `ValidationResult` from `@cratis/arc/validation`. The runtime class defaults `reason` to `rule`.

`reason` is an open string set. Known values include `rule`, `validatorFailed`, `dependencyUnavailable`, and `malformedRequest`. Optional Chronicle integration can also produce `concurrencyViolation` and `constraintViolation`; these do not imply that standalone Arc uses an event store. `reasonDetail` identifies a finer cause where supplied. Do not match message prose to distinguish failures or reject unfamiliar reason strings.

Show authored rule messages only when suitable for your audience; map framework diagnostics to your own user-facing copy. `state` is application-authored metadata and must not contain secrets.

## Severity Levels

- `Error`
- `Warning`
- `Information`
- `Unknown`

For command execution filtering by severity, see [Severity Filtering](./severity-filtering.md).

## Related

- [Command Result](../commands/command-result.md)
- [Validation](../commands/validation.md)
