// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_screen_that_cannot_be_scanned : Specification
{
    const string Component = """
        import { DataPage } from '@cratis/components';
        import { AllAuthors } from './AllAuthors';
        export const AuthorList = () => (
            <DataPage title="Authors" query={AllAuthors}>{`unterminated
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

    [Fact] void should_keep_the_imported_query() => _result.Source.Contains("data Author[] via query AllAuthors", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_write_a_title() => _result.Source.Contains("title \"Authors\"", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_write_a_table() => _result.Source.Contains("table Author", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_once() => _unread.Count.ShouldEqual(1);
    [Fact] void should_report_the_scan_problem() => _unread[0].Contains("cannot be scanned past line 4", StringComparison.Ordinal).ShouldBeTrue();
}
