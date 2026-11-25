// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_graph_source_order_changes : given.a_generated_document
{
    string _first = string.Empty;

    void Because()
    {
        Generate(given.GraphSources.Files());
        _first = Result.Source;
        Generate(given.GraphSources.Files().Reverse().ToArray());
    }

    [Fact] void should_bind_the_reordered_document() => AssertDocument();
    [Fact] void should_emit_identical_source() => Result.Source.ShouldEqual(_first);
}
