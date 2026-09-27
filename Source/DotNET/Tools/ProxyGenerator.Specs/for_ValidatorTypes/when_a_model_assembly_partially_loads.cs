// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes;

public class when_a_model_assembly_partially_loads : Specification
{
    Exception? _error;
    List<string> _warnings = [];

    void Because()
    {
        var modelAssembly = typeof(TransitiveName).Assembly;
        _error = Catch.Exception(() => ValidatorTypes.Find(
            typeof(when_a_model_assembly_partially_loads).Assembly,
            typeof(TransitiveName),
            [modelAssembly],
            assembly => assembly == modelAssembly
                ? throw new ReflectionTypeLoadException([typeof(TransitiveName), null], [new TypeLoadException("Missing model type")])
                : [],
            _warnings.Add));
    }

    [Fact] void should_fail_even_when_the_model_is_also_a_dependency() => _error.ShouldBeOfExactType<ValidatorTypesCouldNotBeLoaded>();
    [Fact] void should_name_the_model_assembly() => _error!.Message.ShouldContain(typeof(TransitiveName).Assembly.GetName().Name!);
    [Fact] void should_not_warn() => _warnings.ShouldBeEmpty();
}
