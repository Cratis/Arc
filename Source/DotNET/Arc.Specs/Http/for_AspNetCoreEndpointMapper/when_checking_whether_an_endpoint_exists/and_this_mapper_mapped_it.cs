// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_checking_whether_an_endpoint_exists;

public class and_this_mapper_mapped_it : given.an_endpoint_mapper
{
    const string EndpointName = "MappedByThisMapper";
    bool _mapped;
    bool _mappedWithoutMetadata;

    void Establish()
    {
        _mapper.MapPost("/test/mapped", _ => Task.CompletedTask, new EndpointMetadata(EndpointName, string.Empty, [], false));
        _mapper.MapGet("/test/anonymous", _ => Task.CompletedTask);
    }

    void Because()
    {
        _mapped = _mapper.EndpointExists(EndpointName);
        _mappedWithoutMetadata = _mapper.EndpointExists(string.Empty);
    }

    [Fact] void should_find_it() => _mapped.ShouldBeTrue();
    [Fact] void should_not_record_a_name_for_an_endpoint_mapped_without_metadata() => _mappedWithoutMetadata.ShouldBeFalse();
}
