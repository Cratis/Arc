// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture concatenates independently readable snippets.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandReadModelLookupAnalyzer;

public class when_analyzing_lookup_keys
{
    [Fact]
    public async Task should_warn_for_several_matching_properties() =>
        await Verify("[Command] public record Change(OwnerId Owner, MemberId From, MemberId To) { public Changed Handle(MemberState state) => new(); }",
            Warning().WithArguments("state", "Change", "From", "To", "not proven"));

    [Fact]
    public async Task should_advise_for_no_matching_property() =>
        await Verify("[Command] public record Change(OwnerId Owner) { public Changed Handle(MemberState state) => new(); }",
            Info().WithArguments("state", "Change", "MemberId", "not proven"));

    [Fact]
    public async Task should_advise_for_two_parameters_resolving_the_same_key() =>
        await Verify("[Command] public record Change(OwnerId Owner) { public Changed Handle(State first, State second) => new(); }",
            Info().WithArguments("second", "first", "same command key"));

    [Fact]
    public async Task should_not_report_a_matching_command_key_type() =>
        await Verify("[Command] public record Change(OwnerId Owner) { public Changed Handle(State state) => new(); }");

    [Fact]
    public async Task should_report_a_unique_distinguishing_property_as_an_off_redirect() =>
        await Verify("[Command] public record Change(OwnerId Owner, MemberId Member) { public Changed Handle(MemberState state) => new(); }", Info().WithArguments("Off and Measure", "Owner", "Member"));

    [Fact]
    public async Task should_not_treat_primitives_as_distinguishing() =>
        await Verify("[Command] public record Change(OwnerId Owner) { public Changed Handle(RawState state) => new(); }");

    [Fact]
    public async Task should_ignore_custom_providers_whose_key_type_is_not_proven() =>
        await Verify("[Command] public record Change : ICanProvideEventSourceId { public EventSourceId GetEventSourceId() => EventSourceId.New(); public Changed Handle(MemberState state) => new(); }");

    [Fact]
    public async Task should_ignore_explicitly_provided_models() =>
        await Verify("[Command] public record Change(OwnerId Owner) { public MemberState Provide() => new(new MemberId(Guid.NewGuid())); public Changed Handle(MemberState state) => new(); }");

    [Theory]
    [InlineData("private")]
    [InlineData("protected")]
    [InlineData("internal")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public async Task should_ignore_models_supplied_by_non_public_provide(string accessibility) =>
        await Verify($"[Command] public record Change(OwnerId Owner) {{ {accessibility} MemberState Provide() => new(new MemberId(Guid.NewGuid())); public Changed Handle(MemberState state) => new(); }}");

    [Theory]
    [InlineData("protected")]
    [InlineData("internal")]
    public async Task should_ignore_models_supplied_by_inherited_non_public_provide(string accessibility) =>
        await Verify($"public record Base {{ {accessibility} MemberState Provide() => new(new MemberId(Guid.NewGuid())); }} [Command] public record Change(OwnerId Owner) : Base {{ public Changed Handle(MemberState state) => new(); }}");

    [Fact]
    public async Task should_not_treat_an_inherited_private_provide_as_eligible() =>
        await Verify("public record Base { private MemberState Provide() => new(new MemberId(Guid.NewGuid())); } [Command] public record Change(OwnerId Owner) : Base { public Changed Handle(MemberState state) => new(); }", Info());

    [Fact]
    public async Task should_not_treat_a_static_provide_as_eligible() =>
        await Verify("[Command] public record Change(OwnerId Owner) { public static MemberState Provide() => new(new MemberId(Guid.NewGuid())); public Changed Handle(MemberState state) => new(); }", Info());

    [Fact]
    public async Task should_report_models_in_non_public_provide() =>
        await Verify("[Command] public record Change(OwnerId Owner) { private Plain Provide(MemberState state) => new(Owner); public Changed Handle(Plain plain) => new(); }", Info());

    [Fact]
    public async Task should_not_report_different_models_resolving_the_same_key() =>
        await Verify("[FromEvent<Changed>] public record OtherState([property: Key] OwnerId Id); [Command] public record Change(OwnerId Owner) { public Changed Handle(State first, OtherState second) => new(); }");

    [Fact]
    public async Task should_not_report_different_models_matching_one_distinguishing_property() =>
        await Verify("[FromEvent<Changed>] public record OtherMemberState([property: Key] MemberId Id); [Command, Cratis.Arc.Commands.ReadProtection(Cratis.Arc.Commands.ReadProtectionMode.On)] public record Change(OwnerId Owner, MemberId Member) { public Changed Handle(MemberState first, OtherMemberState second) => new(); }");

    [Fact]
    public async Task should_report_repeated_models_matching_one_distinguishing_property() =>
        await Verify("[Command, Cratis.Arc.Commands.ReadProtection(Cratis.Arc.Commands.ReadProtectionMode.On)] public record Change(OwnerId Owner, MemberId Member) { public Changed Handle(MemberState first, MemberState second) => new(); }", Info().WithArguments("second", "first", "Member"));

    [Fact]
    public async Task should_report_models_in_provide() =>
        await Verify("[Command] public record Change(OwnerId Owner) { public Plain Provide(MemberState state) => new(Owner); public Changed Handle(Plain plain) => new(); }", Info());

    [Fact]
    public async Task should_ignore_non_chronicle_models_and_non_commands() =>
        await Verify("[Command] public record Change(OwnerId Owner) { public Changed Handle(Plain state) => new(); } public record Other(OwnerId Owner) { public Changed Handle(MemberState state) => new(); }");

    [Fact]
    public async Task should_recognize_a_key_attribute_with_a_non_id_name() =>
        await Verify("[FromEvent<Changed>] public record SelectedState([property: Key] MemberId Member); [Command] public record Change(OwnerId Owner) { public Changed Handle(SelectedState state) => new(); }", Info());

    [Fact]
    public async Task should_recognize_inherited_model_keys() =>
        await Verify("[FromEvent<Changed>] public record DerivedState(MemberId Id) : MemberState(Id); [Command] public record Change(OwnerId Owner) { public Changed Handle(DerivedState state) => new(); }", Info());

    [Fact]
    public async Task should_keep_separate_distinguishing_keys_quiet() =>
        await Verify("[Command, Cratis.Arc.Commands.ReadProtection(Cratis.Arc.Commands.ReadProtectionMode.On)] public record Change(OwnerId Owner, MemberId Member) { public Changed Handle(State state, MemberState member) => new(); }");

    static DiagnosticBuilder Warning() => AnalyzerVerifier<CommandReadModelLookupAnalyzer>.Diagnostic("ARCCHR0023").WithSeverity(DiagnosticSeverity.Warning);
    static DiagnosticBuilder Info() => AnalyzerVerifier<CommandReadModelLookupAnalyzer>.Diagnostic("ARCCHR0023").WithSeverity(DiagnosticSeverity.Info);
    static Task Verify(string source, params ExpectedDiagnostic[] diagnostics) =>
        AnalyzerVerifier<CommandReadModelLookupAnalyzer>.VerifyAnalyzerAsync(DecisionAnalyzerSource.Definitions + source, diagnostics);
}
