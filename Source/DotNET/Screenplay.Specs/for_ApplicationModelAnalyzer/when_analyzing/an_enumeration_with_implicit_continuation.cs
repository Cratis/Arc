// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class an_enumeration_with_implicit_continuation : Specification
{
    const string Source = """
        using Cratis.Chronicle.Events;
        namespace Library.Orders.Placing;
        public enum Status { Unknown = -1, Draft, Issued = 4, Paid }
        [EventType] public record OrderPlaced(Status Status);
        """;

    ApplicationModelAnalysis _result;

    void Because() => _result = Analyzed.Source(Source);

    [Fact] void should_compile_the_source() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_recover_implicit_numbers_from_the_compiler() => _result.Model.Concepts.Single().EnumNumbers.Select(number => (int)number).ShouldEqual([-1, 0, 4, 5]);
}
