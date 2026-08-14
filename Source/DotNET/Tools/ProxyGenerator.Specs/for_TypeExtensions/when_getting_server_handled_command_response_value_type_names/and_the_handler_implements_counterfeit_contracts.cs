// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias FakeContracts;

using Cratis.Arc.Commands;
using FakeHandledValueHandler = FakeContracts::Cratis.Arc.ProxyGenerator.Specs.FakeCommandResponseHandlerDependency.FakeHandledValueHandler;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_getting_server_handled_command_response_value_type_names;

public class and_the_handler_implements_counterfeit_contracts : Specification
{
    string[] _result;

    void Because() => _result = [.. TypeExtensions.GetServerHandledCommandResponseValueTypeNames(
        typeof(FakeHandledValueHandler).GetInterfaces(),
        typeof(ICommandResponseValueHandler).Assembly)];

    [Fact] void should_not_declare_any_value_as_server_handled() => _result.ShouldBeEmpty();
}
