---
title: Generate a Microsoft identity principal for local development
description: Simulate trusted-ingress principal headers on a loopback-only development host, without confusing Base64 assertions with validated bearer tokens.
---

Need to exercise different users and roles before wiring production login? The [Microsoft identity header adapter](../backend/asp-net-core/microsoft-identity.md) can construct a principal from development HTTP headers. This is **identity simulation**, not token validation.

## Understand the trust boundary first

`x-ms-client-principal` is Base64-encoded JSON — an **unsigned assertion**, not a signed bearer token. Arc's header handlers decode it and construct claims; they do not validate a signature, issuer, or audience. Header presence and valid JSON do not prove who sent it.

Use manually supplied headers only on an isolated, loopback-bound development host. In production, either configure a real token/cookie authentication scheme or accept these assertions only from a trusted authenticated ingress. That ingress must **strip and replace all incoming identity headers**, and the backend must not be reachable through a bypass route. An arbitrary browser or proxy sending these headers is not trustworthy.

The [authorization tutorial](/arc/tutorial/authorization/) shows the ASP.NET registration and middleware placement for a Development-only fixture. The [lightweight host authentication](../backend/core/authentication.md) is a different setup; do not mix their APIs.

## Construct the assertion

The adapter expects all three headers:

| Header                       | Content                                                                                                                                                        |
| ---------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `x-ms-client-principal`      | Base64 JSON following the [client principal shape](https://learn.microsoft.com/en-us/azure/static-web-apps/user-information?tabs=csharp#client-principal-data) |
| `x-ms-client-principal-id`   | The selected user's stable identifier                                                                                                                          |
| `x-ms-client-principal-name` | The selected user's display name or email                                                                                                                      |

Save this synthetic fixture as `principal.json` in your local application workspace:

```json
{
    "identityProvider": "aad",
    "userId": "e7f664ca-4ecc-45be-84cf-74b6240d049a",
    "userDetails": "jane.doe@contoso.com",
    "userRoles": ["anonymous", "authenticated", "Librarian"],
    "claims": [
        {
            "typ": "preferred_username",
            "val": "jane.doe@contoso.com"
        },
        {
            "typ": "name",
            "val": "Jane Doe"
        },
        {
            "typ": "given_name",
            "val": "Jane"
        },
        {
            "typ": "family_name",
            "val": "Doe"
        },
        {
            "typ": "oid",
            "val": "e7f664ca-4ecc-45be-84cf-74b6240d049a"
        }
    ]
}
```

`userRoles` is mapped to role claims by the header adapter. Include `Librarian` for the successful tutorial case; remove it for an authenticated-but-not-authorized case. A role field returned by `IProvideIdentityDetails` would not have the same effect.

Encode locally, keeping the result on one line:

```bash
python3 -c 'import base64,pathlib; print(base64.b64encode(pathlib.Path("principal.json").read_bytes()).decode())'
```

A local editor's Base64 action is another option. Do not submit real identity payloads to an online encoder. Base64 provides neither confidentiality nor authenticity.

## Configure ModHeader narrowly

In [ModHeader](https://modheader.com), create a development-only profile and **add an include URL filter before enabling it**. For the tutorial Vite server, use `^http://localhost:5173/.*$`. Add a separate `^http://localhost:5000/.*$` only when testing that backend directly. If you use a different local port, update the filter; never select all requests.

Set:

- `x-ms-client-principal-id`: `e7f664ca-4ecc-45be-84cf-74b6240d049a`
- `x-ms-client-principal-name`: `jane.doe@contoso.com`
- `x-ms-client-principal`: the one-line Base64 output above

> [!WARNING]
> The older screenshot below illustrates the header fields only. Its **“All requests”** setting is unsafe and must not be copied. Apply the localhost include filter above, and disable the profile when finished.

![Older ModHeader header-field example; replace its All requests scope with a localhost include filter](./configure-mod-header.png)

Use separate filtered profiles for different synthetic users. Confirm in browser developer tools that headers are sent only to your intended localhost application.

## Verify the distinct behaviors

- No headers: protected operations reject an unauthenticated caller.
- Valid fixture without `Librarian`: authentication succeeds but role-protected operations reject access.
- Fixture with `Librarian`: role-protected operations can execute, subject to their other checks.
- Identity-details provider returning `IsUserAuthorized: false`: `/.cratis/me` rejects that identity result. This does **not** replace operation authorization.

These checks test the adapter and your authorization configuration, not production token verification. For production, independently test ingress header replacement, blocked direct backend access, and the real authentication scheme. See [identity and access](/arc/understanding-identity-and-access/) for how claims, details, operation permissions, and tenant membership differ.
