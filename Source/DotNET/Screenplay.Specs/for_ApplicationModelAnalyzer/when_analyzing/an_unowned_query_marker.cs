// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class an_unowned_query_marker : Specification
{
    const string Source = """
        using Cratis.Arc.Queries.ModelBound;
        namespace Library.Orders.Listing;
        [ReadModel]
        public record Order
        {
            public static Order ById(string id) => new();
        }
        public class Unowned
        {
            [Query] public static Order MissingOwner() => new();
        }
        """;

    Exception _error;

    void Because() => _error = Catch.Exception(() => Analyzed.Source(Source));

    [Fact] void should_compile_without_running_arc_analyzers() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_fail_extraction_instead_of_silently_omitting_the_marker() => _error.ShouldNotBeNull();
    [Fact] void should_identify_the_method_to_correct() => _error.Message.Contains("MissingOwner", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_explain_the_missing_owner_contract() => _error.Message.Contains("[ReadModel]", StringComparison.Ordinal).ShouldBeTrue();
}
