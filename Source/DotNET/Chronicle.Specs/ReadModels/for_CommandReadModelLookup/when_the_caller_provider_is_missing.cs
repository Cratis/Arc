// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup;

public class when_the_caller_provider_is_missing : given.a_caller_lookup
{
    void Because() => Refuse<MissingCommand>();

    [Fact] void should_deny_without_a_client_key_fallback() => _exceptions.ShouldEachConformTo(exception => exception is ReadModelKeyResolutionDenied);

    class MissingCommand
    {
        public void Handle([LookUpBy<MissingKey>] Model model) { }
    }

    public sealed class MissingKey : IReadModelKeyProvider
    {
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => default;
    }
}
