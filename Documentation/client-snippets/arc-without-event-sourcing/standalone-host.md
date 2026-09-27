```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddCratisArc();
builder.UseCratisMongoDB();

var app = builder.Build();
app.UseCratisArc();
app.Run();
```
