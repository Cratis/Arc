// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphRowMapper.when_mapping;

public class with_a_value_that_is_not_a_map : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => GraphRowMapper.Map("not a map", OrganizationViews.Node("PersonWithGroups")));

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<UnexpectedGraphRow>();
}
