// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_if_an_assembly_can_declare_command_response_value_handlers;

public class and_the_assembly_references_the_contracts : Specification
{
    bool _contracts;
    bool _dependency;

    void Because()
    {
        _contracts = TypeExtensions.CanDeclareCommandResponseValueHandlers(typeof(ICommandResponseValueHandler).Assembly);
        _dependency = TypeExtensions.CanDeclareCommandResponseValueHandlers(typeof(DependencyHandledValueHandler).Assembly);
    }

    [Fact] void should_inspect_the_contracts_assembly() => _contracts.ShouldBeTrue();
    [Fact] void should_inspect_a_dependency_that_references_the_contracts() => _dependency.ShouldBeTrue();
}
