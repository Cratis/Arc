// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;
using Cratis.Arc.Validation;

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes;

public class when_unrelated_base_validators_share_a_model : Specification
{
    Type? _validator;
    Exception? _error;

    void Because()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("UnrelatedBaseValidators"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Validators");
        var first = module.DefineType("FirstUnrelatedValidator", TypeAttributes.Public, typeof(BaseValidator<string>)).CreateType();
        var second = module.DefineType("SecondUnrelatedValidator", TypeAttributes.Public, typeof(BaseValidator<string>)).CreateType();

        _error = Catch.Exception(() => _validator = ValidatorTypes.Find(assembly, typeof(ReferencedEmail), _ => [first, second, typeof(ReferencedEmailValidator)]));
    }

    [Fact] void should_not_fail_on_duplicates_for_an_unrelated_model() => _error.ShouldBeNull();
    [Fact] void should_find_the_requested_validator() => _validator.ShouldEqual(typeof(ReferencedEmailValidator));
}
