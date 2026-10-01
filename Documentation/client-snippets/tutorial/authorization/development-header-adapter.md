```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddMicrosoftIdentityPlatformIdentityAuthentication();
    builder.Services.Configure<ArcOptions>(options => options.TrustForwardedIdentityHeaders = true);
}
```
