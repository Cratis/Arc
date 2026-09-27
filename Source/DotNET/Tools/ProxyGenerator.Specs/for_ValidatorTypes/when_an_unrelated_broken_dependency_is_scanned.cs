// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;
using Cratis.Arc.ProxyGenerator.Specs.TransitiveConceptValidator;

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes;

public class when_an_unrelated_broken_dependency_is_scanned : Specification
{
    Type? _validator;
    Exception? _error;
    List<string> _warnings = [];

    void Because()
    {
        var dependency = typeof(TransitiveNameValidator).Assembly;
        var modelAssembly = typeof(ReferencedEmail).Assembly;
        _error = Catch.Exception(() => _validator = ValidatorTypes.Find(
            typeof(when_an_unrelated_broken_dependency_is_scanned).Assembly,
            typeof(ReferencedEmail),
            [dependency],
            assembly => assembly == dependency
                ? throw new ReflectionTypeLoadException([null], [new TypeLoadException("Missing unrelated type")])
                : assembly == modelAssembly ? [typeof(ReferencedEmailValidator)] : [],
            _warnings.Add));
    }

    [Fact] void should_find_the_model_validator() => _validator.ShouldEqual(typeof(ReferencedEmailValidator));
    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_warn_about_the_unrelated_assembly() => _warnings.Single().ShouldContain(typeof(TransitiveNameValidator).Assembly.GetName().Name!);
}
