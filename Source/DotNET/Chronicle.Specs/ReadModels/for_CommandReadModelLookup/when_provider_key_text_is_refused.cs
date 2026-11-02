// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup;

public class when_provider_key_text_is_refused : given.a_caller_lookup
{
    void Because()
    {
        _exceptions = new[] { "", " ", "*", EventSourceId.Unspecified.Value, "a#b", " key", "key " }.SelectMany(value =>
        {
            using var frame = AuthorizationCommandIdentity.Enter(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", value)], "test")));
            Refuse<Command>();
            return _exceptions;
        }).ToArray();
    }

    [Fact] void should_deny_without_exposing_the_key() => _exceptions.ShouldEachConformTo(exception => exception is ReadModelKeyResolutionDenied && !exception.Message.Contains("a#b", StringComparison.Ordinal));
}
