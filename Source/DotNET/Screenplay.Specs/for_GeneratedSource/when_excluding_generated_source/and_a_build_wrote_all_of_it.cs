// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_GeneratedSource.when_excluding_generated_source;

/// <summary>
/// Where a build wrote every last file, leaving nothing is not an improvement on leaving all of it - the question of
/// where the application sits still has to be answered, and answering it with nothing writes every path in the
/// document against the machine that generated it.
/// </summary>
public class and_a_build_wrote_all_of_it : Specification
{
    static readonly string[] _paths =
    [
        "Library/obj/Debug/net10.0/Some.Generator/Registration.g.cs",
        "Library/obj/Debug/net10.0/Some.Generator/Listing.g.cs"
    ];

    IReadOnlyList<string> _result;

    void Because() => _result = GeneratedSource.Excluded(_paths);

    [Fact] void should_fall_back_to_all_of_it() => _result.ShouldContainOnly(_paths);
}
