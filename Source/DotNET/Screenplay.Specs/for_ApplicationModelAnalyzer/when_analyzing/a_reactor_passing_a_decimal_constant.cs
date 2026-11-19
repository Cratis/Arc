// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A decimal constant that survives the round trip through a double is kept as a literal on every runtime.
/// </summary>
public class a_reactor_passing_a_decimal_constant : Specification
{
    const string Source = """
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Reactors;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;

        namespace Library.Lending.Fees;

        [EventType]
        public record BookReturnedLate(string Isbn);

        [Command]
        public record ChargeFee(string Isbn, decimal Amount)
        {
            public void Handle()
            {
            }
        }

        public class FeeKeeping : IReactor
        {
            public CommandInvocation BookReturnedLate(BookReturnedLate @event) =>
                Invoke.Command(new ChargeFee(@event.Isbn, 1.23m));
        }
        """;

    ApplicationModelAnalysis _analysis;
    IEnumerable<PropertyMappingModel> _mappings;

    void Establish() => _analysis = Analyzed.Source(Source);

    void Because() => _mappings = _analysis.Slice().Reactors.Single().Reactions.Single().Invokes.Single().Mappings;

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_keep_the_constant_as_a_literal() => _mappings.Single(_ => _.Property == "Amount").Source.ShouldEqual(new LiteralSource(1.23m));
}
