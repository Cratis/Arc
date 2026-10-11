// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model;

public class and_an_id_event_and_operations_are_returned : given.an_attributed_command_response
{
    void Because() => GenerateProxy(nameof(given.command_methods.ReturnsIdEventAndOperations));

    [Fact] void should_select_the_id() => _result.ResponseModel.Type.ShouldEqual(typeof(given.ProjectId));
    [Fact] void should_generate_the_id_response_type() => _proxy.ShouldContain("extends Command<IRegisterProject, Guid>");
}
