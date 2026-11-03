// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication.given;

public class a_short_lived_actor_forward : a_signed_request
{
    protected DateTimeOffset _now;

    void Establish()
    {
        _now = DateTimeOffset.FromUnixTimeSeconds(1700000000);
        _clock.GetUtcNow().Returns(_now);
        ForwardedIdentityForSpecs.AddActor(_headers, _options, _now, expires: _now.AddSeconds(5));
        Resign(_now.ToUnixTimeSeconds());
    }
}
