// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A scenario proving a command is rejected often issues it with a value the command's type does not really allow: a
/// number cast to an enumeration no member is declared with (<see langword="default"/> for an enumeration declaring no
/// zero member is one), or <see langword="null"/> for a record or list the command requires. The document checks every value against the type of its property and has no form for either, so the
/// scenario is left out and reported, because writing the command or the state it starts from without that value
/// would state a different one whose rejection has lost its cause - while <see langword="null"/> is still stated
/// where the property may be absent.
/// </summary>
public class from_source_stating_values_the_document_cannot_hold : Specification
{
    const string Slice = """
        using System.Collections.Generic;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Contracts.Signing;

        public enum ContractSide
        {
            Customer,
            Consultant
        }

        public enum SigningTier
        {
            Basic = 1,
            Premium = 2
        }

        public record SigningPreferences(bool Remind);

        [EventType]
        public record SigningTierChanged(SigningTier Tier);

        [EventType]
        public record SigningTagged(IReadOnlyList<string> Tags);

        [Command]
        public record ChangeSigningTier(SigningTier Tier)
        {
            public SigningTierChanged Handle() => new(Tier);
        }

        [Command]
        public record TagSigning(IReadOnlyList<string> Tags)
        {
            public SigningTagged Handle() => new(Tags);
        }

        [EventType]
        public record SigningReissued(ContractSide Side, string Note);

        [EventType]
        public record SigningPreferencesSet(SigningPreferences Preferences, SigningPreferences? Fallback);

        [Command]
        public record ReissueSigning(ContractSide Side, string Note)
        {
            public SigningReissued Handle() => new(Side, Note);
        }

        [Command]
        public record SetSigningPreferences(SigningPreferences Preferences, SigningPreferences? Fallback)
        {
            public SigningPreferencesSet Handle() => new(Preferences, Fallback);
        }
        """;

    const string SideScenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Contracts.Signing;
        using Xunit;

        namespace Library.Contracts.Signing.when_reissuing;

        public class and_the_side_is_not_a_contract_side
        {
            readonly CommandScenario<ReissueSigning> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new ReissueSigning((ContractSide)99, "again"));

