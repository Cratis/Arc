// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using NSubstitute;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_is_server_handled_command_response_value;

public class and_an_assembly_imitates_arc_operation_identity : Specification
{
    bool _result;

    void Because()
    {
        _ = typeof(ICommandOperation).Assembly;

        // Do not load a second Core-named assembly: host discovery later in the suite must remain isolated.
        var assembly = Substitute.For<Assembly>();
        assembly.GetName().Returns(new AssemblyName("Cratis.Arc.Core"));
        var marker = Substitute.For<Type>();
        marker.FullName.Returns("Cratis.Arc.Commands.ICommandOperation");
        marker.Assembly.Returns(assembly);
        marker.BaseType.Returns((Type?)null);
        marker.GetInterfaces().Returns([]);
        _result = marker.IsServerHandledCommandResponseValue();
    }

    [Fact] void should_require_the_actual_contract_assembly() => _result.ShouldBeFalse();
}
