// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_screen_holding_urls_and_templates : Specification
{
    const string Component = """
        import { DataPage, Column } from '@cratis/components';
        import { AllAuthors } from './AllAuthors';
        export const AuthorList = () => (
            <DataPage title="Docs: https://example.com/a" query={AllAuthors}>
                <Column field="name" header="Name // label" />
                <Column field="id" body={a => `${a}//${`nested${a.id}`}`} />
                See http://x // y. Don't stop.
            </DataPage>
        );
        """;

    ScreenplayGenerationResult _result;

    void Because()
    {
        var files = DeclaredUserInterfaceFiles.Holding(("Library/Authors/Listing/AuthorList.tsx", Component));
        _result = new ScreenplayGenerator(new ApplicationModelAnalyzer(files), new ScreenplayEmitter())
            .Generate(Analyzed.Compile(("Library/Authors/Registration/Registration.cs", LibrarySource.AuthorRegistration), ("Library/Authors/Listing/Listing.cs", LibrarySource.AuthorListing)), new ScreenplayOptions());
    }

    [Fact] void should_preserve_the_title() => _result.Source.Contains("title \"Docs: https://example.com/a\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_the_column_label() => _result.Source.Contains("column name label \"Name // label\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_only_the_computed_column() => _result.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.ScreenStructureNotInferred).ShouldEqual(1);
}
