// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Culture.for_CultureHostBuilderExtensions;

public class when_using_invariant_culture : Specification
{
    IHostBuilder _hostBuilder;
    CultureInfo? _previousCulture;
    CultureInfo? _previousUICulture;

    void Establish()
    {
        _previousCulture = CultureInfo.DefaultThreadCurrentCulture;
        _previousUICulture = CultureInfo.DefaultThreadCurrentUICulture;
        _hostBuilder = Host.CreateDefaultBuilder();
    }

    void Because() => _hostBuilder.UseInvariantCulture();

    [Fact] void should_set_default_thread_current_culture_to_invariant_culture() =>
        CultureInfo.DefaultThreadCurrentCulture.ShouldEqual(CultureInfo.InvariantCulture);

    [Fact] void should_set_default_thread_current_ui_culture_to_invariant_culture() =>
        CultureInfo.DefaultThreadCurrentUICulture.ShouldEqual(CultureInfo.InvariantCulture);

    [Fact] void should_return_host_builder_for_continuation() =>
        _hostBuilder.ShouldNotBeNull();

    void Destroy()
    {
        CultureInfo.DefaultThreadCurrentCulture = _previousCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _previousUICulture;
    }
}
