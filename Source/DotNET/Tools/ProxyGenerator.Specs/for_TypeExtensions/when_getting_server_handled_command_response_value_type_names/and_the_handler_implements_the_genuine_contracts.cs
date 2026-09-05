// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_getting_server_handled_command_response_value_type_names;

public class and_the_handler_implements_the_genuine_contracts : Specification
{
    string[] _result;

    void Because() => _result = [.. TypeExtensions.GetServerHandledCommandResponseValueTypeNames(
        typeof(DependencyHandledValueHandler).GetInterfaces(),
        typeof(ICommandResponseValueHandler).Assembly)];

    [Fact] void should_declare_the_handled_value() =>
        _result.ShouldContain(typeof(DependencyHandledValue).AssemblyQualifiedName);

    [Fact] void should_declare_the_handled_collection() =>
        _result.ShouldContain(typeof(IEnumerable<DependencyHandledValue>).AssemblyQualifiedName);
}
