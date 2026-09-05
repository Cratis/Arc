# IdentityProvider Service

`IIdentityProvider` gives you advanced control over identity processing during HTTP requests.

It is useful for stateless applications that need to associate request-specific selections or preferences with the current user.

## Purpose

`IIdentityProvider` lets you:

- Get identity results from the identity cookie or current HTTP context
- Write identity information to the response as cookies and JSON
- Modify identity details during a request

## Key Methods

| Method | Description |
| ------ | ----------- |
| `Get()` | Gets an `IdentityProviderResult` from the identity cookie when available, otherwise from the current HTTP context |
| `Get<TDetails>()` | Gets an `IdentityProviderResult<TDetails>` with strongly-typed details |
| `SetCookieForHttpResponse(IdentityProviderResult)` | Writes the identity result to the response as both a cookie and JSON |
| `ModifyDetails<TDetails>(Func<TDetails, TDetails>)` | Modifies identity details stored in the identity cookie |

## Use Case: Modifying User Preferences in Stateless Applications

```csharp
public class UserPreferencesController : ControllerBase
{
    private readonly IIdentityProvider _identityHandler;

    public UserPreferencesController(IIdentityProvider identityHandler)
    {
        _identityHandler = identityHandler;
    }

    [HttpPost("set-department")]
    public async Task<IActionResult> SetDepartment([FromBody] string department)
    {
        await _identityHandler.ModifyDetails<UserDetails>(details =>
            details with { SelectedDepartment = department });

        return Ok();
    }
}
```
