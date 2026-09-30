// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A scenario proving a command is rejected often issues it with a value the command's type does not really allow: a
/// number cast to an enumeration no member is declared with, or <see langword="null"/> for a record the command
/// requires. The document checks every value against the type of its property and has no form for either, so the
/// scenario states everything but that value - and still states <see langword="null"/> where the property may be
/// absent.
/// </summary>
public class from_source_stating_values_the_document_cannot_hold : Specification
{
    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Contracts.Signing;

        public enum ContractSide
        {
            Customer,
            Consultant
        }

        public record SigningPreferences(bool Remind);

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
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Contracts.Signing;
        using Xunit;

        namespace Library.Contracts.Signing.when_reissuing;

        public class and_the_side_is_not_a_contract_side
        {
            readonly CommandScenario<ReissueSigning> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new ReissueSigning((ContractSide)99, "again"));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }
        """;

    const string PreferencesScenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Contracts.Signing;
        using Xunit;

        namespace Library.Contracts.Signing.when_setting_preferences;

        public class and_the_preferences_are_null
        {
            readonly CommandScenario<SetSigningPreferences> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new SetSigningPreferences(null!, null));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Contracts/Signing/Signing.cs", Slice),
        ("Library/Contracts/Signing/when_reissuing/and_the_side_is_not_a_contract_side.cs", SideScenario),
        ("Library/Contracts/Signing/when_setting_preferences/and_the_preferences_are_null.cs", PreferencesScenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;

    void Because()
    {
        _result = new ScreenplayGenerator().Generate(Analyzed.Compile(_sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
    }

    IEnumerable<string> Reasons() =>
        _result.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecificationValue).Select(_ => _.Message);

    IEnumerable<string> Lines() =>
        _result.Source.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(_ => _.Trim());

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_state_both_scenarios() => Lines().Count(_ => _.StartsWith("specification ", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_never_state_a_number_no_member_is_declared_with() => Lines().ShouldNotContain("side = 99");
    [Fact] void should_still_state_the_other_values_of_the_command() => Lines().ShouldContain(@"note = ""again""");
    [Fact] void should_never_state_null_for_a_record_the_command_requires() => Lines().ShouldNotContain("preferences = null");
    [Fact] void should_still_state_null_for_a_record_that_may_be_absent() => Lines().ShouldContain("fallback = null");
    [Fact] void should_report_each_value_it_left_out() => Reasons().Count().ShouldEqual(2);
    [Fact] void should_say_which_number_no_member_is_declared_with() => Reasons().ShouldContain(
        "The value 'when_reissuing_and_the_side_is_not_a_contract_side' states for 'ReissueSigning.Side' is 99, which no member of the enumeration 'ContractSide' is declared with, so the scenario states everything but that value");
    [Fact] void should_say_which_required_property_was_given_null() => Reasons().ShouldContain(
        "The value 'when_setting_preferences_and_the_preferences_are_null' states for 'SetSigningPreferences.Preferences' is null, which a required property of type 'SigningPreferences' cannot hold, so the scenario states everything but that value");
    [Fact] void should_not_claim_to_write_the_number() => _result.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.UnnamedEnumerationValue).ShouldBeFalse();
}
