// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes;

public class when_loading_types_fails : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => ValidatorTypes.Find(
        typeof(ReferencedEmailValidator).Assembly,
        typeof(ReferencedEmail),
        _ => throw new ReflectionTypeLoadException(
            [typeof(ReferencedEmailValidator), null],
            [new TypeLoadException("Missing validator dependency")])));

    [Fact] void should_fail_instead_of_using_partial_types() => _error.ShouldBeOfExactType<ValidatorTypesCouldNotBeLoaded>();
    [Fact] void should_name_the_assembly() => _error!.Message.ShouldContain(typeof(ReferencedEmailValidator).Assembly.GetName().Name!);
    [Fact] void should_name_the_loader_failure() => _error!.Message.ShouldContain("Missing validator dependency");
}
