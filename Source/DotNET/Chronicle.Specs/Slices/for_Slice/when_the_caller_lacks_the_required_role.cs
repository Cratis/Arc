// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Chronicle.Testing.Slices;

namespace Cratis.Arc.Chronicle.Slices.for_Slice;

public class when_the_caller_lacks_the_required_role : Specification
{
    Slice<given.SliceAdministration> _slice;
    SliceOutcome<given.SliceAdministration> _outcome;

    void Establish()
    {
        _slice = new(new given.SliceAdministration());
        _slice.GivenCaller(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "User")], "Slice")));
    }

    async Task Because() => _outcome = await _slice.Run();

    [Fact] void should_deny_the_command() => _outcome.ShouldBeDenied();
    [Fact] async Task should_not_append_events() => await _outcome.ShouldNotHaveAppendedEvents();

    async Task Destroy() => await _slice.DisposeAsync();
}
