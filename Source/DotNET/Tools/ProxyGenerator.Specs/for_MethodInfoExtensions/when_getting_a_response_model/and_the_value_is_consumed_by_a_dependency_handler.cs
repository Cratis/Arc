// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model.given;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model;

public class and_the_value_is_consumed_by_a_dependency_handler : Specification
{
    (bool HasResponse, Templates.ModelDescriptor ResponseModel) _result;

    void Because()
    {
        _ = typeof(DependencyHandledValueHandler).Assembly;
        _result = typeof(command_methods)
            .GetMethod(nameof(command_methods.ReturnsServerHandledValue))!
            .GetResponseModel();
    }

    [Fact] void should_discover_the_handler_from_a_dependency_assembly() => typeof(DependencyHandledValueHandler).Assembly.ShouldNotEqual(typeof(MethodInfoExtensions).Assembly);
    [Fact] void should_not_expose_a_client_response() => _result.HasResponse.ShouldBeFalse();
    [Fact] void should_not_create_a_response_model() => _result.ResponseModel.ShouldEqual(Templates.ModelDescriptor.Empty);
}
