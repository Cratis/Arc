// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;
using Cratis.Arc.Validation;

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes;

public class when_finding_multiple_validators_for_a_concept : Specification
{
    Exception? _error;

    void Because()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DuplicateValidators"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Validators");
        var baseType = typeof(ConceptValidator<ReferencedEmail>);
        var first = module.DefineType("FirstReferencedEmailValidator", TypeAttributes.Public, baseType).CreateType();
        var second = module.DefineType("SecondReferencedEmailValidator", TypeAttributes.Public, baseType).CreateType();

        _error = Catch.Exception(() => ValidatorTypes.Find(assembly, typeof(ReferencedEmail), _ => [second, first]));
    }

    [Fact] void should_reject_the_duplicate_validators() => _error.ShouldBeOfExactType<MultipleValidatorsForType>();
    [Fact] void should_name_the_concept() => _error!.Message.ShouldContain(nameof(ReferencedEmail));
    [Fact] void should_name_the_first_validator() => _error!.Message.ShouldContain("FirstReferencedEmailValidator");
    [Fact] void should_name_the_second_validator() => _error!.Message.ShouldContain("SecondReferencedEmailValidator");
}
