// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;
using Cratis.Arc.Validation;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions;

public class when_converting_a_command_with_an_assembly_blocking_severity : Specification
{
    CommandDescriptor _result;
    string _generatedCode;

    void Because()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"AssemblyPolicy{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(BlockOnValidationSeverityAttribute).GetConstructor([typeof(ValidationResultSeverity)])!, [ValidationResultSeverity.Warning]));
        var builder = assembly.DefineDynamicModule("Commands").DefineType("Commands.AssemblyPolicyCommand", TypeAttributes.Public);
        builder.DefineMethod("Handle", MethodAttributes.Public, typeof(void), Type.EmptyTypes).GetILGenerator().Emit(OpCodes.Ret);
        var type = builder.CreateType()!.GetTypeInfo();
        _result = type.ToCommandDescriptor("/output", 1, false, "api", [type]);
        _generatedCode = InMemoryProxyGenerator.GenerateCommand(_result);
    }

    [Fact] void should_use_the_assembly_policy() => _result.BlockOnValidationSeverity.ShouldEqual((int)ValidationResultSeverity.Warning);
    [Fact] void should_emit_the_policy() => _generatedCode.ShouldContain("readonly blockOnValidationSeverity = 2;");
}
