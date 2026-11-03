// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CallerSubject.when_resolving;

public class and_caller_has_one_subject : given.a_caller
{
    void Because() => _result = Resolve();

    [Fact] void should_find_the_subject() => _result.Key.ShouldEqual("represented");
    [Fact] void should_be_found() => _result.Outcome.ShouldEqual(ReadModelKeyLookupOutcome.Found);
}
