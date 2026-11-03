// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.when_resolving;

public class and_a_decision_read_declares_a_key_provider : Specification
{
    ServiceProvider _services;
    Exception _exception;
    bool _activated;

    void Establish() => _services = new ServiceCollection()
        .AddTransient<DecisionRead<Model>>(_ =>
        {
            _activated = true;
            throw new InvalidOperationException();
        })
        .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([new ChronicleParameterDependencyResolver([typeof(Model)])]))
        .BuildServiceProvider();

    void Because() => _exception = Catch.Exception(() => ParameterDependencyResolver.Resolve(_services, typeof(Command).GetMethod("Handle")!.GetParameters()[0], _ => new InvalidOperationException()));

    void Destroy() => _services.Dispose();

    [Fact] void should_reject_the_unsupported_binding() => _exception.ShouldBeOfExactType<InvalidLookUpBy>();
    [Fact] void should_not_resolve_a_decision_read_through_the_command_key() => _activated.ShouldBeFalse();

    class Command
    {
        public void Handle([LookUpBy<CallerSubject>] DecisionRead<Model> model) { }
    }

    class Model;
}
