// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2CredentialSource.given;

public class a_hop_reader : Specification
{
    protected ArcOptions _options;
    protected Dictionary<string, string> _headers;
    protected List<string> _warnings;
    private protected IVerifiedCredentialSource _source;
    private protected CredentialPresentation _presentation;
    private protected CredentialVerification _result;
    ServiceProvider _services;

    void Establish()
    {
        _options = new();
        _options.ForwardedIdentity.ClientCertificate.Required = true;
        _headers = new() { [ForwardedIdentityEnvelope.Header] = "must-not-be-logged" };
        _warnings = [];
        var vector = VerificationFixture.DirectBrowser();
        _presentation = VerificationFixture.Presentation(vector);
        var request = Substitute.For<IHttpRequestContext, ICanReadForwardedIdentityRequest, ICanReadClientCertificate>();
        request.Headers.Returns(_headers);
        _services = new ServiceCollection().AddSingleton<IOptions<ArcOptions>>(Options.Create(_options)).BuildServiceProvider();
        request.RequestServices.Returns(_services);
        var accessor = Substitute.For<IHttpRequestContextAccessor>();
        accessor.Current.Returns(request);
        _source = new ForwardedIdentityV2CredentialSource(accessor, VerificationFixture.Settings(vector.Inputs), VerificationFixture.Verifier(vector), new("resource", "test"), new PersonDirectory(), new recording_logger(_warnings), new ForwardedIdentityHopLogging(new FixedTime(vector.Inputs.Now)));
    }

    void Destroy() => _services.Dispose();

    sealed class recording_logger(List<string> warnings) : ILogger<ForwardedIdentityV2CredentialSource>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning) warnings.Add(formatter(state, exception));
        }
    }
}
