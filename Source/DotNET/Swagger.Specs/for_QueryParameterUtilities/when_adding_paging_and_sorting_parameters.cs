// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OpenApi;

namespace Cratis.Arc.Swagger.for_QueryParameterUtilities;

public class when_adding_paging_and_sorting_parameters : Specification
{
    OpenApiOperation _operation;

    void Establish()
    {
        _operation = new OpenApiOperation
        {
            Parameters = []
        };
    }

    void Because() => QueryParameterUtilities.AddPagingAndSortingParameters(_operation);

    [Fact] void should_add_four_parameters() => _operation.Parameters.Count.ShouldEqual(4);

    [Fact] void should_add_sortby_parameter() => _operation.Parameters.Any(p => p.Name == "sortby").ShouldBeTrue();
    [Fact] void should_add_sort_direction_parameter() => _operation.Parameters.Any(p => p.Name == "sortDirection").ShouldBeTrue();
    [Fact] void should_add_page_size_parameter() => _operation.Parameters.Any(p => p.Name == "pageSize").ShouldBeTrue();
    [Fact] void should_add_page_parameter() => _operation.Parameters.Any(p => p.Name == "page").ShouldBeTrue();

    [Fact] void should_set_all_as_query_parameters() => _operation.Parameters.All(p => p.In == ParameterLocation.Query).ShouldBeTrue();
    [Fact] void should_set_all_as_optional() => _operation.Parameters.All(p => !p.Required).ShouldBeTrue();

    [Fact] void should_set_page_size_as_int32() =>
        _operation.Parameters.First(p => p.Name == "pageSize").Schema.Format.ShouldEqual("int32");

    [Fact] void should_set_page_as_int32() =>
        _operation.Parameters.First(p => p.Name == "page").Schema.Format.ShouldEqual("int32");

    [Fact] void should_set_sortby_as_string() =>
        ((OpenApiSchema)_operation.Parameters.First(p => p.Name == "sortby").Schema).Type.ShouldEqual(JsonSchemaType.String);

    [Fact] void should_set_sort_direction_as_string_with_enum() =>
        _operation.Parameters.First(p => p.Name == "sortDirection").Schema.Enum.Count.ShouldEqual(2);
}
