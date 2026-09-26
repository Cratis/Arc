// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;
using Cratis.Arc.ProxyGenerator.Specs.TransitiveConceptValidator;

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes;

public class when_an_unrelated_dependency_partially_loads : Specification
{
    Type? _validator;
    Exception? _error;
    List<string> _warnings = [];

    void Because()
    {
        var dependency = typeof(TransitiveNameValidator).Assembly;
        _error = Catch.Exception(() => _validator = ValidatorTypes.Find(
            typeof(when_an_unrelated_dependency_partially_loads).Assembly,
            typeof(TransitiveName),
            [dependency],
            assembly => assembly == dependency
                ? throw new ReflectionTypeLoadException(
                    [typeof(TransitiveNameValidator), null],
                    [new TypeLoadException("Missing unrelated type")])
                : [],
            _warnings.Add));
    }

    [Fact] void should_continue_with_the_loaded_validator() => _validator.ShouldEqual(typeof(TransitiveNameValidator));
    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_warn_about_the_dependency() => _warnings.Single().ShouldContain(typeof(TransitiveNameValidator).Assembly.GetName().Name!);
    [Fact] void should_include_the_loader_failure() => _warnings.Single().ShouldContain("Missing unrelated type");
}
