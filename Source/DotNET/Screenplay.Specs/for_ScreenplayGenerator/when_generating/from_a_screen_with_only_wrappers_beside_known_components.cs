// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_screen_with_only_wrappers_beside_known_components : Specification
{
    const string Component = """
        import { DataPage, Column } from '@cratis/components';
        import { AllAuthors } from './AllAuthors';
        export const AuthorList = () => (
            <><React.Fragment><Fragment><div>
                {/* An empty comment is not direct content. */}
                <DataPage title="Authors" query={AllAuthors}>
                    <Column field="name" />
                    <UnknownInput />
                </DataPage>
            </div></Fragment></React.Fragment></>
        );
        """;

    ScreenplayGenerationResult _result;

    void Because()
    {
        var files = DeclaredUserInterfaceFiles.Holding(("Library/Authors/Listing/AuthorList.tsx", Component));
        _result = new ScreenplayGenerator(new ApplicationModelAnalyzer(files), new ScreenplayEmitter())
            .Generate(Analyzed.Compile(("Library/Authors/Registration/Registration.cs", LibrarySource.AuthorRegistration), ("Library/Authors/Listing/Listing.cs", LibrarySource.AuthorListing)), new ScreenplayOptions());
    }

    [Fact] void should_read_the_title() => _result.Source.Contains("title \"Authors\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_report_wrappers_or_children_of_a_known_component() => _result.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.ScreenStructureNotInferred).ShouldBeEmpty();
}