            [Fact] void should_not_succeed() => _result.ShouldHaveValidationErrors();
        }
        """;

    const string TierScenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Contracts.Signing;
        using Xunit;

        namespace Library.Contracts.Signing.when_changing_tier;

        public class and_the_tier_is_the_default
        {
            readonly CommandScenario<ChangeSigningTier> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new ChangeSigningTier(default));

            [Fact] void should_not_succeed() => _result.ShouldHaveValidationErrors();
        }
        """;

    const string TagsScenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Contracts.Signing;
        using Xunit;

        namespace Library.Contracts.Signing.when_tagging;

        public class and_the_tags_are_null
        {
            readonly CommandScenario<TagSigning> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new TagSigning(null!));

            [Fact] void should_not_succeed() => _result.ShouldHaveValidationErrors();
        }
        """;

    const string PreferencesScenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Contracts.Signing;
        using Xunit;

        namespace Library.Contracts.Signing.when_setting_preferences;

        public class and_the_preferences_are_null
        {
            readonly CommandScenario<SetSigningPreferences> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new SetSigningPreferences(null!, null));

            [Fact] void should_not_succeed() => _result.ShouldHaveValidationErrors();
        }
        """;

    const string SeededScenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Contracts.Signing;
        using Xunit;

        namespace Library.Contracts.Signing.when_reissuing;

        public class and_it_was_reissued_to_a_side_that_is_not_a_contract_side
        {
            readonly CommandScenario<ReissueSigning> _scenario = new();
            Result _result = null!;

            void Establish() => _scenario.Given.ForEventSource("signing").Events(new SigningReissued((ContractSide)99, "before"));

            async Task Because() => _result = await _scenario.Execute(new ReissueSigning(ContractSide.Customer, "again"));

            [Fact] void should_not_succeed() => _result.ShouldHaveValidationErrors();
        }
        """;

    const string OptionalScenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Contracts.Signing;
        using Xunit;

        namespace Library.Contracts.Signing.when_setting_preferences;

        public class and_there_is_no_fallback
        {
            readonly CommandScenario<SetSigningPreferences> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new SetSigningPreferences(new SigningPreferences(true), null));

            [Fact] void should_not_succeed() => _result.ShouldHaveValidationErrors();
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Contracts/Signing/Signing.cs", Slice),
        ("Library/Contracts/Signing/when_reissuing/and_the_side_is_not_a_contract_side.cs", SideScenario),
        ("Library/Contracts/Signing/when_changing_tier/and_the_tier_is_the_default.cs", TierScenario),
        ("Library/Contracts/Signing/when_tagging/and_the_tags_are_null.cs", TagsScenario),
        ("Library/Contracts/Signing/when_setting_preferences/and_the_preferences_are_null.cs", PreferencesScenario),
        ("Library/Contracts/Signing/when_reissuing/and_it_was_reissued_to_a_side_that_is_not_a_contract_side.cs", SeededScenario),
        ("Library/Contracts/Signing/when_setting_preferences/and_there_is_no_fallback.cs", OptionalScenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;

    void Because()
    {
        _result = new ScreenplayGenerator().Generate(Analyzed.Compile(_sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
    }

    IEnumerable<string> Omissions() =>
        _result.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Select(_ => _.Message);

    IEnumerable<string> Lines() =>
        _result.Source.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(_ => _.Trim());

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_state_only_the_scenario_whose_values_the_document_can_hold() => Lines().Count(_ => _.StartsWith("specification ", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_state_null_for_a_record_that_may_be_absent() => Lines().ShouldContain("fallback = null");
    [Fact] void should_never_state_a_number_no_member_is_declared_with() => Lines().ShouldNotContain("side = 99");
    [Fact] void should_never_state_the_command_without_the_value_it_was_rejected_for() => Lines().ShouldNotContain(@"note = ""again""");
    [Fact] void should_never_state_null_for_a_record_the_command_requires() => Lines().ShouldNotContain("preferences = null");
    [Fact] void should_leave_out_each_scenario_it_cannot_state() => Omissions().Count().ShouldEqual(5);
    [Fact] void should_warn_about_each_of_them() => _result.Diagnostics
        .Where(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification)
        .All(_ => _.Severity == ScreenplayDiagnosticSeverity.Warning)
        .ShouldBeTrue();
    [Fact] void should_say_which_number_no_member_is_declared_with_in_the_command() => Omissions().ShouldContain(
        "The scenario 'when_reissuing_and_the_side_is_not_a_contract_side' was left out because it states 'ReissueSigning.Side' as 99, which no member of the enumeration 'ContractSide' is declared with");
    [Fact] void should_say_which_number_no_member_is_declared_with_in_the_event_it_starts_from() => Omissions().ShouldContain(
        "The scenario 'when_reissuing_and_it_was_reissued_to_a_side_that_is_not_a_contract_side' was left out because it states 'SigningReissued.Side' as 99, which no member of the enumeration 'ContractSide' is declared with");
    [Fact] void should_say_default_names_no_member_of_an_enumeration_without_a_zero_member() => Omissions().ShouldContain(
        "The scenario 'when_changing_tier_and_the_tier_is_the_default' was left out because it states 'ChangeSigningTier.Tier' as 0, which no member of the enumeration 'SigningTier' is declared with");
    [Fact] void should_say_which_required_list_was_given_null() => Omissions().ShouldContain(
        "The scenario 'when_tagging_and_the_tags_are_null' was left out because it states 'TagSigning.Tags' as null, which a required list of 'String' cannot hold");
    [Fact] void should_say_which_required_property_was_given_null() => Omissions().ShouldContain(
        "The scenario 'when_setting_preferences_and_the_preferences_are_null' was left out because it states 'SetSigningPreferences.Preferences' as null, which a required property of type 'SigningPreferences' cannot hold");
    [Fact] void should_not_report_a_value_left_out_of_a_scenario_that_was_left_out() => _result.Diagnostics
        .Any(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecificationValue && !_.Message.Contains("and_there_is_no_fallback", StringComparison.Ordinal))
        .ShouldBeFalse();
    [Fact] void should_not_claim_to_write_the_number() => _result.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.UnnamedEnumerationValue).ShouldBeFalse();
}
