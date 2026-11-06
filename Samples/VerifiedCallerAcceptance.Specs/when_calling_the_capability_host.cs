// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace VerifiedCallerAcceptance.Specs;

public class when_calling_the_capability_host(given.a_host host) : IClassFixture<given.a_host>
{
    [Fact]
    public void should_match_the_shared_vector_bytes_and_verify_the_signature()
    {
        var (inputMatches, signatureVerifies) = host.Signer.CheckSharedVector();
        inputMatches.ShouldBeTrue();
        signatureVerifies.ShouldBeTrue();
    }

    [Fact]
    public async Task should_dispatch_a_signed_editor_as_the_verified_person()
    {
        using var request = host.Signer.Request(host.CapabilityPort, "POST", "/articles/publish");
        using var response = await host.Client.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldEqual(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("\"isSuccess\":true");
        body.ShouldContain(VerifiedCallerAcceptanceSettings.Editor);
    }

    [Fact]
    public async Task should_read_using_a_separate_query_scope()
    {
        using var request = host.Signer.Request(host.CapabilityPort, "GET", "/articles", new() { Scope = "read" });
        using var response = await host.Client.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldEqual(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain(VerifiedCallerAcceptanceSettings.Editor);
    }

    [Fact]
    public async Task should_refuse_a_signed_caller_without_a_client_certificate()
    {
        await AcceptControl();
        var before = host.Counts.Publishes;
        using var request = host.Signer.Request(host.CapabilityPort, "POST", "/articles/publish");
        using var response = await host.NoCertificateClient.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
        host.Counts.Publishes.ShouldEqual(before);
    }

    [Fact]
    public async Task should_reject_unsigned_forwarding() => await Refuse(host.Signer.Request(host.CapabilityPort, "POST", "/articles/publish", new() { OmitSignature = true }), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task should_reject_a_replayed_nonce()
    {
        var nonce = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        using (var first = host.Signer.Request(host.CapabilityPort, "POST", "/articles/publish", new() { Nonce = nonce }))
        using (var response = await host.Client.SendAsync(first, TestContext.Current.CancellationToken)) response.StatusCode.ShouldEqual(HttpStatusCode.OK);
        await Refuse(host.Signer.Request(host.CapabilityPort, "POST", "/articles/publish", new() { Nonce = nonce }), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task should_reject_an_unknown_key() => await Refuse(host.Signer.Request(host.CapabilityPort, "POST", "/articles/publish", new() { KeyId = "unknown" }), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task should_reject_a_different_audience() => await Refuse(host.Signer.Request(host.CapabilityPort, "POST", "/articles/publish", new() { Audience = "other-api" }), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task should_reject_a_person_missing_from_the_directory() => await Refuse(host.Signer.Request(host.CapabilityPort, "POST", "/articles/publish", new() { Subject = "00000000-0000-4000-8000-000000000001" }), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task should_not_treat_a_signed_tenant_as_membership() => await Refuse(host.Signer.Request(host.CapabilityPort, "POST", "/articles/publish", new() { Tenant = "tenant-b" }), HttpStatusCode.Forbidden);

    [Fact]
    public async Task should_refuse_when_product_roles_do_not_authorize()
    {
        await AcceptControl();
        var before = host.Counts.Publishes;
        using var request = host.Signer.Request(host.CapabilityPort, "POST", "/articles/publish", new() { Subject = VerifiedCallerAcceptanceSettings.Reader });
        using var response = await host.Client.SendAsync(request, TestContext.Current.CancellationToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("\"isAuthorized\":false");
        }
        else
        {
            response.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
        }
        host.Counts.Publishes.ShouldEqual(before);
    }

    [Fact]
    public async Task should_not_borrow_scopes_from_a_same_subject_product_scheme()
    {
        var request = host.Signer.Request(host.CapabilityPort, "POST", "/articles/publish", new() { Scope = "read" });
        request.Headers.Add("X-Product-User", VerifiedCallerAcceptanceSettings.Editor);
        await Refuse(request, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task should_treat_an_absent_scope_ceiling_as_empty() => await Refuse(host.Signer.Request(host.CapabilityPort, "POST", "/articles/publish", new() { Scope = string.Empty }), HttpStatusCode.Forbidden);

    [Fact]
    public async Task should_reject_a_different_subject_from_another_scheme()
    {
        var request = host.Signer.Request(host.CapabilityPort, "POST", "/articles/publish");
        request.Headers.Add("X-Product-User", VerifiedCallerAcceptanceSettings.Reader);
        await Refuse(request, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task should_reject_a_capability_route_on_the_default_listener() => await Refuse(host.Signer.Request(host.DefaultPort, "POST", "/articles/publish", useHttps: false), HttpStatusCode.NotFound);

    async Task AcceptControl()
    {
        var before = host.Counts.Publishes;
        using var request = host.Signer.Request(host.CapabilityPort, "POST", "/articles/publish");
        using var response = await host.Client.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldEqual(HttpStatusCode.OK);
        host.Counts.Publishes.ShouldEqual(before + 1);
    }

    async Task Refuse(HttpRequestMessage request, HttpStatusCode status)
    {
        using (request)
        {
            await AcceptControl();
            var before = host.Counts.Publishes;
            using var response = await host.Client.SendAsync(request, TestContext.Current.CancellationToken);
            response.StatusCode.ShouldEqual(status);
            host.Counts.Publishes.ShouldEqual(before);
        }
    }
}
