// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ArtifactCatalog;

/// <summary>
/// A specification that declares a projection to assert something about a contract, alongside the real one for
/// the same read model — the shape a capture of a real application actually meets.
/// </summary>
/// <remarks>
/// Both halves matter. The fixture must not be captured, because it ships only in Debug and the application
/// nobody debugs does not have it; and capturing it makes the document say a read model is built twice, which is
/// not legal Screenplay, so the document stops compiling and cannot be imported anywhere.
/// <para>
/// The specification here has no <c>Because</c>, deliberately: a specification that only inspects a contract
/// never needs one, and those are precisely the ones that declare a fixture. A rule written around the members a
/// specification usually holds would not recognise this as one.
/// </para>
/// </remarks>
public class when_a_specification_declares_a_fixture : Specification
{
    ApplicationModelAnalysis _analysis = null!;

    void Because() => _analysis = Analyzed.Source(
        Analyzed.Root,
        ("Library/Feature/Slice/Slice.cs", """
        using Cratis.Chronicle.Projections;
        using Cratis.Chronicle.ReadModels;

        namespace Library.Feature.Slice;

        [ReadModel]
        public record Balance(int Total);

        public class BalanceProjection : IProjectionFor<Balance>
        {
            public void Define(IProjectionBuilderFor<Balance> builder) => builder.From<Deposited>(_ => _);
        }

        [EventType]
        public record Deposited(int Amount);
        """),
        ("Library/Feature/Inspection/when_the_contract_is_inspected.cs", """
        using Cratis.Chronicle.Projections;
        using Cratis.Specifications;
        using Library.Feature.Slice;

        namespace Library.Feature.Inspection;

        public class when_the_contract_is_inspected : Specification
        {
            sealed class SpecificationProjection : IProjectionFor<Balance>
            {
                public void Define(IProjectionBuilderFor<Balance> builder) => builder.From<Deposited>(_ => _);
            }
        }
        """));

    [Fact] void should_capture_the_projection_the_application_ships() =>
        Projections().ShouldContain("Library.Feature.Slice.BalanceProjection");

    [Fact] void should_not_capture_the_fixture_the_specification_declares() =>
        Projections().Any(projection => projection.EndsWith("SpecificationProjection", StringComparison.Ordinal)).ShouldBeFalse();

    // The read model would otherwise be built twice, which is not legal Screenplay - the document stops
    // compiling and nothing downstream can read it.
    [Fact] void should_build_the_read_model_exactly_once() =>
        Projections().Count(projection => projection.Contains("Balance", StringComparison.Ordinal)).ShouldEqual(1);

    IEnumerable<string> Projections() =>
        _analysis.Model.Slices.SelectMany(slice => slice.Projections).Select(projection => projection.Identifier);
}
