```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddCratis(
    configureArcBuilder: arcBuilder => arcBuilder.WithMongoDB(
        configureMongoDB: mongoBuilder => mongoBuilder.WithCamelCaseNamingPolicy()),
    configureChronicleBuilder: chronicleBuilder => chronicleBuilder.WithCamelCaseNamingPolicy());

var app = builder.Build();
app.UseCratis();

await app.RunAsync();
```
