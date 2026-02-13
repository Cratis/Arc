# Microsoft Identity

Cratis' Arc provides a way to easily work with providing an object that represents properties the application finds important for describing
the logged in user. The purpose of this is to provide details about the logged in user on the ingress level of an application and letting it
provide the details on the request going in. Having it on the ingress level lets you expose the details to all microservices behind the ingress.

The values provided by the provider are values that are typically application specific and goes beyond what is already found in the token representing the user.
This is optimized for working with Microsoft Azure well known HTTP headers passed on by the different app services, such as Azure ContainerApps or WebApps.
Internally, it is based on the following HTTP headers to be present.

| Header | Description |
| ------ | ----------- |
| x-ms-client-principal | The token holding all the details, base64 encoded [Microsoft Client Principal Data definition](https://learn.microsoft.com/en-us/azure/static-web-apps/user-information?tabs=csharp#client-principal-data) |
| x-ms-client-principal-id | The unique identifier from the identity provider for the identity |
| x-ms-client-principal-name | The name of the identity, typically resolved from claims within the token |

> Important note: Since local development is not configured with the identity provider, but you still need a way to test that both the backend and the frontend
> deals with the identity in the correct way. This can be achieved by creating the correct token and injecting it as request headers using
> a browser extension. Read more about [generating principal tokens for local development](../general/generating-principal.md).

The token in the `x-ms-client-principal` should be a base64 encoded [Microsoft Client Principal Data definition](https://learn.microsoft.com/en-us/azure/static-web-apps/user-information?tabs=csharp#client-principal-data).

## Authentication / Authorization

To get the Microsoft Client Principal supported in your backend, the Arc offers an `AuthenticationHandler` that supports the HTTP headers and
does the right thing to put ASP.NET Core and every `HttpContext` in the right state.

You can add this by calling the `AddMicrosoftIdentityPlatformIdentityAuthentication()` method on your services.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMicrosoftIdentityPlatformIdentityAuthentication();
```

The above code will then also call the `.AddAuthentication()` with the default scheme name (**MicrosoftIdentityPlatform**) and register
the appropriate `AuthenticationHandler` for that scheme.

You can override the scheme name on the extension method by passing your own string as an argument.

For it to be appropriately setup, you'll need to enable the default authentication and authorization on your app, like below:

```csharp
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

## Identity Details

For information about providing additional identity details for logged-in users, including authorization checks and custom identity information, see the [Identity documentation](identity.md).

The Microsoft Identity integration works seamlessly with the generic identity system to provide domain-specific information beyond what's available in identity provider tokens.
