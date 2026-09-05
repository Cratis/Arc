// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryRequestReaders;

/// <summary>
/// Verifies that the query request readers are discovered by convention through the real type-discovery
/// system — the same mechanism <see cref="QueryEndpointMapper"/> relies on at runtime via
/// <c>IInstancesOf&lt;IQueryRequestReader&gt;</c>.
/// </summary>
public class when_discovering_implementations : Specification
{
    IEnumerable<Type> _readerTypes;

    void Because() => _readerTypes = Cratis.Types.Types.Instance.FindMultiple<IQueryRequestReader>();

    [Fact] void should_discover_the_query_string_reader() => _readerTypes.ShouldContain(typeof(QueryStringQueryRequestReader));
    [Fact] void should_discover_the_body_reader() => _readerTypes.ShouldContain(typeof(BodyQueryRequestReader));
}
