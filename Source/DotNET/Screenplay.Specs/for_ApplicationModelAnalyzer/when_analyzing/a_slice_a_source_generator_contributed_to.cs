// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A compilation loaded from a project carries what every source generator emitted to the intermediate folder of the
/// build. Those files declare real members of the slice and say nothing at all about where it is written, so counting
/// them reports a slice sitting in one folder as spread over two and looks for its screens in a build folder.
/// </summary>
public class a_slice_a_source_generator_contributed_to : Specification
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
            public string Describe() => nameof(RegistryNotifier);
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Onboarding/Registry/Registry.cs", Written),
        ("Library/obj/Debug/net10.0/Microsoft.Gen.Logging/Microsoft.Gen.Logging.LoggingGenerator/Registry.Logging.g.cs", Emitted)
    ];

    static readonly DeclaredUserInterfaceFiles _files = new(
        "Library/Onboarding/Registry/RegistryPage.tsx",
        "Library/obj/Debug/net10.0/Microsoft.Gen.Logging/Microsoft.Gen.Logging.LoggingGenerator/Emitted.tsx");

    ApplicationModelAnalysis _analysis;
    IEnumerable<ScreenModel> _screens;

    void Establish()
    {
        _analysis = Analyzed.Source(_files, _sources);
        _screens = _analysis.Slice().Screens;
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_not_report_the_slice_as_spread_over_folders() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.AmbiguousScreenFile).ShouldBeFalse();
    [Fact] void should_take_its_screens_only_from_the_folder_it_is_written_in() => _screens.Select(_ => _.Name).ShouldContainOnly(["RegistryPage"]);
    [Fact] void should_report_nothing_beyond_what_no_screen_states() => _analysis.Diagnostics.Select(_ => _.Code).Distinct().ShouldContainOnly([ScreenplayDiagnosticCodes.ScreenStructureNotInferred]);
}
