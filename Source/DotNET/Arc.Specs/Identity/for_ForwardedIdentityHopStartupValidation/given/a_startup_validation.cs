// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHopStartupValidation.given;

public class a_startup_validation : Specification
{
    protected ArcOptions _options;
    private protected ForwardedIdentityHopStartupValidation _validation;
    ForwardedIdentityClientCertificateTrustStore _trust;
    string _bundle;

    protected List<string> Warnings { get; } = [];

    void Establish()
    {
        _options = new();
        var monitor = Substitute.For<IOptionsMonitor<ArcOptions>>();
        monitor.CurrentValue.Returns(_options);
        _trust = new(monitor, NullLogger<ForwardedIdentityClientCertificateTrustStore>.Instance);
        _validation = new(_trust, Options.Create(_options), new recording_logger(Warnings));
    }

    protected void RequireAdmission()
    {
        _options.ForwardedIdentity.ClientCertificate.Required = true;
        _options.ForwardedIdentity.ClientCertificate.AllowedDnsNames.Add("forwarder.test");
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=startup-warning-spec", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        using var root = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        _bundle = Path.GetTempFileName();
        File.WriteAllText(_bundle, root.ExportCertificatePem());
        _options.ForwardedIdentity.ClientCertificate.TrustedCertificateAuthoritiesPath = _bundle;
    }

    void Destroy()
    {
        _trust.Dispose();
        if (_bundle is not null) File.Delete(_bundle);
    }

    sealed class recording_logger(List<string> warnings) : ILogger<ForwardedIdentityHopStartupValidation>
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
