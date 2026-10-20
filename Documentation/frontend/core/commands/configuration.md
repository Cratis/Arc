---
title: Command Configuration
description: Configure command microservice routing and API base path per command instance or once through the React Arc root.
---

Configure a command's service-routing header and API base path when its endpoint is behind a gateway or hosted under a path prefix. Set these values per instance for core TypeScript usage, or through the React `<Arc>` provider for commands created by React hooks.

## Microservice Routing

`setMicroservice(...)` selects a logical service by adding an HTTP header; it does not change the command's route or API origin. The default service name is empty, so no routing header is added. The header name comes from `Globals.microserviceHttpHeader`, whose default is `x-cratis-microservice`.

Per command:

```typescript
command.setMicroservice('user-service');
```

Your gateway or backend must interpret that header. Giving it a value does not create a service or configure backend routing.

Recommended global setup:

- Configure `microservice` on `<Arc>` for commands and queries created by its hooks.
- See [Arc Configuration](../../react/arc.md#microservice-support).

## API Base Path

`setApiBasePath(...)` supplies a path prefix joined with the command's declared `route`. The default prefix is empty. Keep the origin separate: `setOrigin(...)` selects the API origin, while the base path selects a location beneath it.

Per command:

```typescript
command.setApiBasePath('/api/v1');
```

For an existing generated command whose route is `/users/register`, a complete instance-level routing setup might be:

```typescript
command.setOrigin('https://api.example.com');
command.setApiBasePath('/api/v1');
command.setMicroservice('user-service');
const result = await command.execute();
```

This assumes the command's required values have already been supplied. The request goes to `https://api.example.com/api/v1/users/register` and carries the service-routing header. If a generated route already contains `/api`, do not add that segment again in the base path.

Core command constructors initially copy `microservice`, `apiBasePath`, and `origin` from `Globals`; setters replace those instance values. React's `useCommand` applies the nearest provider's values when it creates the command. Changing `Globals` later does not reconfigure an existing instance, and a manually constructed command does not read React context.

Recommended global setup:

- Configure `apiBasePath` on `<Arc>` for consistent routing across hook-created commands.
- See [Arc Configuration](../../react/arc.md#configuration-options).

## See Also

- [Command Contracts](./contracts.md)
- [Backend Integration](./integration.md)
- [Query Configuration](../queries/configuration.md)
