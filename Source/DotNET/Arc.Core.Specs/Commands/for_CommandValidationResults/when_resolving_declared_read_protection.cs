// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandValidationResults;

public class when_resolving_declared_read_protection
{
    [Theory]
    [InlineData(ReadProtectionMode.On, null, true)]
    [InlineData(ReadProtectionMode.On, ReadProtectionMode.Off, false)]
    [InlineData(ReadProtectionMode.On, ReadProtectionMode.Measure, false)]
    [InlineData(ReadProtectionMode.Off, ReadProtectionMode.On, true)]
    [InlineData(ReadProtectionMode.Measure, null, false)]
    [InlineData(ReadProtectionMode.Off, null, false)]
    public void should_resolve_command_over_its_own_assembly(ReadProtectionMode assemblyMode, ReadProtectionMode? commandMode, bool blocks)
    {
        var type = DeclareCommand(assemblyMode, commandMode);
        var policy = CommandValidationResults.ForCommand(type, ValidationResultSeverity.Error);
        policy.BlockUnknown.ShouldEqual(blocks);
        policy.AllowedSeverity.ShouldEqual(blocks ? ValidationResultSeverity.Warning : ValidationResultSeverity.Error);
        CommandValidationResults.ForCommand(type, ValidationResultSeverity.Error).ShouldEqual(policy);
    }

    [Fact]
    public void should_inherit_a_command_declaration()
    {
        var policy = CommandValidationResults.ForCommand(typeof(DerivedOnCommand), ValidationResultSeverity.Error);
        policy.AllowedSeverity.ShouldEqual(ValidationResultSeverity.Warning);
        policy.BlockUnknown.ShouldBeTrue();
    }

    [Fact]
    public void should_not_inherit_the_base_types_assembly_declaration()
    {
        var baseType = DeclareCommand(ReadProtectionMode.On, null);
        var type = DeclareCommand(ReadProtectionMode.Off, null, baseType);
        CommandValidationResults.ForCommand(type, ValidationResultSeverity.Error).BlockUnknown.ShouldBeFalse();
    }

    [Fact]
    public void should_respect_a_command_opt_out_of_assembly_on()
    {
        var type = DeclareCommand(ReadProtectionMode.On, null, typeof(OptOutCommand));
        CommandValidationResults.ForCommand(type, ValidationResultSeverity.Error).BlockUnknown.ShouldBeFalse();
    }

    [Theory]
    [InlineData(ValidationResultSeverity.Unknown, true)]
    [InlineData(ValidationResultSeverity.Information, false)]
    [InlineData(ValidationResultSeverity.Warning, false)]
    [InlineData(ValidationResultSeverity.Error, true)]
    public void should_keep_only_errors_and_unknown_with_a_permissive_caller(ValidationResultSeverity severity, bool blocks)
    {
        var policy = CommandValidationResults.ForCommand(typeof(OnCommand), (ValidationResultSeverity)99);
        var failure = new ValidationResult(severity, "Rejected", [], null!);
        CommandValidationResults.Blocking([failure], policy.AllowedSeverity, policy.BlockUnknown).Any().ShouldEqual(blocks);
    }

    [Fact]
    public void should_allow_the_caller_to_be_stricter()
    {
        var policy = CommandValidationResults.ForCommand(typeof(OnCommand), ValidationResultSeverity.Unknown);
        policy.AllowedSeverity.ShouldEqual(ValidationResultSeverity.Unknown);
    }

    [Fact]
    public void should_use_an_explicit_inherited_policy_instead_of_the_error_floor()
    {
        var policy = CommandValidationResults.ForCommand(typeof(DerivedStrictCommand), ValidationResultSeverity.Error);
        policy.AllowedSeverity.ShouldEqual(ValidationResultSeverity.Unknown);
        policy.BlockUnknown.ShouldBeTrue();
    }

    static Type DeclareCommand(ReadProtectionMode assemblyMode, ReadProtectionMode? commandMode, Type? baseType = null)
    {
        var constructor = typeof(ReadProtectionAttribute).GetConstructor([typeof(ReadProtectionMode)])!;
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"ValidationFloor{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        assembly.SetCustomAttribute(new CustomAttributeBuilder(constructor, [assemblyMode]));
        var type = assembly.DefineDynamicModule("Commands").DefineType("Command", TypeAttributes.Public, baseType);
        if (commandMode is { } mode)
        {
            type.SetCustomAttribute(new CustomAttributeBuilder(constructor, [mode]));
        }
        return type.CreateType()!;
    }

    [ReadProtection(ReadProtectionMode.On)]
    public class OnCommand;

    public class DerivedOnCommand : OnCommand;

    [ReadProtection(ReadProtectionMode.On), BlockOnValidationSeverity(ValidationResultSeverity.Information)]
    public class StrictCommand;

    public class DerivedStrictCommand : StrictCommand;

    [OptOut]
    public class OptOutCommand;

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class OptOutAttribute : Attribute, IUnprotectedDecisionAttribute;
}
