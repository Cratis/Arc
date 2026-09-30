// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

public class and_controllers_are_left_on : Specification
{
    WebApplication? _app;
    bool _hasApplicationParts;
    bool _hasCommandValidationRouteConvention;
    object? _actions;

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        _hasApplicationParts = builder.Services.Any(_ => _.ServiceType == typeof(ApplicationPartManager));
        _hasCommandValidationRouteConvention = builder.Services.Any(_ =>
            _.ServiceType == typeof(IApplicationModelProvider) && _.ImplementationType == typeof(CommandValidationRouteConvention));
        _app = builder.Build();
        _actions = _app.Services.GetService<IActionDescriptorCollectionProvider>();
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_discover_controllers() => _hasApplicationParts.ShouldBeTrue();
    [Fact] void should_add_the_command_validation_route_convention() => _hasCommandValidationRouteConvention.ShouldBeTrue();
    [Fact] void should_register_mvc() => _actions.ShouldNotBeNull();
}
