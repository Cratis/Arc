// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Chronicle.Testing.Slices;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Slices.for_Slice;

public class when_the_caller_has_the_required_role : Specification
{
    Slice<given.SliceAdministration> _slice;
    SliceOutcome<given.SliceAdministration> _outcome;

    void Establish()
    {
        _slice = new(new given.SliceAdministration());
        _slice.GivenCaller(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Administrator")], "Slice")));
    }

    async Task Because() => _outcome = await _slice.Run();

    [Fact] void should_allow_the_command() => _outcome.ShouldBeSuccessful();
    [Fact] void should_run_the_handler() => ((CommandResult<string>)_outcome.Result).Response.ShouldEqual("Completed");

    async Task Destroy() => await _slice.DisposeAsync();
}
