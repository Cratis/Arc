// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Culture.for_CultureWebApplicationBuilderExtensions;

public class when_using_invariant_culture : Specification
{
    WebApplicationBuilder _builder;
    CultureInfo? _previousCulture;
    CultureInfo? _previousUICulture;
    RequestLocalizationOptions? _options;

    void Establish()
    {
        _previousCulture = CultureInfo.DefaultThreadCurrentCulture;
        _previousUICulture = CultureInfo.DefaultThreadCurrentUICulture;
        _builder = WebApplication.CreateBuilder();
    }

    void Because()
    {
        _builder.UseInvariantCulture();
        var app = _builder.Build();
        _options = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
    }

    [Fact] void should_set_default_thread_current_culture_to_invariant_culture() =>
        CultureInfo.DefaultThreadCurrentCulture.ShouldEqual(CultureInfo.InvariantCulture);

    [Fact] void should_set_default_thread_current_ui_culture_to_invariant_culture() =>
        CultureInfo.DefaultThreadCurrentUICulture.ShouldEqual(CultureInfo.InvariantCulture);

    [Fact] void should_configure_request_localization_default_culture_to_invariant() =>
        _options!.DefaultRequestCulture.Culture.ShouldEqual(CultureInfo.InvariantCulture);

    [Fact] void should_configure_supported_cultures_to_contain_only_invariant() =>
        _options!.SupportedCultures.ShouldContainOnly(CultureInfo.InvariantCulture);

    [Fact] void should_configure_supported_ui_cultures_to_contain_only_invariant() =>
        _options!.SupportedUICultures.ShouldContainOnly(CultureInfo.InvariantCulture);

    [Fact] void should_clear_request_culture_providers() =>
        _options!.RequestCultureProviders.ShouldBeEmpty();

    [Fact] void should_return_web_application_builder_for_continuation() =>
        _builder.ShouldNotBeNull();

    void Destroy()
    {
        CultureInfo.DefaultThreadCurrentCulture = _previousCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _previousUICulture;
    }
}
