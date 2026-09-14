# Identity Contracts

Identity providers work with two key contracts: `IdentityProviderContext` as input and `IdentityDetails` as output.

## IdentityProviderContext

`IdentityProviderContext` contains the incoming identity data.

| Property | Description |
| -------- | ----------- |
| Id | The identity identifier from the identity provider |
| Name | The display name of the identity |
| Claims | Collection of `KeyValuePair<string, string>` claims from the authenticated request principal, not a token parsed by this provider |

## IdentityDetails

`IdentityDetails` represents the provider result.

| Property | Description |
| -------- | ----------- |
| IsUserAuthorized | Whether the user is authorized to enter the application |
| Details | Domain-specific details as an object payload |

On `/.cratis/me`, a result marked unauthenticated yields HTTP 401; one marked authenticated but unauthorized yields 403; an authenticated and authorized result yields 200. These flags can come from a cached client-controlled cookie, not necessarily a fresh provider invocation. `IsUserAuthorized` does not add claims or authorize commands/queries; see [provider flow](provider-flow.md).

> Note: Providers can use constructor dependencies via dependency inversion.
