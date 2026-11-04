// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver.when_resolving;

public class and_provider_metadata_is_inherited : Specification
{
    ServiceProvider _services;
    Exception? _exception;

    void Establish() => _services = new ServiceCollection().BuildServiceProvider();

    void Because()
    {
        var parameter = typeof(DerivedCommand).GetMethod("Handle")!.GetParameters()[0];
        _exception = Catch.Exception(() => ParameterDependencyResolver.Resolve(_services, parameter, _ => new InvalidOperationException()));
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_refuse_the_inherited_lookup_without_activating_the_provider() => _exception.ShouldBeOfExactType<InvalidLookUpBy>();

    class BaseCommand
    {
        public virtual void Handle([LookUpBy<UnusedKey>] object model) { }
    }

    class DerivedCommand : BaseCommand
    {
        public override void Handle(object model) { }
    }

    public sealed class UnusedKey : IReadModelKeyProvider
    {
        public UnusedKey() => throw new InvalidOperationException("Metadata must not activate providers.");
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => throw new InvalidOperationException();
    }
}
