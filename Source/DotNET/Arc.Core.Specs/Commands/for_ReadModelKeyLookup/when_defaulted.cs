// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_ReadModelKeyLookup;

public class when_defaulted : Specification
{
    ReadModelKeyLookup _result;

    void Because() => _result = default;

    [Fact] void should_be_missing() => _result.Outcome.ShouldEqual(ReadModelKeyLookupOutcome.Missing);
}
