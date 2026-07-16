# Authorization

Model-bound commands support authorization through standard ASP.NET Core authorization attributes as well as the convenient `[Roles]` attribute provided by the Arc.

## Using the Authorize Attribute

You can secure commands using the standard `[Authorize]` attribute at the class level:

```csharp
[Command]
[Authorize]
public record AddItemToCart(string Sku, int Quantity)
{
    public void Handle(ICartService carts)
    {
        carts.AddItemToCart(Sku, Quantity);
    }
}
```

With role requirements:

```csharp
[Command]
[Authorize(Roles = "Admin,Manager")]
public record DeleteProduct(ProductId Id)
{
    public void Handle(IProductService products)
    {
        products.Delete(Id);
    }
}
```

## Using the Roles Attribute

The Arc provides a more convenient `[Roles]` attribute for cleaner syntax when specifying multiple roles:

```csharp
[Command]
[Roles("Admin", "Manager")]
public record UpdateProductPrice(ProductId Id, decimal NewPrice)
{
    public void Handle(IProductService products)
    {
        products.UpdatePrice(Id, NewPrice);
    }
}
```

The user needs to have **at least one** of the specified roles to execute the command.

## Anonymous Access with AllowAnonymous

Use `[AllowAnonymous]` to allow public access to specific commands. This is particularly useful when you have a global authorization requirement but need certain commands to be accessible without authentication:

```csharp
[Command]
[AllowAnonymous]
public record RegisterUser(string Email, string Password)
{
    public void Handle(IUserService users)
    {
        users.Register(Email, Password);
    }
}
```

### Combining with Global Authorization

When your application has global authorization requirements (e.g., via `[Authorize]` on controllers or through middleware), you can use `[AllowAnonymous]` to exempt specific commands:

```csharp
// This command can be executed without authentication
// even if global authorization is configured
[Command]
[AllowAnonymous]
public record RequestPasswordReset(string Email)
{
    public void Handle(IPasswordResetService service)
    {
        service.SendResetEmail(Email);
    }
}

// This command requires authentication
[Command]
[Authorize]
public record ChangePassword(string CurrentPassword, string NewPassword)
{
    public void Handle(IPasswordService service)
    {
        service.ChangePassword(CurrentPassword, NewPassword);
    }
}
```

### Common Use Cases for AllowAnonymous

- **User registration** - New users need to create accounts before they can authenticate
- **Password reset requests** - Users who forgot their password can't authenticate
- **Public data submissions** - Contact forms, feedback submissions
- **Health checks or status endpoints** - System monitoring that shouldn't require authentication

## Policy-Based Authorization

For more complex authorization scenarios, you can use policy-based authorization:

```csharp
[Command]
[Authorize(Policy = "RequireElevatedAccess")]
public record PerformSensitiveOperation(string Data)
{
    public void Handle(ISensitiveOperationService service)
    {
        service.Execute(Data);
    }
}
```

## Authorization Results

When authorization fails, the command pipeline returns an unauthorized result. The command handler will not be executed:

```csharp
var result = await commandPipeline.Execute(command);

if (!result.IsAuthorized)
{
    // Handle unauthorized access
    // The command was not executed
}
```

## Executing Commands from Server-Side Code

Authorization reads the principal from the current HTTP request. Server-side callers — reactors, hosted services, background jobs, sagas, or one command orchestrating another — have no HTTP request, so a command carrying `[Authorize]` or `[Roles]` would be denied. To run such a command as a trusted system actor, establish a server-side execution scope with `ISystemExecution`:

```csharp
public class NightlyReconciliation(ISystemExecution systemExecution, ICommandPipeline pipeline) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (systemExecution.AsSystem("Administrator"))
        {
            await pipeline.Execute(new ReconcileLedger());
        }
    }
}
```

`AsSystem(params string[] roles)` runs as an authenticated system actor carrying exactly the roles you name — the normal role check still applies, so a `[Roles("Administrator")]` command passes while a `[Roles("Auditor")]` command is still denied. With no roles the actor satisfies `[Authorize]` but no `[Roles]`. Use `As(ClaimsPrincipal principal)` when you need to run as a specific principal. The scope is ambient and restores the previous context when disposed, so it flows into every command executed inside the `using` block, including nested calls.

> The server-side principal is consulted **only when there is no HTTP request context**. On any HTTP request the request principal is always authoritative — a server-side scope can never influence authorization of an HTTP-origin command, and request-supplied data can never enter the scope.

### Executing Commands from Reactors

When a reactor returns a command as a side effect, mark the reactor with `[ExecuteCommandsAsSystem]` to run those commands under the declared roles automatically:

```csharp
[Reactor]
[ExecuteCommandsAsSystem("Administrator")]
public class ConsultantProvisioner : IReactor
{
    public InviteConsultant ConsultantRequested(ConsultantRequested @event, EventContext context) =>
        new(@event.Email);
}
```

A reactor that instead injects `ICommandPipeline` and calls `Execute` directly establishes the scope itself:

```csharp
public class ConsultantProvisioner(ISystemExecution systemExecution, ICommandPipeline pipeline) : IReactor
{
    [OnceOnly]
    public async Task ConsultantRequested(ConsultantRequested @event, EventContext context)
    {
        using (systemExecution.AsSystem("Administrator"))
        {
            await pipeline.Execute(new InviteConsultant(@event.Email));
        }
    }
}
```

## Best Practices

1. **Apply authorization at the command level** - Each command should declare its own authorization requirements
2. **Use the `[Roles]` attribute** - More convenient than the standard `[Authorize(Roles = "...")]` syntax
3. **Be explicit about public access** - Use `[AllowAnonymous]` to clearly indicate intentionally public commands
4. **Consider the principle of least privilege** - Only grant the minimum access required
5. **Test authorization** - Ensure unauthorized users cannot execute protected commands
6. **Use policies for complex logic** - Implement custom authorization policies for domain-specific rules
7. **Log authorization failures** - Monitor and log unauthorized access attempts

> **Note**: Authorization is evaluated as part of the command filter pipeline before the command handler is called. If authorization fails, the command will not be executed and the `CommandResult.IsAuthorized` will be `false`.
