// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_ReadModelKeyProviders.when_validating;

public class and_the_same_provider_is_restated : Specification
{
    ILookUpByKeyProvider? _provider;

    void Because() => _provider = ReadModelKeyProviders.Validate(typeof(DerivedCommand).GetMethod("Handle").GetParameters()[0], typeof(DerivedCommand));

    [Fact] void should_accept_the_override_without_activating_the_provider() => _provider.ProviderType.ShouldEqual(typeof(UnusedKey));

    class BaseCommand
    {
        public virtual void Handle([LookUpBy<UnusedKey>] object model) { }
    }

    class DerivedCommand : BaseCommand
    {
        public override void Handle([LookUpBy<UnusedKey>] object model) { }
    }

    public sealed class UnusedKey : IReadModelKeyProvider
    {
        public UnusedKey() => throw new InvalidOperationException("Reading declarations must not construct the provider.");
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => throw new InvalidOperationException();
    }
}
