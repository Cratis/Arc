// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting.for_HostBuilderExtensions.when_adding_cratis_arc;

[Collection(MutatesAppContextSwitchesCollection.Name)]
public class and_the_controllers_switch_is_off : Specification
{
    IHost? _host;
    object? _applicationParts;
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
        _host = new HostBuilder()
            .ConfigureDefaults([])
            .AddCratisArc()
            .Build();
        _applicationParts = _host.Services.GetService<ApplicationPartManager>();
        _actions = _host.Services.GetService<IActionDescriptorCollectionProvider>();
    }

    void Destroy()
    {
        AppContext.SetSwitch(ArcFeatureSwitches.ControllersSupportName, true);
        _host?.Dispose();
    }

    [Fact] void should_not_discover_controllers() => _applicationParts.ShouldBeNull();
    [Fact] void should_not_register_mvc() => _actions.ShouldBeNull();
}
