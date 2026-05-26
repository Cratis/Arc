# Identity Provider Context

`IdentityProviderContext` contains the incoming identity data available to your provider.

| Property | Description |
| -------- | ----------- |
| Id | The identity identifier from the identity provider |
| Name | The display name of the identity |
| Token | Parsed principal data represented as a `JsonObject` |
| Claims | Collection of `KeyValuePair<string, string>` claims from the token |
