// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model read through a query that keeps answering as it changes.
/// </summary>
public class an_observable_query : a_read_model_document
{
    const string Reactive = """
        namespace System.Reactive.Subjects
        {
            public interface ISubject<T>
            {
            }
        }
        """;

    const string Source = """
        using System.Collections.Generic;
        using System.Reactive.Subjects;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Live;

        [ReadModel]
        public record Author(string Name)
        {
            public static ISubject<IEnumerable<Author>> ObserveAll() => null!;
        }
        """;

    void Because() => Generate((Analyzed.SlicePath, Source), ("Library/Reactive.cs", Reactive));

    [Fact] void should_keep_the_query_observable() => Result.Source.ShouldContain("=> observable Author[]");
    [Fact] void should_declare_the_read_model() => Says("readmodel Author").ShouldBeTrue();
    [Fact] void should_resolve_every_reference() => UnresolvedReferences.ShouldBeEmpty();
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => RoundTrip.IsStable.ShouldBeTrue();
}
