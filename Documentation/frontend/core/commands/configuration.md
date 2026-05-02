# Command Configuration

Command routing and endpoint settings can be configured per command instance, but are usually configured once through the React `<Arc>` root component.

## Microservice Routing

Per command:

```typescript
command.setMicroservice('user-service');
```

Recommended global setup:

- Configure `microservice` on `<Arc>` so every command and query uses the same service routing strategy.
- See [Arc Configuration](../../react/arc.md#microservice-support).

## API Base Path

Per command:

```typescript
command.setApiBasePath('/api/v1');
```

Recommended global setup:

- Configure `apiBasePath` on `<Arc>` for consistent routing across the application.
- See [Arc Configuration](../../react/arc.md#configuration-options).

## See Also

- [Command Contracts](./contracts.md)
- [Backend Integration](./integration.md)
