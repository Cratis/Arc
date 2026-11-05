// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Cratis.Arc.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.ForwardedIdentityHop;

public class when_requiring_a_hop_for_v1 : for_ForwardedIdentityAuthentication.given.a_signed_request
{
    AuthenticationResult _refused;
    AuthenticationResult _accepted;
    AuthenticationResult _withoutCapability;

    async Task Because()
    {
        _options.ForwardedIdentity.ClientCertificate.Required = true;
        Resign(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var request = Substitute.For<IHttpRequestContext, ICanReadForwardedIdentityRequest, ICanReadAuthenticatedForwardingHop>();
        request.Method.Returns("POST");
        request.Path.Returns("/base/a%2Fb");
        request.Headers.Returns(_headers);
        request.Items.Returns(new Dictionary<object, object?>());
        var raw = (ICanReadForwardedIdentityRequest)request;
        raw.RawTarget.Returns("/base/a%2fb?x=1");
        raw.HeaderNames.Returns(_rawHeaders.Keys);
        raw.GetHeaderValues(Arg.Any<string>()).Returns(call => _rawHeaders.TryGetValue(call.Arg<string>(), out var values) ? values : []);
        var handler = new MicrosoftIdentityPlatformAuthenticationHandler(Options.Create(_options), NullLoggerFactory.Instance);
        _withoutCapability = await handler.HandleAuthentication(_context);
        _refused = await handler.HandleAuthentication(request);
        ((ICanReadAuthenticatedForwardingHop)request).IsAuthenticatedForwardingHop.Returns(true);
        _accepted = await handler.HandleAuthentication(request);
    }

    [Fact] void should_refuse_an_adapter_without_transport_evidence() => _withoutCapability.Failure.Reason.ToString().ShouldEqual("hop");
    [Fact] void should_refuse_before_verification() => _refused.Failure.Reason.ToString().ShouldEqual("hop");
    [Fact] void should_accept_the_same_envelope_after_admission() => _accepted.Failure.ShouldBeNull();
}
