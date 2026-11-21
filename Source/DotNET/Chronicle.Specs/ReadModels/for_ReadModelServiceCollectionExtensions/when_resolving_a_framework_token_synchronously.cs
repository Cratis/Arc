// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_resolving_a_framework_token_synchronously : given.a_decision_token_ownership_probe
{
    ICanResolveParameterDependency _applicationResolver;
    object? _resolved;

    void Establish()
    {
        _applicationResolver = Substitute.For<ICanResolveParameterDependency>();
        _applicationResolver.TryResolve(Arg.Any<IServiceProvider>(), Arg.Any<ParameterInfo>(), out Arg.Any<object?>()).Returns(true);
        _services.AddSingleton(_applicationResolver);
    }

    void Because()
    {
        using var provider = BuildPlain();
        var parameter = typeof(Handler).GetMethod(nameof(Handler.Handle))!.GetParameters()[0];
        _resolved = ParameterDependencyResolver.Resolve(provider, parameter, _ => new InvalidOperationException());
    }

    [Fact] void should_issue_the_framework_token() => _resolved.ShouldBeOfExactType<DecisionRead<ModelBoundReadModel>>();
    [Fact] void should_use_the_existing_blocking_container_factory() => _readerCalls.ShouldContainOnly(true);
    [Fact] void should_not_consult_the_application_resolver() => _applicationResolver.DidNotReceiveWithAnyArgs().TryResolve(default!, default!, out _);

    class Handler
    {
        public void Handle(DecisionRead<ModelBoundReadModel> model) { }
    }
}
