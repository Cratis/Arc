// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_handle_declarations_are_invalid : given.a_wiring_check
{
    Type _overloaded;

    void Establish()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("InvalidWiringSpec"), AssemblyBuilderAccess.Run);
        var command = assembly.DefineDynamicModule("Commands").DefineType("Overloaded", TypeAttributes.Public, typeof(OverloadedCommand));
        command.SetCustomAttribute(new CustomAttributeBuilder(typeof(CommandAttribute).GetConstructor(Type.EmptyTypes)!, []));
        _overloaded = command.CreateType()!;
    }
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(NoHandle), _overloaded, typeof(string)]);
    [Fact] void should_report_all_declarations() => Problems.Count.ShouldEqual(3);
    [Fact] void should_classify_discovery_failures() => Problems.All(_ => _.Category == "discovery").ShouldBeTrue();
}
