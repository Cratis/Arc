# Command And Query Integration

Validation runs automatically before command execution and query performance.

## Commands

```typescript
const command = new CreateUserCommand();
command.email = '';
command.age = 15;

const result = await command.execute();
// result.isValid === false
```

## Queries

```typescript
const query = new SearchUsersQuery();
query.parameters = { searchTerm: 'ab', minAge: -5 };

const result = await query.perform();
// result.isValid === false
```

## Backend-Governed Validation

Rules are defined on the backend and extracted by proxy generation:

- Single source of truth for validation rules
- Consistent frontend/backend behavior
- Type-safe generated validators

See [Backend Command Validation](../../../backend/commands/validation.md) and [Backend Query Validation](../../../backend/queries/validation.md).

## Related

- [Rules And Fluent API](./rules.md)
- [Validation Results](./results.md)
- [Severity Filtering](./severity-filtering.md)
