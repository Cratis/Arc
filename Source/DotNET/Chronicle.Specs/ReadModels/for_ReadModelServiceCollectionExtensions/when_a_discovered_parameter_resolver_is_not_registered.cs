// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_a_discovered_parameter_resolver_is_not_registered : given.an_application_parameter_resolver
{
    Exception? _failure;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple<ICanResolveParameterDependency>().Returns([typeof(ApplicationResolver)]);
        _services.AddSingleton(types);
    }

    void Because() => _failure = Catch.Exception(() => ResolveSynchronously());

    [Fact] void should_fail_closed() => _failure.ShouldBeOfExactType<CannotResolveDependency>();
    [Fact] void should_name_the_resolver() => _failure!.Message.ShouldContain(typeof(ApplicationResolver).FullName!);
    [Fact] void should_explain_the_registration_requirement() => _failure!.Message.ShouldContain("must be registered");
    [Fact] void should_preserve_the_container_failure() => _failure!.InnerException!.InnerException.ShouldBeOfExactType<InvalidOperationException>();
}
