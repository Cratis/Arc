// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

[Collection(MutatesAppContextSwitchesCollection.Name)]
public class and_the_controllers_switch_is_on_but_controllers_are_turned_off : Specification
{
    WebApplication? _app;
    bool _hasApplicationParts;
    object? _actions;

    void Establish() => AppContext.SetSwitch(ArcFeatureSwitches.ControllersSupportName, true);

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc(configureBuilder: arc => arc.WithoutControllers());
        _hasApplicationParts = builder.Services.Any(_ => _.ServiceType == typeof(ApplicationPartManager));
        _app = builder.Build();
        _actions = _app.Services.GetService<IActionDescriptorCollectionProvider>();
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_not_discover_controllers() => _hasApplicationParts.ShouldBeFalse();
    [Fact] void should_not_register_mvc() => _actions.ShouldBeNull();
}
