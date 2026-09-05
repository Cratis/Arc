// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A generated document is meant to be committed, diffed and reviewed, which only works if the same source always
/// yields the same model. Symbol enumeration order is not something to rely on, so everything is ordered explicitly -
/// and this is what proves it, by analyzing two separate compilations of the same source.
/// </summary>
public class the_same_source_twice : Specification
{
    const string Source = """
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;
        using Cratis.Concepts;

        namespace Library.Authors.Registration;

        public record AuthorId(Guid Value) : ConceptAs<Guid>(Value);

        public record AuthorName(string Value) : ConceptAs<string>(Value);

        [EventType]
        [Tag("audit")]
        public record AuthorRegistered(AuthorName Name);

        [EventType]
        public record AuthorRetired(AuthorName Name);

        [Command]
        [Roles("Librarian")]
        public record RegisterAuthor(AuthorId Id, AuthorName Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }

        [Command]
        public record RetireAuthor(AuthorId Id, AuthorName Name)
        {
            public AuthorRetired Handle() => new(Name);
        }

        [ReadModel]
        [FromEvent<AuthorRegistered>]
        public record Author
        {
            [SetFrom<AuthorRegistered>("name")]
            public AuthorName Name { get; init; } = new(string.Empty);

            public static IEnumerable<Author> AllAuthors() => [];
        }
        """;

    string _first;
    string _second;

    void Because()
    {
        _first = Describe(Analyzed.Source(Source));
        _second = Describe(Analyzed.Source(Source));
    }

    static string Describe(ApplicationModelAnalysis analysis) =>
        string.Join('\n', [.. Lines(analysis)]);

    static IEnumerable<string> Lines(ApplicationModelAnalysis analysis)
    {
        yield return string.Join(',', analysis.Model.Concepts.Select(_ => $"{_.Name}:{_.Primitive}"));
        yield return string.Join(',', analysis.Model.Policies.Select(_ => _.Name));

        foreach (var slice in analysis.Model.Slices)
        {
            yield return $"{slice.Namespace}/{slice.Name}/{slice.Kind}";
            yield return string.Join(',', slice.Commands.Select(_ => $"{_.Name}({string.Join('|', _.Properties.Select(p => p.Name))})"));
            yield return string.Join(',', slice.Commands.SelectMany(_ => _.Produces).Select(_ => _.EventName));
            yield return string.Join(',', slice.Events.Select(_ => $"{_.Name}[{string.Join('|', _.Tags)}]"));
            yield return string.Join(',', slice.Queries.Select(_ => _.Name));
            yield return string.Join(',', slice.Projections.SelectMany(_ => _.Scope.From.SelectMany(from => from.EventTypes)));
        }

        yield return string.Join(',', analysis.Diagnostics.Select(_ => _.Code));
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_recover_the_same_model_both_times() => _second.ShouldEqual(_first);
    [Fact] void should_have_recovered_something_to_compare() => _first.ShouldNotBeEmpty();
}
