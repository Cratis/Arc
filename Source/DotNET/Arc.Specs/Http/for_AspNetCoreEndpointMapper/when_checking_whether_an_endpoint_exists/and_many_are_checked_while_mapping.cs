// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_checking_whether_an_endpoint_exists;

/// <summary>
/// The guard that keeps a mapping pass from registering the same endpoint name twice used to ask the route builder
/// every time, and every one of those asks rebuilt the entire endpoint table - so mapping N endpoints cost N table
/// rebuilds and grew with the square of the number of commands and queries. The table must be read once.
/// </summary>
public class and_many_are_checked_while_mapping : given.an_endpoint_mapper
{
    const int Endpoints = 25;
    given.a_counting_endpoint_data_source _dataSource;

    void Establish()
    {
        _dataSource = new("AlreadyThere");
        _routeBuilder.DataSources.Add(_dataSource);
    }

    void Because()
    {
        for (var index = 0; index < Endpoints; index++)
        {
            var name = $"Endpoint{index}";
            if (!_mapper.EndpointExists(name))
            {
                _mapper.MapGet($"/test/{name}", _ => Task.CompletedTask, new EndpointMetadata(name, string.Empty, [], false));
            }
        }
    }

    [Fact] void should_materialize_the_endpoint_table_only_once() => _dataSource.Reads.ShouldEqual(1);
    [Fact] void should_map_every_endpoint() => Enumerable.Range(0, Endpoints).Count(index => _mapper.EndpointExists($"Endpoint{index}")).ShouldEqual(Endpoints);
}
