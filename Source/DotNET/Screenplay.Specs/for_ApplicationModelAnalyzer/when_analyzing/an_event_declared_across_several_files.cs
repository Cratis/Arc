// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Roslyn hands over the members of a type split across partial declarations in the order the syntax trees arrived,
/// and the order a build globs its files in is not something anyone controls. A document meant to be committed and
/// diffed cannot reorder itself because the file system did, so the declarations are read in the order of the files
/// they live in rather than the order they were handed over.
/// </summary>
public class an_event_declared_across_several_files : Specification
{
    const string First = """
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public partial record AuthorRegistered
        {
            public string Alpha { get; init; } = string.Empty;
        }
        """;

    const string Second = """
        namespace Library.Authors.Registration;

        public partial record AuthorRegistered
        {
            public string Beta { get; init; } = string.Empty;
        }
        """;

    IEnumerable<string> _inOneOrder;
    IEnumerable<string> _inTheOther;

    void Establish()
    {
        _inOneOrder = PropertiesOf(
            ("Library/Authors/Registration/A.cs", First),
            ("Library/Authors/Registration/B.cs", Second));

        _inTheOther = PropertiesOf(
            ("Library/Authors/Registration/B.cs", Second),
            ("Library/Authors/Registration/A.cs", First));
    }

    static IEnumerable<string> PropertiesOf(params (string Path, string Text)[] sources) =>
        [.. Analyzed.Source(sources).Slice().Events.Single().Properties.Select(_ => _.Name)];

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Authors/Registration/A.cs", First), ("Library/Authors/Registration/B.cs", Second)).ShouldBeEmpty();
    [Fact] void should_read_the_declarations_in_the_order_of_the_files() => _inOneOrder.ShouldEqual(["Alpha", "Beta"]);
    [Fact] void should_read_them_the_same_way_whatever_order_they_arrived_in() => _inTheOther.ShouldEqual(_inOneOrder);
}
