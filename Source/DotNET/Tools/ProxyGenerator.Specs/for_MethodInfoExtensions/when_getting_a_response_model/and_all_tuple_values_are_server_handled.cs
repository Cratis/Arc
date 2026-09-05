// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model.given;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model;

public class and_all_tuple_values_are_server_handled : Specification
{
    (bool HasResponse, Templates.ModelDescriptor ResponseModel) _result;

    void Because()
    {
        _ = typeof(DependencyHandledValueHandler).Assembly;
        _result = typeof(command_methods).GetMethod(nameof(command_methods.ReturnsAllHandledTuple))!.GetResponseModel();
    }

    [Fact] void should_not_expose_a_client_response() => _result.HasResponse.ShouldBeFalse();
}
