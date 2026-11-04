// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup.when_resolving;

public class and_the_provider_key_has_no_strict_accessor : given.a_caller_lookup
{
    Exception _exception;

    void Establish() => _traversal = new(Options.Create(new ProtectedReadModelKeyOptions { Strict = true }), NullLogger<ProtectedReadModelKeyTraversal>.Instance);

    void Because() => _exception = Catch.Exception(() => CommandReadModelLookup.Resolve(_context, typeof(StrictCommand).GetMethod("Handle")!.GetParameters()[0], ReadProtectionMode.On, _traversal, out _));

    [Fact] void should_preserve_the_coverage_refusal() => _exception.ShouldBeOfExactType<ProtectedReadModelKeyAccessorMissing>();

    class StrictCommand
    {
        public void Handle([LookUpBy<CompositeKey>] Model model) { }
    }

    public sealed class CompositeKey : IReadModelKeyProvider
    {
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => ReadModelKeyLookup.Found(new OpaqueKey());
    }

    sealed class OpaqueKey
    {
        public string Part => "selected";
        public override string ToString() => "opaque";
    }
}
