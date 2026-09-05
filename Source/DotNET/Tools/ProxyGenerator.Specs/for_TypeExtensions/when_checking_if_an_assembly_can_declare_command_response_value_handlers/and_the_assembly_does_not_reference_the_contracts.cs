// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias FakeContracts;

using FakeHandledValueHandler = FakeContracts::Cratis.Arc.ProxyGenerator.Specs.FakeCommandResponseHandlerDependency.FakeHandledValueHandler;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_if_an_assembly_can_declare_command_response_value_handlers;

public class and_the_assembly_does_not_reference_the_contracts : Specification
{
    bool _counterfeitContracts;
    bool _unrelated;

    void Because()
    {
        _counterfeitContracts = TypeExtensions.CanDeclareCommandResponseValueHandlers(typeof(FakeHandledValueHandler).Assembly);
        _unrelated = TypeExtensions.CanDeclareCommandResponseValueHandlers(typeof(object).Assembly);
    }

    [Fact] void should_skip_a_dependency_shipping_counterfeit_contracts() => _counterfeitContracts.ShouldBeFalse();
    [Fact] void should_skip_an_unrelated_assembly() => _unrelated.ShouldBeFalse();
}
