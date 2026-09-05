// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// A read model whose only generic method is a composition helper for its real queries - the shape that reaches
/// query discovery as a false positive, because its return type is indistinguishable from a query's.
/// </summary>
[ReadModel]
public class ReadModelWithGenericHelper
{
    public int Count { get; set; }

    public static ISubject<ReadModelWithGenericHelper> Totals(ISubject<IEnumerable<string>> source) =>
        CountOf(source);

    internal static ISubject<ReadModelWithGenericHelper> CountOf<TDocument>(ISubject<IEnumerable<TDocument>> source) =>
        Subject.Create<ReadModelWithGenericHelper>(
            Observer.Create<ReadModelWithGenericHelper>(_ => { }),
            source.Select(documents => new ReadModelWithGenericHelper { Count = documents.Count() }));
}
