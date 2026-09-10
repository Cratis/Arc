// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_is_server_handled_command_response_value;

public class and_a_dynamic_assembly_imitates_arc_operation_identity : Specification
{
    bool _result;

    void Because()
    {
        _ = typeof(ICommandOperation).Assembly;
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Cratis.Arc.Core"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Imitation");
        var marker = module.DefineType("Cratis.Arc.Commands.ICommandOperation", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Interface).CreateType();
        _result = marker.IsServerHandledCommandResponseValue();
    }

    [Fact] void should_require_the_actual_contract_assembly() => _result.ShouldBeFalse();
}
