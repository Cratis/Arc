// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_an_executable_model_cap;

public class a_command_route : a_generated_document
{
    const string Source = """
        [Command, EventSourceType("Account", concurrency: true), EventStreamType("Transactions", concurrency: true)]
        public record RegisterAuthor(AuthorId AuthorId, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    void Because() => Generate(new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V7 }, (Analyzed.SlicePath, IdentifierSources.With(Source)));

    [Fact] void should_withhold_v8_declarations() => Result.Source.ShouldNotContain("eventsource Account");
    [Fact] void should_preserve_legacy_concurrency() => Result.Source.ShouldContain("sourceType Account");
    [Fact] void should_preserve_the_legacy_route_diagnostic() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EventSourceNotRepresentable).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_not_report_a_binding_defect() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeFalse();
    [Fact] void should_preserve_only_the_known_legacy_binding_limit() => Bound.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContainOnly("PLAY0271");
}
