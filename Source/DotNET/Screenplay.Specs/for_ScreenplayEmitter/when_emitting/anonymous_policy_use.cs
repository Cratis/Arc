// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting;

public class anonymous_policy_use : Specification
{
    [Theory]
    [InlineData("Authenticated", false)]
    [InlineData("authenticated", false)]
    [InlineData(" Authenticated ", false)]
    [InlineData("Authenticated", true)]
    [InlineData("authenticated", true)]
    [InlineData(" Authenticated ", true)]
    public void should_refuse_an_anonymous_declaration_that_captures_an_unrelated_implicit_authentication_gate(string name, bool query)
    {
        var declaration = new PolicyModel(name, AnonymousRequirement.Instance);
        var model = Model(query, new(true, [])) with { Policies = [declaration] };
        var emission = new ScreenplayEmitter().Emit(model, new());
        AnonymousPolicySources.AssertRefused(emission, AnonymousPolicySources.Ambiguous);
        Assert.Same(declaration, model.Policies.Single());
        declaration.Name.ShouldEqual(name);
        declaration.Requirement.ShouldBeOfExactType<AnonymousRequirement>();
    }

    [Theory]
    [InlineData("Authenticated")]
    [InlineData("authenticated")]
    [InlineData(" Authenticated ")]
    public void should_reserve_the_implicit_authentication_name_even_for_an_unused_anonymous_declaration(string name)
    {
        var declaration = new PolicyModel(name, AnonymousRequirement.Instance);
        var model = Model(false, new(false, [])) with { Slices = [], Policies = [declaration] };
        AnonymousPolicySources.AssertRefused(new ScreenplayEmitter().Emit(model, new()), AnonymousPolicySources.Ambiguous);
        Assert.Same(declaration, model.Policies.Single());
        declaration.Name.ShouldEqual(name);
        declaration.Requirement.ShouldBeOfExactType<AnonymousRequirement>();
    }

