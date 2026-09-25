// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationPrincipalIdentity;

public class when_comparing_standard_principals : Specification
{
    [Fact]
    void should_distinguish_different_actors_with_identical_root_claims()
    {
        var original = Principal(Actor("first"));
        var selected = Principal(Actor("second"));

        AuthorizationPrincipalIdentity.Same(original, selected).ShouldBeFalse();
    }

    [Fact]
    void should_distinguish_different_nested_actors()
    {
        var original = Principal(Actor("same", Actor("first")));
        var selected = Principal(Actor("same", Actor("second")));

        AuthorizationPrincipalIdentity.Same(original, selected).ShouldBeFalse();
    }

    [Fact]
    void should_distinguish_different_labels()
    {
        var original = Principal();
        var selected = Principal();
        original.Identities.Single().Label = "first";
        selected.Identities.Single().Label = "second";

        AuthorizationPrincipalIdentity.Same(original, selected).ShouldBeFalse();
    }

    [Fact]
    void should_distinguish_different_bootstrap_contexts()
    {
        var original = Principal();
        var selected = Principal();
        original.Identities.Single().BootstrapContext = "first";
        selected.Identities.Single().BootstrapContext = "second";

        AuthorizationPrincipalIdentity.Same(original, selected).ShouldBeFalse();
    }

    [Fact]
    void should_distinguish_opaque_bootstrap_context_references()
    {
        var original = Principal();
        var selected = Principal();
        original.Identities.Single().BootstrapContext = new object();
        selected.Identities.Single().BootstrapContext = new object();

        AuthorizationPrincipalIdentity.Same(original, selected).ShouldBeFalse();
    }

    [Fact]
    void should_detect_replaced_actor_and_bootstrap_context_after_capture()
    {
        var original = Principal(Actor("same"));
        var identity = original.Identities.Single();
        identity.BootstrapContext = new object();
        var snapshot = AuthorizationPrincipalIdentity.Capture(original);
        identity.Actor = Actor("same");

        AuthorizationPrincipalIdentity.Same(snapshot, original).ShouldBeFalse();

        var newSnapshot = AuthorizationPrincipalIdentity.Capture(original);
        identity.BootstrapContext = new object();

        AuthorizationPrincipalIdentity.Same(newSnapshot, original).ShouldBeFalse();
    }

    [Fact]
    void should_detect_replaced_equal_bootstrap_string_after_capture()
    {
        var original = Principal();
        original.Identities.Single().BootstrapContext = "bootstrap";
        var snapshot = AuthorizationPrincipalIdentity.Capture(original);
        original.Identities.Single().BootstrapContext = new string("bootstrap".ToCharArray());

        AuthorizationPrincipalIdentity.Same(snapshot, original).ShouldBeFalse();
    }

    [Fact]
    void should_allow_a_shared_opaque_bootstrap_context_on_a_standard_clone()
    {
        var original = Principal();
        var context = new object();
        original.Identities.Single().BootstrapContext = context;
        var selected = Principal();
        selected.Identities.Single().BootstrapContext = context;

        AuthorizationPrincipalIdentity.Same(original, selected).ShouldBeTrue();
    }

    [Fact]
    void should_detect_mutated_actor_and_label_after_capture()
    {
        var original = Principal(Actor("first"));
        var snapshot = AuthorizationPrincipalIdentity.Capture(original);
        original.Identities.Single().Actor!.AddClaim(new Claim("permission", "changed"));

        AuthorizationPrincipalIdentity.Same(snapshot, original).ShouldBeFalse();

        snapshot = AuthorizationPrincipalIdentity.Capture(original);
        original.Identities.Single().Label = "changed";

        AuthorizationPrincipalIdentity.Same(snapshot, original).ShouldBeFalse();
    }

    [Fact]
    void should_compare_equivalent_standard_clones_including_actor_and_immutable_bootstrap()
    {
        var original = Principal(Actor("same", Actor("nested")));
        original.Identities.Single().Label = "root";
        original.Identities.Single().BootstrapContext = "bootstrap";
        var selected = new ClaimsPrincipal(original.Identities.Select(identity => identity.Clone()));

        AuthorizationPrincipalIdentity.Same(original, selected).ShouldBeTrue();
    }

    [Fact]
    void should_not_compare_custom_actor_types_across_principals()
    {
        var original = Principal(new CustomIdentity([new Claim("actor", "same")], "test"));
        var selected = Principal(new CustomIdentity([new Claim("actor", "same")], "test"));

        AuthorizationPrincipalIdentity.Same(original, selected).ShouldBeFalse();
        AuthorizationPrincipalIdentity.Same(original, original).ShouldBeTrue();
    }

    [Fact]
    void should_not_compare_custom_nested_actor_types_across_principals()
    {
        var original = Principal(Actor("same", new CustomIdentity([new Claim("actor", "nested")], "test")));
        var selected = Principal(Actor("same", new CustomIdentity([new Claim("actor", "nested")], "test")));

        AuthorizationPrincipalIdentity.Same(original, selected).ShouldBeFalse();
    }

    static ClaimsIdentity Actor(string name, ClaimsIdentity? actor = null) => new([new Claim("actor", name)], "test") { Actor = actor };

    static ClaimsPrincipal Principal(ClaimsIdentity? actor = null) => new(new ClaimsIdentity([new Claim("subject", "same")], "test") { Actor = actor });

    sealed class CustomIdentity(IEnumerable<Claim> claims, string authenticationType) : ClaimsIdentity(claims, authenticationType);
}
