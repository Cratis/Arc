// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A reactor that turns what happened into something else that happens is translating. Reading the actual call into
/// the command pipeline is what tells that apart from a reactor that merely holds one, which is the distinction a
/// dependency-based guess cannot make.
/// </summary>
public class a_reactor_executing_a_command : Specification
{
    const string Source = """
        using System.Threading.Tasks;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;

        namespace Library.Lending.Restocking;

        [EventType]
        public record BookReserved(string Isbn);

        [Command]
        public record DecreaseStock(string Isbn)
        {
            public void Handle()
            {
            }
        }

        public class StockKeeping(ICommandPipeline pipeline) : IReactor
        {
            public async Task BookReserved(BookReserved @event, EventContext context) =>
                await pipeline.Execute(new DecreaseStock(@event.Isbn));
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_call_it_translating() => _analysis.Slice().Reactors.Single().IsTranslating.ShouldBeTrue();
    [Fact] void should_infer_a_translate_slice() => _analysis.Slice().Kind.ShouldEqual(SliceKind.Translate);
}
