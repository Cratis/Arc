// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_is_server_handled_command_response_value;

public class and_a_type_has_the_same_full_name_from_another_assembly : Specification
{
    bool _result;

    void Because()
    {
        _ = typeof(DependencyHandledValueHandler).Assembly;
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Collision.{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Main");
        var type = module.DefineType(typeof(DependencyHandledValue).FullName!, TypeAttributes.Public).CreateType()!;
        _result = type.IsServerHandledCommandResponseValue();
    }

    [Fact] void should_not_treat_the_type_as_server_handled() => _result.ShouldBeFalse();
}
