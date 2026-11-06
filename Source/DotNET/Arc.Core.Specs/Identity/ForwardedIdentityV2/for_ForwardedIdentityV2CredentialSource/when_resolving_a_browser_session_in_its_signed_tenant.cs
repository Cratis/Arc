// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2CredentialSource;

public class when_resolving_a_browser_session_in_its_signed_tenant : given.a_browser_session_resolution
{
    CallerResolution _result;
    string _thumbprint;
    string _jti;

    async Task Because()
    {
        var vector = VerificationFixture.DirectBrowser();
        using var binding = JsonDocument.Parse(Convert.FromBase64String(vector.Inputs.Headers.Single(header => header.Name == "x-cratis-sender-binding").Value));
        _thumbprint = binding.RootElement.GetProperty("jkt").GetString()!;
        _jti = binding.RootElement.GetProperty("jti").GetString()!;
        _result = await Resolve(vector, new("tenant-test"), new());
    }

    [Fact] void should_accept() => _result.Refusal.ShouldBeNull();
    [Fact] void should_keep_the_contract() => _result.Caller!.ContractVersion.ShouldEqual("1");
    [Fact] void should_keep_the_principal_kind() => _result.Caller!.PrincipalKind.ShouldEqual(CallerPrincipalKind.Person);
    [Fact] void should_keep_the_credential_kind() => _result.Caller!.CredentialKind.ShouldEqual(CallerCredentialKind.BrowserSession);
    [Fact] void should_keep_the_credential_id() => _result.Caller!.CredentialId.Value.ShouldEqual("credential-test");
    [Fact] void should_keep_the_account() => _result.Caller!.AccountKey.ShouldEqual(new CallerAccountKey("https://identity.example.test", "8b167ad2-0f89-4c78-9d42-6d4580495d32"));
    [Fact] void should_resolve_the_person() => _result.Caller!.Person!.Value.ShouldEqual("persisted-person");
    [Fact] void should_have_no_service() => _result.Caller!.ServicePrincipal.ShouldBeNull();
    [Fact] void should_have_no_actor() => _result.Caller!.Actor.ShouldBeNull();
    [Fact] void should_have_no_grant() => _result.Caller!.GrantId.ShouldBeNull();
    [Fact] void should_have_no_organisation() => _result.Caller!.Organisation.ShouldBeNull();
    [Fact] void should_keep_the_idp() => _result.Caller!.Assurance!.Idp.ShouldEqual("https://login.example.test");
    [Fact] void should_keep_the_authentication_time() => _result.Caller!.Assurance!.AuthTime.ShouldEqual(1699999900L);
    [Fact] void should_keep_the_assurance_context() => _result.Caller!.Assurance!.Acr.ShouldEqual("basic");
    [Fact] void should_keep_the_authentication_methods() => _result.Caller!.Assurance!.Amr!.ShouldEqual<string>("pwd");
    [Fact] void should_keep_the_declared_client() => _result.Caller!.Client.ShouldEqual(new CallerClient("client-test", CallerClientAuthentication.Declared));
    [Fact] void should_keep_the_resource() => _result.Caller!.Resource.ShouldEqual("resource");
    [Fact] void should_keep_the_signed_tenant() => _result.Caller!.Tenant!.Value.ShouldEqual("tenant-test");
    [Fact] void should_keep_the_policy_epoch() => _result.Caller!.PolicyEpoch.ShouldEqual((ulong?)7);
    [Fact] void should_keep_the_environment() => _result.Caller!.Environment.ShouldEqual("test");
    [Fact] void should_keep_the_scope_ceiling() => _result.Caller!.Scopes.SetEquals(["example:read", "example:write"]).ShouldBeTrue();
    [Fact] void should_have_no_permissions() => _result.Caller!.Permissions.ShouldBeEmpty();
    [Fact] void should_keep_the_sender_binding() => _result.Caller!.SenderBinding.ShouldEqual(new CallerSenderBinding(_thumbprint, _jti));
}
