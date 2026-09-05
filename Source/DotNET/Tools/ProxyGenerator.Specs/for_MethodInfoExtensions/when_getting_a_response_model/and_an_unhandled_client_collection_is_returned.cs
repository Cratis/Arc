// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model.given;

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model;

public class and_an_unhandled_client_collection_is_returned : Specification
{
    (bool HasResponse, Templates.ModelDescriptor ResponseModel) _result;

    void Because() => _result = typeof(command_methods)
        .GetMethod(nameof(command_methods.ReturnsClientResponses))!
        .GetResponseModel();

    [Fact] void should_expose_a_client_response() => _result.HasResponse.ShouldBeTrue();
    [Fact] void should_preserve_the_collection_shape() => _result.ResponseModel.IsEnumerable.ShouldBeTrue();
    [Fact] void should_use_the_element_type() => _result.ResponseModel.Type.ShouldEqual(typeof(ClientResponse));
}
