// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Testing.Commands;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_preparation_refuses_the_caller : given.an_evaluation
{
    bool _pipelineDenied;
    void Establish()
    {
        var fallback = Substitute.For<IFallbackAuthorizationEvaluator>();
        fallback.GetAuthorizationRequirements(Arg.Any<Type>()).Returns(_ => throw new TenantMembershipDenied());
        Services.AddSingleton<IInstancesOf<IFallbackAuthorizationEvaluator>>(new KnownInstancesOf<IFallbackAuthorizationEvaluator>([fallback]));
    }

    async Task Because()
    {
        Result = await CommandAuthorization.For<given.UnrestrictedCommand>(Provider, Principal);
        await using var scenario = new CommandScenario<given.UnrestrictedCommand>();
        foreach (var descriptor in Services) scenario.Services.Add(descriptor);
        scenario.WithPrincipal(Principal);
        _pipelineDenied = !(await scenario.Execute(new given.UnrestrictedCommand())).IsAuthorized;
    }

    [Fact] void should_deny() => Result.ShouldBeDenied();
    [Fact] void should_match_pipeline_preparation() => _pipelineDenied.ShouldBeTrue();
    [Fact] void should_restore_the_principal() => Accessor.Current.ShouldBeNull();
}
