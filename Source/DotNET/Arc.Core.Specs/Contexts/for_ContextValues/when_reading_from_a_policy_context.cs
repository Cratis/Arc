// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Contexts.for_ContextValues;

public class when_reading_from_a_policy_context : given.a_provided_vessel
{
    object _value;

    void Because()
    {
        var context = new AuthorizationPolicyContext(new ClaimsPrincipal(), typeof(string), "resource") { Services = _services };
        _value = context.Values.Vessel;
    }

    [Fact] void should_read_the_provided_value() => _value.ShouldEqual("Aurora");
    [Fact] void should_read_for_the_target() => _readFor.ShouldEqual(typeof(string));
}
