# Identity Contracts

Identity providers work with two key contracts: `IdentityProviderContext` as input and `IdentityDetails` as output.

## IdentityProviderContext

`IdentityProviderContext` contains the incoming identity data.

| Property | Description |
| -------- | ----------- |
| Id | The identity identifier from the identity provider |
| Name | The display name of the identity |
| Claims | Collection of `KeyValuePair<string, string>` claims from the token |

## IdentityDetails

`IdentityDetails` represents the provider result.

| Property | Description |
| -------- | ----------- |
| IsUserAuthorized | Whether the user is authorized to enter the application |
| Details | Domain-specific details as an object payload |

When `IsUserAuthorized` is `false`, Arc returns HTTP 403. When authorized, Arc returns HTTP 200.

> Note: Providers can use constructor dependencies via dependency inversion.
