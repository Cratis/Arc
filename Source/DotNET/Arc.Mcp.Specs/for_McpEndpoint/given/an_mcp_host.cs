// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Capabilities;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// A real host serving MCP on the capabilities port, behind the capability gate, with a probe recording every dispatch.
/// </summary>
public class an_mcp_host : Specification
{
    protected const string ReadScope = "app:read";
    protected const string WriteScope = "app:write";
    protected const string PublicOrigin = "https://public.example.test";
    const int MaxStartAttempts = 5;

    protected int _capabilityPort;
    protected int _defaultPort;
    protected McpProbe _probe;
    protected WebApplication _app;
    protected HttpClient _client;
    private protected IVerifiedCallerAccessor _verified;

    protected virtual McpProfile Profile => McpProfile.PatOnly;
    protected virtual long MaxRequestBodySize => 1024 * 1024;
    protected virtual bool SelectGatedEndpoints => true;

    protected virtual void ConfigureMcp(McpBuilder mcp)
    {
    }

    protected virtual void ConfigureOptions(McpOptions options)
    {
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    protected virtual void ConfigureArcOptions(ArcOptions options)
    {
    }

    protected virtual void ConfigureEndpoints(WebApplication app)
    {
    }

    protected virtual void MapMcpEndpoint(WebApplication app) => app.MapArcMcp();

    protected virtual void ConfigureCallers(IServiceCollection services)
    {
        services.AddSingleton(new VerifiedCallerInstallation("https://resource.example", "test"));
        services.AddSingleton(new CallerResourceCatalog(new Dictionary<string, string> { ["https://resource.example"] = "audience" }));
        services.AddScoped<IVerifiedCredentialSource>(_ => Substitute.For<VerifiedCredentialSourceForSpecs>());
        _verified = Substitute.For<IVerifiedCallerAccessor>();
        _verified.GetCurrent().Returns(new CallerResolution(null, CallerRefusal.NoCredential));
        services.AddScoped(_ => _verified);
    }

    protected CancellationToken CancellationToken => global::Xunit.TestContext.Current.CancellationToken;

    async Task Establish()
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await StartHost();
                break;
            }
            catch (IOException ex) when (ex.InnerException is AddressInUseException && attempt < MaxStartAttempts)
            {
                // A reserved port was taken between release and bind; dispose the failed host and retry with fresh ports.
                await _app.DisposeAsync();
            }
        }

        _client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    async Task StartHost()
    {
        _capabilityPort = FreePort();
        do
        {
            _defaultPort = FreePort();
        }
        while (_defaultPort == _capabilityPort);

        _probe = new McpProbe();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{_capabilityPort}", $"http://127.0.0.1:{_defaultPort}");
        builder.AddCratisArc(
            options =>
            {
                options.EndpointGroups.Assign("capabilities", _capabilityPort);
                ConfigureArcOptions(options);
            },
            configureBuilder: arc => arc
                .WithoutControllers()
                .WithCapabilities(options => options.GatedEndpoints = SelectGatedEndpoints ? new CapabilityEndpointSelector() : null)
                .WithMcp(mcp =>
                {
                    mcp.Configure(Configure)
                        .Expose<WriteNote>("write_note", "Writes a note.")
                        .AddToolSource<WhoAmI>();
                    ConfigureMcp(mcp);
                }));
        ConfigureCallers(builder.Services);
        builder.Services.AddSingleton(_probe);
        RegisterCommands(builder.Services);
        ConfigureServices(builder.Services);
        _app = builder.Build();
        _app.UseCratisArc();
        MapMcpEndpoint(_app);
        ConfigureEndpoints(_app);
        await _app.StartAsync(CancellationToken);
    }

    async Task Destroy()
    {
        _client?.Dispose();
        await _app.DisposeAsync();
    }

    protected void Authenticate(CallerCredentialKind credential, params string[] scopes) => AuthenticateIn("tenant-a", credential, scopes);

    protected void AuthenticateIn(string tenant, CallerCredentialKind credential, params string[] scopes) =>
        _verified.GetCurrent().Returns(new CallerResolution(Caller(credential, tenant, scopes), null));

    protected static VerifiedCaller VerifiedCallerFor(CallerCredentialKind credential, string tenant, params string[] scopes) => Caller(credential, tenant, scopes);

    protected void Refuse(CallerRefusal refusal) => _verified.GetCurrent().Returns(new CallerResolution(null, refusal));

    protected void Unauthenticate() => _verified.GetCurrent().Returns(new CallerResolution(null, CallerRefusal.NoCredential));

    protected Task<McpResponse> Post(
        string body,
        string accept = "application/json, text/event-stream",
        string? host = null,
        Action<HttpRequestMessage>? customize = null,
        int? port = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:{port ?? _capabilityPort}/mcp")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Accept", accept);
        customize?.Invoke(request);
        if (host is not null)
        {
            request.Headers.Host = host;
            request.Headers.TryAddWithoutValidation("X-Forwarded-Host", host);
        }

        return Send(request);
    }

    protected Task<McpResponse> CallTool(string name, string arguments) =>
        Post($$$"""{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"{{{name}}}","arguments":{{{arguments}}}}}""");

    protected Task<McpResponse> ListTools(Action<HttpRequestMessage>? customize = null) =>
        Post("""{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}""", customize: customize);

    protected Task<McpResponse> Get(string path, string? host = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{_capabilityPort}{path}");
        if (host is not null)
        {
            request.Headers.Host = host;
            request.Headers.TryAddWithoutValidation("X-Forwarded-Host", host);
        }

        return Send(request);
    }

    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    static VerifiedCaller Caller(CallerCredentialKind credential, string tenant, string[] scopes) =>
        new(
            "1",
            CallerPrincipalKind.Person,
            credential,
            new("credential"),
            null,
            new("person"),
            null,
            null,
            credential == CallerCredentialKind.OAuthGrant ? new("grant") : null,
            null,
            null,
            "resource",
            new TenantId(tenant),
            null,
            null,
            "test",
            new HashSet<string>(scopes),
            new HashSet<string>(),
            null);

    static void RegisterCommands(IServiceCollection services)
    {
        var handlers = new[] { typeof(WriteNote), typeof(HiddenNote), typeof(WriteFormattedNote), typeof(WriteNestedNote) }
            .Select(type => (ICommandHandler)new ModelBoundCommandHandler(type, type.GetMethod("Handle")!)).ToArray();
        var commands = Substitute.For<ICommandHandlerProviders>();
        commands.Handlers.Returns(handlers);
        commands.TryGetHandlerFor(Arg.Any<object>(), out Arg.Any<ICommandHandler?>()).Returns(call =>
        {
            var found = handlers.SingleOrDefault(handler => handler.CommandType == call[0]!.GetType());
            call[1] = found;
            return found is not null;
        });
        services.AddSingleton(commands);
        services.AddSingleton(Substitute.For<ICommandKeys>());
    }

    void Configure(McpOptions options)
    {
        options.ApplicationName = "app";
        options.Profile = Profile;
        options.MaxRequestBodySize = MaxRequestBodySize;
        if (Profile != McpProfile.PatOnly)
        {
            options.Resource = $"{PublicOrigin}/mcp";
            options.PublicOrigin = PublicOrigin;
            options.AuthorizationServers.Add("https://login.example.test");
        }

        ConfigureOptions(options);
    }

    async Task<McpResponse> Send(HttpRequestMessage request)
    {
        using (request)
        {
            using var response = await _client.SendAsync(request, CancellationToken);
            var body = await response.Content.ReadAsStringAsync(CancellationToken);
            var challenge = response.Headers.WwwAuthenticate.ToString();
            return new McpResponse(response.StatusCode, challenge, response.Content.Headers.ContentType?.MediaType, body);
        }
    }
}
