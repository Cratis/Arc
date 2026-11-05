// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerContract.when_accepting;

public class the_supported_version : Specification
{
    bool _accepted;
    void Because() => _accepted = VerifiedCallerContract.Accepts("1");

    [Fact] void should_accept_version_one() => _accepted.ShouldBeTrue();
}
