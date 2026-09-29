// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

[Collection(MutatesAppContextSwitchesCollection.Name)]
public class and_the_controllers_switch_is_off : Specification
{
    WebApplication? _app;
    bool _hasApplicationParts;
    bool _hasApplicationModelProviders;
    bool _hasArcStartupFilter;
    object? _actions;

    void Establish()
    {
        // Read the cached switch value while the switch is still on, so turning it off here cannot freeze it off for
        // the specs that run after this one in the same process.
        _ = ArcFeatureSwitches.ControllersAreSupported;
        AppContext.SetSwitch(ArcFeatureSwitches.ControllersSupportName, false);
    }

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        _hasApplicationParts = builder.Services.Any(_ => _.ServiceType == typeof(ApplicationPartManager));
        _hasApplicationModelProviders = builder.Services.Any(_ => _.ServiceType == typeof(IApplicationModelProvider));
        _hasArcStartupFilter = builder.Services.Any(_ => _.ServiceType == typeof(IStartupFilter) && _.ImplementationType == typeof(ArcStartupFilter));
        _app = builder.Build();
        _actions = _app.Services.GetService<IActionDescriptorCollectionProvider>();
    }

    void Destroy()
    {
        AppContext.SetSwitch(ArcFeatureSwitches.ControllersSupportName, true);
        _app?.DisposeAsync().GetAwaiter().GetResult();
    }

    [Fact] void should_not_discover_controllers() => _hasApplicationParts.ShouldBeFalse();
    [Fact] void should_not_add_mvc_application_model_providers() => _hasApplicationModelProviders.ShouldBeFalse();
    [Fact] void should_not_register_mvc() => _actions.ShouldBeNull();
    [Fact] void should_still_add_the_arc_middlewares() => _hasArcStartupFilter.ShouldBeTrue();
}
