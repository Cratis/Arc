// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model holding a value the document has no type for. A declaration would have to either leave the value
/// out, describing a shape the application does not have, or name a type nothing declares - so the read model is not
/// declared, and the projection building it names it exactly as it did before read models were declared at all.
/// </summary>
public class a_read_model_holding_a_value_the_document_cannot_type : a_read_model_document
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Inventory.Tallies;

        [EventType]
        public record TallyStarted(string Name);

        [ReadModel]
        [FromEvent<TallyStarted>]
        public record Tally(string Name, IDictionary<string, int> Counts);
        """;

    void Because() => GenerateSlice(Source);

    [Fact] void should_not_declare_the_read_model() => Result.Source.ShouldNotContain("readmodel Tally");
    [Fact] void should_still_build_it() => Result.Source.ShouldContain("=> Tally");
    [Fact] void should_say_why() => Result.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.UndeclarableReadModel).Select(_ => _.Message).Single().ShouldContain("'Counts'");
    [Fact] void should_not_report_the_type_it_never_wrote() => Result.Diagnostics.Select(_ => _.Code).ShouldNotContain(ScreenplayDiagnosticCodes.UnmappableTypeReference);
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
}
