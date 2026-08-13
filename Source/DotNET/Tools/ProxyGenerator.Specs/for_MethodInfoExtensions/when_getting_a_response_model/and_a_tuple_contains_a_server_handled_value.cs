// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model.given;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model;

public class and_a_tuple_contains_a_server_handled_value : Specification
{
    (bool HasResponse, Templates.ModelDescriptor ResponseModel) _direct;
    (bool HasResponse, Templates.ModelDescriptor ResponseModel) _wrapped;

    void Because()
    {
        _ = typeof(DependencyHandledValueHandler).Assembly;
        _direct = typeof(command_methods).GetMethod(nameof(command_methods.ReturnsHandledAndClientTuple))!.GetResponseModel();
        _wrapped = typeof(command_methods).GetMethod(nameof(command_methods.ReturnsWrappedHandledAndClientTuple))!.GetResponseModel();
    }

    [Fact] void should_expose_the_client_value_from_the_direct_tuple() => _direct.ResponseModel.Type.ShouldEqual(typeof(ClientResponse));
    [Fact] void should_expose_the_client_value_from_the_wrapped_tuple() => _wrapped.ResponseModel.Type.ShouldEqual(typeof(ClientResponse));
}
