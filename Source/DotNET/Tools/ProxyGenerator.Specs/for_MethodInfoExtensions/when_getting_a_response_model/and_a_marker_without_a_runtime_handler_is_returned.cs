// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model.given;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model;

public class and_a_marker_without_a_runtime_handler_is_returned : Specification
{
    (bool HasResponse, Templates.ModelDescriptor ResponseModel) _result;

    void Because() => _result = typeof(command_methods)
        .GetMethod(nameof(command_methods.ReturnsMarkerOnlyValue))!
        .GetResponseModel();

    [Fact] void should_expose_a_client_response() => _result.HasResponse.ShouldBeTrue();
    [Fact] void should_use_the_returned_type() => _result.ResponseModel.Type.ShouldEqual(typeof(MarkerOnlyValue));
}
