// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphDriverValues.when_converting;

public class with_a_local_date : Specification
{
    object? _result;

    void Because() => _result = GraphDriverValues.Convert(new LocalDate(2026, 3, 1));

    [Fact] void should_give_the_iso_8601_date() => ((string)_result!).ShouldEqual("2026-03-01");
}
