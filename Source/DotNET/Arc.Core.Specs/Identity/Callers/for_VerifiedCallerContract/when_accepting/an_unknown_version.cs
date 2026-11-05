// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerContract.when_accepting;

/// <summary>Vector 15.</summary>
public class an_unknown_version : Specification
{
    bool[] _accepted;
    void Because() => _accepted = new string?[] { "2", null, "", "01", "1 " }.Select(VerifiedCallerContract.Accepts).ToArray();

    [Fact] void should_refuse_every_unknown_version() => _accepted.Any(value => value).ShouldBeFalse();
}
