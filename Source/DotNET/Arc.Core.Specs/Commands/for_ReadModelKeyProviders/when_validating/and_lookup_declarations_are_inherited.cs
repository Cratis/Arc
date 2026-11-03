// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_ReadModelKeyProviders.when_validating;

public class and_lookup_declarations_are_inherited : Specification
{
    Exception?[] _exceptions;

    void Because() => _exceptions = new[] { typeof(MixedCommand), typeof(DuplicateCommand) }.Select(type =>
        Catch.Exception(() => ReadModelKeyProviders.Validate(type.GetMethod("Handle")!.GetParameters()[0], type))).ToArray();

    [Fact] void should_reject_every_conflict_before_activation() => _exceptions.ShouldEachConformTo(exception => exception is InvalidLookUpBy);

    class BaseCommand
    {
        public virtual void Handle([LookUpBy<CallerSubject>] object model) { }
    }

    class MixedCommand : BaseCommand
    {
        public override void Handle([LookUpBy("Key")] object model) { }
    }

    class DuplicateCommand : BaseCommand
    {
        public override void Handle([LookUpBy<UnusedKey>] object model) { }
    }

    public sealed class UnusedKey : IReadModelKeyProvider
    {
        public UnusedKey() => throw new InvalidOperationException("The conflicting provider must not be constructed.");
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => throw new InvalidOperationException();
    }
}
