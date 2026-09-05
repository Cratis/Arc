// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_GeneratedSource.when_excluding_generated_source;

/// <summary>
/// Where a person wrote some of the source, that is the whole of the answer to where the application is written.
/// </summary>
public class and_a_person_wrote_some_of_it : Specification
{
    IReadOnlyList<string> _result;

    void Because() => _result = GeneratedSource.Excluded(
    [
        "Library/Authors/Registration/Registration.cs",
        "Library/obj/Debug/net10.0/Some.Generator/Registration.g.cs",
        null,
        "   "
    ]);

    [Fact] void should_keep_only_what_a_person_wrote() => _result.ShouldContainOnly(["Library/Authors/Registration/Registration.cs"]);
}
