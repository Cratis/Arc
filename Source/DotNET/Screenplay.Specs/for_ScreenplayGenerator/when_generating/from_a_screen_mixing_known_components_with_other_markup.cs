// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_screen_mixing_known_components_with_other_markup : Specification
{
    const string Component = """
        import { DataPage, Column } from '@cratis/components';
        import { AllAuthors } from './AllAuthors';
        export const AuthorList = () => (
            <div className="page">
                <h1>Library</h1>
                <Card title="Totals"><span>{n}</span></Card>
                <DataPage title="Authors" query={AllAuthors}><Column field="name" /></DataPage>
                <Button label="Refresh" onClick={refresh} />
                <Button label="Export" onClick={exportAll} />
            </div>
        );
        """;

    ScreenplayGenerationResult _result;
    List<string> _unread;

    void Because()
    {
        var files = DeclaredUserInterfaceFiles.Holding(("Library/Authors/Listing/AuthorList.tsx", Component));
        _result = new ScreenplayGenerator(new ApplicationModelAnalyzer(files), new ScreenplayEmitter())
            .Generate(Analyzed.Compile(("Library/Authors/Registration/Registration.cs", LibrarySource.AuthorRegistration), ("Library/Authors/Listing/Listing.cs", LibrarySource.AuthorListing)), new ScreenplayOptions());
        _unread = _result.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.ScreenStructureNotInferred).Select(_ => _.Message).ToList();
    }

    [Fact] void should_keep_the_title() => _result.Source.Contains("title \"Authors\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_table() => _result.Source.Contains("table Author", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_column() => _result.Source.Contains("column name", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_each_distinct_component_and_one_markup_list() => _unread.Count.ShouldEqual(3);
    [Fact] void should_report_the_card_first() => _unread[0].Contains("renders a <Card>", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_the_button_once() => _unread[1].Contains("renders a <Button>", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_list_the_html_content() => _unread[2].Contains("renders <h1>, <span>", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_exempt_the_wrapper() => _unread.Exists(_ => _.Contains("<div>", StringComparison.Ordinal)).ShouldBeFalse();
}
