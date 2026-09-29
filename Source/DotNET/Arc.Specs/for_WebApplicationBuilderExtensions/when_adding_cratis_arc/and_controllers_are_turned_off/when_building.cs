// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ControllerQueryPerformerProvider = Cratis.Arc.Queries.ControllerBased.QueryPerformerProvider;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc.and_controllers_are_turned_off;

public class when_building : Specification
{
    WebApplication? _app;
    bool _hasApplicationParts;
    bool _hasApplicationModelProviders;
    bool _hasArcStartupFilter;
    object? _actions;
    object? _validators;
    ControllerQueryPerformerProvider _controllerQueries;

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc(configureBuilder: arc => arc.WithoutControllers());
        _hasApplicationParts = builder.Services.Any(_ => _.ServiceType == typeof(ApplicationPartManager));
        _hasApplicationModelProviders = builder.Services.Any(_ => _.ServiceType == typeof(IApplicationModelProvider));
        _hasArcStartupFilter = builder.Services.Any(_ => _.ServiceType == typeof(IStartupFilter) && _.ImplementationType == typeof(ArcStartupFilter));
        _app = builder.Build();
        _actions = _app.Services.GetService<IActionDescriptorCollectionProvider>();
        _validators = _app.Services.GetService<IDiscoverableValidators>();
        _controllerQueries = _app.Services.GetRequiredService<ControllerQueryPerformerProvider>();
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_not_discover_controllers() => _hasApplicationParts.ShouldBeFalse();
    [Fact] void should_not_add_mvc_application_model_providers() => _hasApplicationModelProviders.ShouldBeFalse();
    [Fact] void should_not_register_mvc() => _actions.ShouldBeNull();
    [Fact] void should_still_add_the_arc_middlewares() => _hasArcStartupFilter.ShouldBeTrue();
    [Fact] void should_still_register_validator_discovery() => _validators.ShouldNotBeNull();
    [Fact] void should_have_no_controller_based_queries() => _controllerQueries.Performers.ShouldBeEmpty();
}
