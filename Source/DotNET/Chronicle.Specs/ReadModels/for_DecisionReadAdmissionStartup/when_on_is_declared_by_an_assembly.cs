// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Arc.Commands;
using Microsoft.Extensions.Logging;
using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.Declarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_on_is_declared_by_an_assembly : given.a_startup
{
    Type _command;

    void Establish()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"StartupOn{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(ReadProtectionAttribute).GetConstructor([typeof(ReadProtectionMode)])!, [ReadProtectionMode.On]));
        _command = assembly.DefineDynamicModule("Commands").DefineType("AssemblyOnCommand", TypeAttributes.Public, typeof(PlainCommand)).CreateType()!;
        Configure(_command);
    }
    Task Because() => StartAndCheck();

    [Fact] void should_check_inherited_parameters_under_assembly_on() => Logged(LogLevel.Critical, _command.FullName!).ShouldBeTrue();
    [Fact] void should_stop_the_host() => _lifetime.Received(1).StopApplication();
}
