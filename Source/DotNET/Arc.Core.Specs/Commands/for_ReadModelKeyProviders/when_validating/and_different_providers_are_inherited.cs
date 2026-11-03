// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_ReadModelKeyProviders.when_validating;

public class and_different_providers_are_inherited : Specification
{
    Exception? _exception;

    void Because() => _exception = Catch.Exception(() => ReadModelKeyProviders.Validate(typeof(DerivedCommand).GetMethod("Handle").GetParameters()[0], typeof(DerivedCommand)));

    [Fact] void should_reject_the_conflict_before_activating_either_provider() => _exception.ShouldBeOfExactType<InvalidLookUpBy>();

    class BaseCommand
    {
        public virtual void Handle([LookUpBy<CallerSubject>] object model) { }
    }

    class DerivedCommand : BaseCommand
    {
        public override void Handle([LookUpBy<UnusedKey>] object model) { }
    }

    public sealed class UnusedKey : IReadModelKeyProvider
    {
        public UnusedKey() => throw new InvalidOperationException("The conflicting provider must not be constructed.");
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => throw new InvalidOperationException();
    }
}
