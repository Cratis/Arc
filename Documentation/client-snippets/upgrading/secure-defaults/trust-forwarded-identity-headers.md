```csharp
// Local development only. Deployed hosts must configure signed forwarding instead.
builder.AddCratis(options =>
{
    options.Profile = Cratis.Arc.DeploymentProfile.LocalDevelopment;
    options.TrustForwardedIdentityHeaders = true;
});
```
