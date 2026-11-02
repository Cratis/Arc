// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup.when_resolving;

public class and_a_provider_returns_an_invalid_composite : given.a_caller_lookup
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => CommandReadModelLookup.Resolve(_context, typeof(CompositeCommand).GetMethod("Handle")!.GetParameters()[0], ReadProtectionMode.On, _traversal, out _));

    [Fact] void should_deny_the_lookup() => ((ReadModelKeyResolutionDenied)_exception).Reason.ShouldEqual(ReadModelKeyRefusal.RefusedKeyText);
    [Fact] void should_not_expose_the_composite_text() => _exception.Message.ShouldNotContain("private-part");

    class CompositeCommand
    {
        public void Handle([LookUpBy<CompositeKey>] Model model) { }
    }

    public sealed class CompositeKey : IReadModelKeyProvider
    {
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => ReadModelKeyLookup.Found(("private-part", (string?)null));
    }
}
