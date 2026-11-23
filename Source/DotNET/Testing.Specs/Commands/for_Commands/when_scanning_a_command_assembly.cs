// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_scanning_a_command_assembly : given.a_wiring_check
{
    Assembly _assembly;
    Exception _failure;

    void Establish()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("WiringSpec"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Commands");
        var command = module.DefineType("Command", TypeAttributes.Public);
        command.SetCustomAttribute(new CustomAttributeBuilder(typeof(CommandAttribute).GetConstructor(Type.EmptyTypes)!, []));
        var handle = command.DefineMethod("Handle", MethodAttributes.Private, typeof(void), Type.EmptyTypes);
        handle.GetILGenerator().Emit(OpCodes.Ret);
        command.CreateType();
        _assembly = assembly;
    }
    void Because() => _failure = Catch.Exception(() => CommandWiring.ShouldBeExecutableIn(Provider, _assembly, _assembly));
    [Fact] void should_discover_a_private_handle_and_deduplicate_assemblies() => _failure.ShouldBeNull();
}
