# Identity Details

Providers return `IdentityDetails` with authorization state and an application-defined details payload.

| Property | Description |
| -------- | ----------- |
| IsUserAuthorized | Whether the user is authorized to enter the application |
| Details | Domain-specific details as an object payload |

When `IsUserAuthorized` is `false`, the endpoint returns HTTP 403. When authorized, it returns HTTP 200.

> Note: Dependency inversion applies, so your provider can take constructor dependencies as needed.
