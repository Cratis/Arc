// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_ArcAuthorizationPolicyRuntime;

public class when_an_explicit_scheme_list_is_malformed : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => AuthorizationRequirement.FromAttribute(null, null, "Bearer,"));

    [Fact] void should_reject_the_empty_scheme_name() => _error.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
}
