// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// The fall back a projection makes when nothing names a value has to be the same one a command makes - the number
/// as it stands, and a diagnostic saying so. A projection quietly writing a number while a command reports one would
/// leave the same mistake visible in one half of the document and invisible in the other.
/// </summary>
public class a_projection_setting_a_value_no_member_is_declared_with : Specification
{
    const string Source = """
        using System;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Access.Listing;

        [Flags]
        public enum Access
        {
            None = 0,
            Read = 1,
            Write = 2
        }

        [EventType]
        public record AccessRequested(string Subject);

        [ReadModel]
        [FromEvent<AccessRequested>]
        public record Request
        {
            [SetFrom<AccessRequested>("subject")]
            public string Subject { get; init; } = string.Empty;

            [SetValue<AccessRequested>(Access.Read | Access.Write)]
            public Access Granted { get; init; }
        }
        """;

    ApplicationModelAnalysis _analysis;
    ProjectionFromModel _from;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _from = _analysis.Slice().Projection!.Scope.From.Single();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_fall_back_to_the_number_behind_the_value() => _from.Properties["Granted"].ShouldEqual("$value(3)");
    [Fact] void should_report_the_value_it_could_not_name() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.UnnamedEnumerationValue]);
    [Fact] void should_say_which_property_it_was_given_to() => _analysis.Diagnostics.Single().Message.Contains("'Granted'", StringComparison.Ordinal).ShouldBeTrue();
}
