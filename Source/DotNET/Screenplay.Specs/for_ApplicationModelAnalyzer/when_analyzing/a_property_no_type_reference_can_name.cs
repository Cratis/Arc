// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A Screenplay type reference is a single identifier, so a constructed generic loses its arguments the moment it is
/// written as one - a map of names to values comes out as the bare word <c>KeyValuePair</c>, which says nothing and
/// which the document never declares. Nothing better can be written, so what was lost is said instead of passed off
/// as an answer.
/// </summary>
public class a_property_no_type_reference_can_name : Specification
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name, IDictionary<string, string> Extras);
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_report_what_the_name_loses() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.UnmappableTypeReference);
    [Fact] void should_name_the_type_it_could_not_express() => _analysis.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnmappableTypeReference).Message.ShouldContain("KeyValuePair");
    [Fact] void should_say_nothing_about_the_property_it_could_express() => _analysis.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.UnmappableTypeReference).ShouldEqual(1);
}