    [Theory]
    [InlineData("Authenticated", false)]
    [InlineData("authenticated", false)]
    [InlineData(" Authenticated ", false)]
    [InlineData("Authenticated", true)]
    [InlineData("authenticated", true)]
    [InlineData(" Authenticated ", true)]
    public void should_allow_non_anonymous_declarations_to_supply_the_implicit_authentication_requirement(string name, bool query)
    {
        var model = Model(query, new(true, [])) with { Policies = [new(name, AuthenticatedRequirement.Instance)] };
        var emission = new ScreenplayEmitter().Emit(model, new());
        emission.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
        emission.Source.ShouldContain("require authenticated");
        new ScreenplayVerifier().Verify(emission.Source).Compiles.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, true, "", "Anyone")]
    [InlineData(true, true, "", "Anyone")]
    [InlineData(false, false, "Staff", "Anyone")]
    [InlineData(true, false, "Staff", "Anyone")]
    [InlineData(false, false, "Anyone", "Anyone")]
    [InlineData(true, false, "Anyone", "Anyone")]
    [InlineData(false, false, "Anyone", "")]
    [InlineData(true, false, "Anyone", "")]
    [InlineData(false, false, "", "Anyone,Protected")]
    [InlineData(true, false, "", "Anyone,Protected")]
    public void should_refuse_unrepresentable_hand_constructed_command_and_query_gates(bool query, bool authenticated, string roles, string policies)
    {
        var authorization = new AuthorizationModel(authenticated, Split(roles)) { Policies = Split(policies) };
        var model = Model(query, authorization);
        var original = model.Policies.Single(policy => policy.Name == "Anyone");
        var emission = new ScreenplayEmitter().Emit(model, new());
        AnonymousPolicySources.AssertRefused(emission);
        Assert.Same(original, model.Policies.Single(policy => policy.Name == "Anyone"));
        original.Requirement.ShouldBeOfExactType<AnonymousRequirement>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_nested_anonymous_conditions_without_analyzer_evidence(bool isOr)
    {
        var model = Model(false, new(false, []) { Policies = ["Anyone"] }) with
        {
            Policies = [new("Anyone", new CombinedRequirement(AnonymousRequirement.Instance, isOr, AuthenticatedRequirement.Instance))]
        };
        var emission = new ScreenplayEmitter().Emit(model, new());
        AnonymousPolicySources.AssertRefused(emission);
    }

    [Theory]
    [InlineData("Anyone", false)]
    [InlineData("anyone", false)]
    [InlineData(" Anyone ", false)]
    [InlineData("Anyone", true)]
    [InlineData("anyone", true)]
    [InlineData(" Anyone ", true)]
    public void should_refuse_ambiguous_hand_constructed_declarations_in_either_order(string name, bool reverse)
    {
        var anonymous = new PolicyModel("Anyone", AnonymousRequirement.Instance);
        var protectedPolicy = new PolicyModel(name, AuthenticatedRequirement.Instance);
        var model = Model(false, new(false, []) { Policies = ["Anyone"] }) with
        {
            Policies = reverse ? [protectedPolicy, anonymous] : [anonymous, protectedPolicy]
        };
        AnonymousPolicySources.AssertRefused(new ScreenplayEmitter().Emit(model, new()), AnonymousPolicySources.Ambiguous);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_a_single_unconditional_gate_printable_for_direct_callers(bool query)
    {
        var model = Model(query, new(false, []) { Policies = ["Anyone"] });
        var emission = new ScreenplayEmitter().Emit(model, new());
        emission.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
        emission.Source.ShouldContain("require anonymous");
        new ScreenplayVerifier().Verify(emission.Source).Compiles.ShouldBeTrue();
        model.Policies.Single(policy => policy.Name == "Anyone").Requirement.ShouldBeOfExactType<AnonymousRequirement>();
    }

    [Fact]
    public void should_preserve_analyzer_collision_evidence_for_repeated_direct_emissions()
    {
        var sources = AnonymousPolicySources.All(extraRegistrations: "services.AddArcAuthorizationPolicy<ProtectedPolicy>(\"anyone\");");
        var analysis = Analyzed.Source(sources);
        var emitter = new ScreenplayEmitter();
        var first = emitter.Emit(analysis.Model, new());
        var second = emitter.Emit(analysis.Model, new());
        AnonymousPolicySources.AssertRefused(first, AnonymousPolicySources.Ambiguous);
        AnonymousPolicySources.AssertRefused(second, AnonymousPolicySources.Ambiguous);
        Assert.Equal(first.Diagnostics.ToArray(), second.Diagnostics.ToArray());
        analysis.Model.Policies.Single(policy => policy.Name == "Anyone").Requirement.ShouldBeOfExactType<AnonymousRequirement>();
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public void should_associate_recovery_evidence_idempotently_without_erasing_incomplete_evidence(bool firstIncomplete, bool secondIncomplete, bool exact)
    {
        var policy = new PolicyModel("Anyone", AnonymousRequirement.Instance);
        PolicyRecovery.Register(policy, firstIncomplete);
        PolicyRecovery.Register(policy, secondIncomplete);
        PolicyRecovery.IsExact(policy).ShouldEqual(exact);
    }

    [Fact]
    public void should_keep_refusal_evidence_separate_and_idempotent_without_rewriting_the_requirement()
    {
        var policy = new PolicyModel("Anyone", AnonymousRequirement.Instance);
        PolicyRecovery.Register(policy, false);
        var first = PolicyRecovery.Refuse(policy, "duplicate registration", "Library");
        var second = PolicyRecovery.Refuse(policy, "duplicate registration", "Library");
        Assert.Same(first, second);
        Assert.Same(first, PolicyRecovery.RefusalFor(policy));
        PolicyRecovery.IsExact(policy).ShouldBeFalse();
        policy.Requirement.ShouldBeOfExactType<AnonymousRequirement>();
    }

    [Fact]
    public void should_not_verify_a_refusal_returned_by_a_substituted_emitter()
    {
        var generator = new ScreenplayGenerator(new ApplicationModelAnalyzer(), new refusing_emitter());
        var result = generator.Generate(Analyzed.Compile(Analyzed.Root), new());
        AnonymousPolicySources.AssertRefused(result);
    }

    static string[] Split(string value) => value.Split(',', StringSplitOptions.RemoveEmptyEntries);

    static ApplicationModel Model(bool query, AuthorizationModel authorization)
    {
        const string Namespace = "Library.Catalog.Browsing";
        var readModel = new ReadModelModel("Interest", [new("InterestId", new("string", false, false))]) { Namespace = Namespace, FullName = $"{Namespace}.Interest" };
        var slice = SliceModel.Empty(Namespace, "Browsing", query ? SliceKind.StateView : SliceKind.StateChange) with
        {
            Commands = query ? [] : [new("RegisterInterest", null, [], authorization, [], [], null, null) { HasNoFactBehavior = true }],
            Queries = query ? [new("AllInterests", new("Interest", true, false), null, [], authorization) { ReturnTypeFullName = readModel.FullName }] : [],
            ReadModels = query ? [readModel] : []
        };
        return new("Library", "Library", [], [new("Anyone", AnonymousRequirement.Instance), new("Protected", AuthenticatedRequirement.Instance)], [slice], []);
    }

    sealed class refusing_emitter : IScreenplayEmitter
    {
        public ScreenplayEmission Emit(ApplicationModel model, ScreenplayOptions options) =>
            new(string.Empty, new([], [], [], [], SourceLocation.Start), [new(ScreenplayDiagnosticSeverity.Error, AnonymousPolicySources.Unrepresentable, "Anyone cannot represent the protected operation.", "Library.Catalog.Browsing")]);
    }
}
