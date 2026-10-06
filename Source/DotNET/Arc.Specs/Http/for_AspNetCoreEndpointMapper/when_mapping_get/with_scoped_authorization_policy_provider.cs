// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_mapping_get;

public class with_scoped_authorization_policy_provider : Specification
{
    WebApplication _app;
    AspNetCoreEndpointMapper _mapper;
    AuthorizationPolicy? _policy;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<IAuthorizationPolicyProvider>(services => new DefaultAuthorizationPolicyProvider(services.GetRequiredService<IOptions<AuthorizationOptions>>()));
        _app = builder.Build();
        _mapper = new AspNetCoreEndpointMapper(_app);
    }

    void Because()
    {
        _mapper.MapGet("/catalog", _ => Task.CompletedTask, new EndpointMetadata("Catalog") { RequireAuthentication = true });
        _policy = ((IEndpointRouteBuilder)_app).DataSources.SelectMany(source => source.Endpoints).Single().Metadata.GetMetadata<AuthorizationPolicy>();
    }

    [Fact] void should_map_an_authenticated_endpoint() => _policy.ShouldNotBeNull();

    async Task Destroy() => await _app.DisposeAsync();
}
