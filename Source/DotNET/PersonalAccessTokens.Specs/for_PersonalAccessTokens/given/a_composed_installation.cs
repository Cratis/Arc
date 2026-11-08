// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Arc.Identity;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.PersonalAccessTokens.for_PersonalAccessTokens.given;

public class a_composed_installation : a_pat_module
{
    readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    readonly string _authorityBundle = Path.Combine(Path.GetTempPath(), $"arc-composed-{Guid.NewGuid():N}.pem");
    readonly IHttpRequestContextAccessor _requests = Substitute.For<IHttpRequestContextAccessor>();
    X509Certificate2 _certificate;
    protected ServiceProvider _services;
    protected PersistedPersons _persons;

    void Establish()
    {
        _certificate = CreateTransportCertificate();
        var services = new ServiceCollection();
        var arc = Substitute.For<IArcBuilder>();
        arc.Services.Returns(services);
        arc.WithPersonalAccessTokens(new("direct") { AllowedScopes = ["read"] }, callers => callers
            .ForResource("resource-a", "resource-a", "test")
            .UsePersonDirectory<PersistedPersons>()
            .UseSignedForwardedIdentityV2(v2 => v2.WithRealmIssuer(_account.Issuer)
                .AddPublicKey("test", _key.ExportSubjectPublicKeyInfoPem()).UseProcessLocalNonceStore())
            .AllowPersonalAccessTokenCallers());
        var options = new ArcOptions { TrustForwardedIdentityHeaders = true };
        options.ForwardedIdentity.ClientCertificate.Required = true;
        options.ForwardedIdentity.ClientCertificate.TrustedCertificateAuthoritiesPath = _authorityBundle;
        options.ForwardedIdentity.ClientCertificate.AllowedDnsNames.Add("proxy.test");
        var tenants = Substitute.For<ITenantIdAccessor>();
        tenants.Current.Returns(_tenant);
        _persons = new(_account);
        services.AddLogging();
        services.AddSingleton<IOptions<ArcOptions>>(Options.Create(options));
        services.AddSingleton(_requests);
        services.AddSingleton(tenants);
        services.AddSingleton<ICallerPersonDirectory>(_persons);
        services.AddSingleton(_authority);
        services.AddSingleton(_epochs);
        services.AddSingleton(_policy);
        services.AddSingleton(_clock);
        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    protected Dictionary<string, string[]> Browser(string audience = "resource-a", string tenant = "tenant-one", int age = 0)
    {
        var headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["x-ms-client-principal"] = [Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new { identityProvider = "https://upstream.example", userId = _account.Subject, userDetails = "Example Person", userRoles = new[] { "user" }, claims = Array.Empty<object>() }))],
            ["x-ms-client-principal-id"] = [_account.Subject],
            ["x-ms-client-principal-name"] = ["Example Person"],
            ["x-cratis-subject"] = [_account.Subject],
            ["x-cratis-issuer"] = [_account.Issuer],
            ["x-cratis-idp"] = ["https://upstream.example"],
            ["x-cratis-auth-time"] = [Now.AddMinutes(-5).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)],
            ["x-cratis-credential-kind"] = ["browser-session"],
            ["x-cratis-acr"] = ["basic"],
            ["x-cratis-sender-binding"] = ["none"],
            ["x-cratis-credential-id"] = ["session-one"],
            ["x-cratis-token-scope"] = ["read"],
            ["x-cratis-tenant-id"] = [tenant],
            ["x-cratis-policy-epoch"] = ["7"]
        };
        Sign(headers, age, audience);
        headers["Cookie"] = ["cratis-identity-session=synthetic-session"];
        return headers;
    }

    protected void Sign(Dictionary<string, string[]> headers, int age = 0, string audience = "resource-a")
    {
        headers.Remove(ForwardedIdentityEnvelope.Header);
        var timestamp = Now.AddSeconds(-age).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var nonce = Url(RandomNumberGenerator.GetBytes(16));
        var covered = headers.Where(header => header.Key.StartsWith("x-cratis-", StringComparison.Ordinal) || header.Key.StartsWith("x-ms-client-principal", StringComparison.Ordinal) || header.Key == "tenant-id").OrderBy(header => header.Key, StringComparer.Ordinal).ToArray();
        var input = new StringBuilder("Cratis.AuthProxy.ForwardedIdentity.v2\n");
        foreach (var value in new[] { audience, "test", timestamp, nonce, "POST", "/identity/pat", covered.Length.ToString(CultureInfo.InvariantCulture) }) Frame(input, value);
        foreach (var header in covered)
        {
            Frame(input, header.Key);
            Frame(input, header.Value.Single());
        }
        var signature = Url(_key.SignData(Encoding.UTF8.GetBytes(input.ToString()), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        headers[ForwardedIdentityEnvelope.Header] = [$"v2.test.{timestamp}.{nonce}.{signature}"];
    }

    protected async Task<T> InRequest<T>(Dictionary<string, string[]> headers, Func<IServiceProvider, Task<T>> action, bool authenticatedHop = true, string target = "/identity/pat")
    {
        await using var scope = _services.CreateAsyncScope();
        var request = (IHttpRequestContext)Substitute.For<ComposedRequest, IHttpRequestContext, ICanReadForwardedIdentityRequest>();
        ((ICanReadClientCertificate)request).TransportClientCertificate.Returns(_ => authenticatedHop ? new X509Certificate2(_certificate) : null);
        ((ICanPreserveCallerCredentialHeaders)request).PreservesDuplicateCredentialHeaders.Returns(true);
        request.RequestServices.Returns(scope.ServiceProvider);
        request.Items.Returns(new Dictionary<object, object?>());
        request.Method.Returns("POST");
        request.Headers.Returns(headers.ToDictionary(header => header.Key, header => header.Value[0]));
        var raw = (ICanReadForwardedIdentityRequest)request;
        raw.RawTarget.Returns(target);
        raw.HeaderNames.Returns(headers.Keys.ToArray());
        raw.GetHeaderValues(Arg.Any<string>()).Returns(call => headers.GetValueOrDefault(call.Arg<string>()) ?? []);
        _requests.Current.Returns(request);
        return await action(scope.ServiceProvider);
    }

    protected Task<PatIssueResult> IssueWith(Dictionary<string, string[]> headers, bool authenticatedHop = true, string target = "/identity/pat") => InRequest(
        headers,
        services => services.GetRequiredService<IPersonalAccessTokens>().Issue(TimeSpan.FromDays(1), ["read"], TestContext.Current.CancellationToken),
        authenticatedHop,
        target);

    protected Task<CallerResolution> ResolveWith(Dictionary<string, string[]> headers) => InRequest(headers,
        services => services.GetRequiredService<IVerifiedCallerAccessor>().GetCurrent());

    static void Frame(StringBuilder input, string value) => input.Append(Encoding.UTF8.GetByteCount(value).ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append('\n');
    static string Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    async Task Destroy()
    {
        await _services.DisposeAsync();
        _key.Dispose();
        _certificate.Dispose();
        File.Delete(_authorityBundle);
    }

    X509Certificate2 CreateTransportCertificate()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var authorityRequest = new CertificateRequest("CN=Arc composed spec authority", key, HashAlgorithmName.SHA256);
        authorityRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        authorityRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        using var authority = authorityRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(2));
        File.WriteAllText(_authorityBundle, authority.ExportCertificatePem());
        var request = new CertificateRequest("CN=proxy.test", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.2") }, true));
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("proxy.test");
        request.CertificateExtensions.Add(names.Build());
        return request.Create(authority, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), RandomNumberGenerator.GetBytes(16));
    }

    public class ComposedRequest : CredentialHeaderPreservationForSpecs, ICanReadClientCertificate
    {
        public X509Certificate2? TransportClientCertificate => Certificate();
        public virtual X509Certificate2? Certificate() => null;
    }

    public class PersistedPersons(CallerAccountKey account) : ICallerPersonDirectory
    {
        public int Calls { get; private set; }
        public Task<CallerPersonFound> Find(CallerAccountKey accountKey, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(accountKey == account ? CallerPersonFound.Found(new("person-one")) : CallerPersonFound.NotFound());
        }
    }
}
