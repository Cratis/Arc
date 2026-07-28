// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing.source_that_did_not_compile;

/// <summary>
/// A type is written in as many places as it has partial declarations, and the Cratis house style puts one of them in
/// the intermediate folder of the build. An error there is an error inside the artifact just as much as one in the
/// file a person wrote, so every place a type is written counts - reading only the first would call a declaration
/// clean on the strength of the half that happens to be listed first.
/// </summary>
public class and_an_error_sits_in_a_partial_the_build_wrote : Specification
{
    const string Written = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;

        namespace Library.Onboarding.Registry;

        [EventType]
        public record CompanyRegistered(string Name);

        public partial class RegistryNotifier : IReactor
        {
            public Task CompanyRegistered(CompanyRegistered @event, EventContext context) => Task.CompletedTask;
        }
        """;

    const string Emitted = """
        namespace Library.Onboarding.Registry;

        public partial class RegistryNotifier
        {
            public string Describe() => RegistryMessages.Describe;
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Onboarding/Registry/Registry.cs", Written),
        ("Library/obj/Debug/net10.0/Generated/Registry.Logging.g.cs", Emitted)
    ];

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(_sources);

    ScreenplayDiagnostic Reported => _analysis.Diagnostics.First(_ => _.Code == ScreenplayDiagnosticCodes.SourceDidNotCompile);

    [Fact] void should_be_analyzing_source_that_really_does_not_compile() => Analyzed.ErrorsIn(_sources).ShouldNotBeEmpty();
    [Fact] void should_report_it_as_a_warning() => Reported.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_not_count_the_reactor_the_error_is_written_inside() => Reported.Message.Contains("2 artifact(s) were recovered anyway, 1 of them", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_still_return_the_reactor() => _analysis.Slice().Reactors.Single().Name.ShouldEqual("RegistryNotifier");
}
