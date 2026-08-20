// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_checking_whether_an_endpoint_exists;

public class and_it_was_registered_before_the_mapper_started : given.an_endpoint_mapper
{
    bool _existing;
    bool _unrelated;

    void Establish() => _routeBuilder.DataSources.Add(new given.a_counting_endpoint_data_source("MappedBySomethingElse"));

    void Because()
    {
        _existing = _mapper.EndpointExists("MappedBySomethingElse");
        _unrelated = _mapper.EndpointExists("NeverMapped");
    }

    [Fact] void should_find_the_endpoint_this_mapper_did_not_map() => _existing.ShouldBeTrue();
    [Fact] void should_not_find_an_endpoint_nothing_mapped() => _unrelated.ShouldBeFalse();
}
