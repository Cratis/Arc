// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHop;

public class when_starting_http_listener_with_required_admission : Specification
{
    Exception _failure;

    void Because()
    {
        var options = new ArcOptions();
        options.ForwardedIdentity.ClientCertificate.Required = true;
        using var services = new ServiceCollection().AddSingleton(Options.Create(options)).BuildServiceProvider();
        using var mapper = new HttpListenerEndpointMapper(NullLogger<HttpListenerEndpointMapper>.Instance);
        _failure = Catch.Exception(() => mapper.Start(services));
    }

    [Fact] void should_refuse_before_opening_the_listener() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_explain_the_supported_adapter() => _failure.Message.ShouldContain("ASP.NET Core/Kestrel");
}
