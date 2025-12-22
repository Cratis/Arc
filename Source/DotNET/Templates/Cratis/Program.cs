var builder = WebApplication.CreateBuilder(args);
builder.AddCratis();

var app = builder.Build();
app.UseCratis();
//#if (EnableFrontend)

app.UseDefaultFiles();
app.UseStaticFiles();
//#endif

app.Run();
