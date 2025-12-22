var builder = WebApplication.CreateBuilder(args);
builder.AddCratis();

builder.Services.AddControllers();
builder.Services.AddMvc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.AddConcepts());

var app = builder.Build();
app.UseRouting();
app.UseCratis();

app.MapControllers();
app.UseWebSockets();

app.UseSwagger();
app.UseSwaggerUI();

//#if (EnableFrontend)
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("/index.html");
//#endif

await app.RunAsync();
