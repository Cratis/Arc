// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;
using Cratis.Arc.Validation;

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes;

public class when_finding_validators_for_a_concept_in_two_assemblies : Specification
{
    Exception? _error;

    void Because()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("GeneratedConceptValidator"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Validators");
        module.DefineType("GeneratedReferencedEmailValidator", TypeAttributes.Public, typeof(ConceptValidator<ReferencedEmail>)).CreateType();

        _error = Catch.Exception(() => ValidatorTypes.Find(assembly, typeof(ReferencedEmail)));
    }

    [Fact] void should_reject_the_duplicate_validators() => _error.ShouldBeOfExactType<MultipleValidatorsForType>();
    [Fact] void should_name_the_concept() => _error!.Message.ShouldContain(nameof(ReferencedEmail));
    [Fact] void should_name_the_generated_validator() => _error!.Message.ShouldContain("GeneratedReferencedEmailValidator");
    [Fact] void should_name_the_referenced_validator() => _error!.Message.ShouldContain(nameof(ReferencedEmailValidator));
}
