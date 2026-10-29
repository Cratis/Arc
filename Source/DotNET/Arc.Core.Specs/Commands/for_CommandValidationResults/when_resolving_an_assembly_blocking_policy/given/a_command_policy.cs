// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandValidationResults.when_resolving_an_assembly_blocking_policy.given;

public class a_command_policy : Specification
{
    protected Type _commandType;
    protected (ValidationResultSeverity? AllowedSeverity, bool BlockUnknown) _policy;

    protected static Type DeclareCommand(ValidationResultSeverity? assemblySeverity, ValidationResultSeverity? typeSeverity = null, Type? baseType = null, bool protectReads = false)
    {
        var constructor = typeof(BlockOnValidationSeverityAttribute).GetConstructor([typeof(ValidationResultSeverity)])!;
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"CommandPolicy{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        if (assemblySeverity is { } severity)
        {
            assembly.SetCustomAttribute(new CustomAttributeBuilder(constructor, [severity]));
        }
        var type = assembly.DefineDynamicModule("Commands").DefineType("Command", TypeAttributes.Public, baseType);
        if (typeSeverity is { } declaredSeverity)
        {
            type.SetCustomAttribute(new CustomAttributeBuilder(constructor, [declaredSeverity]));
        }
        if (protectReads)
        {
            type.SetCustomAttribute(new CustomAttributeBuilder(typeof(ReadProtectionAttribute).GetConstructor([typeof(ReadProtectionMode)])!, [ReadProtectionMode.On]));
        }
        return type.CreateType()!;
    }
}
