// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.when_binding_asynchronously;

public class and_a_decision_read_declares_a_key_provider : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_reject_the_unsupported_binding_before_acquiring_a_token(bool frameworkIssued)
    {
        var activated = false;
        await using var services = new ServiceCollection()
            .AddTransient<DecisionRead<Model>>(_ =>
            {
                activated = true;
                throw new InvalidOperationException();
            })
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([
                new ChronicleParameterDependencyResolver([typeof(Model)], (_, _) => frameworkIssued)
            ]))
            .BuildServiceProvider();
        var parameter = typeof(Command).GetMethod(nameof(Command.Handle))!.GetParameters()[0];

        var failure = await Catch.Exception(async () => await ParameterDependencyResolver.ResolveAsync(services, parameter, _ => new InvalidOperationException()));

        failure.ShouldBeOfExactType<InvalidLookUpBy>();
        activated.ShouldBeFalse();
    }

    class Command
    {
        public void Handle([LookUpBy<CallerSubject>] DecisionRead<Model> model) { }
    }

    class Model;
}
