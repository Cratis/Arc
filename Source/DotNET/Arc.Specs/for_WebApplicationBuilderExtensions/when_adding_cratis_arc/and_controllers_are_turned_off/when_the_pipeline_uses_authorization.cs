// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc.and_controllers_are_turned_off;

[Collection("UsesCurrentDirectory")]
public class when_the_pipeline_uses_authorization : Specification
{
    WebApplication? _app;
    Exception? _exception;
    IAuthorizationPolicyProvider? _policyProvider;
    IAuthorizationService? _authorizationService;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddCratisArc(configureBuilder: arc => arc.WithoutControllers());
        _app = builder.Build();
        _exception = await Catch.Exception(async () =>
        {
            _app.UseAuthorization();
            _app.UseCratisArc();
            await _app.StartAsync();
        });
        _policyProvider = _app.Services.GetService<IAuthorizationPolicyProvider>();
        _authorizationService = _app.Services.GetService<IAuthorizationService>();
    }

    async Task Destroy()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact] void should_build_and_start() => _exception.ShouldBeNull();
    [Fact] void should_register_the_policy_provider() => _policyProvider.ShouldNotBeNull();
    [Fact] void should_register_the_authorization_service() => _authorizationService.ShouldNotBeNull();
}
