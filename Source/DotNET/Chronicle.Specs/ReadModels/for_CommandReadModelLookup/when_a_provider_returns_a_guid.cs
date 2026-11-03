// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup;

public class when_a_provider_returns_a_guid : given.a_caller_lookup
{
    void Because() => Resolve<GuidCommand>();

    [Fact] void should_canonicalize_the_scalar_in_every_mode() => _keys.ShouldEachConformTo(key => key.Value == "8aabf858-cf3b-431c-b283-f53c5c7842f8");

    class GuidCommand
    {
        public void Handle([LookUpBy<GuidKey>] Model model) { }
    }

    public sealed class GuidKey : IReadModelKeyProvider
    {
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => ReadModelKeyLookup.Found(new Guid(0x8aabf858, 0xcf3b, 0x431c, 0xb2, 0x83, 0xf5, 0x3c, 0x5c, 0x78, 0x42, 0xf8));
    }
}
