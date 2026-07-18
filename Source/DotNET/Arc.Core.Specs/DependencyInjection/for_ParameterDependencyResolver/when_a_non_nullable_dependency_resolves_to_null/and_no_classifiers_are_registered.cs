// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver.when_a_non_nullable_dependency_resolves_to_null;

public class and_no_classifiers_are_registered : Specification
{
    ParameterInfo _parameter;
    IServiceProvider _serviceProvider;
    Exception _result;

    void Establish()
    {
        _parameter = typeof(Consumer).GetMethod(nameof(Consumer.Method))!.GetParameters()[0];
        _serviceProvider = new ServiceCollection().BuildServiceProvider();
    }

    void Because() => _result = Catch.Exception(() => ParameterDependencyResolver.Resolve(_serviceProvider, _parameter, _ => new TheDefaultFailure()));

    [Fact] void should_throw_the_default_failure() => _result.ShouldBeOfExactType<TheDefaultFailure>();

    class TheDefaultFailure : Exception;
}
