// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.given;

/// <summary>
/// Audiences, availability blocks, a scope marker and gated parts declared as types.
/// </summary>
public static class TypedDeclarations
{
    public class Plus : Audience;

    public class Preview : Audience;

    public class Incident : Audience;

    public enum Package
    {
        Basic = 0,
        Standard = 1
    }

    public record Vessel(bool HasReported, int Score, Package CommercialPackage);

    public class Foundation : AvailabilityBlock
    {
        protected override IReadOnlyList<AvailabilityTerm> Lines => [For<Plus>()];
    }

    public class QualityScores : AvailabilityBlock
    {
        protected override IReadOnlyList<AvailabilityTerm> Lines =>
        [
            With<Foundation>(),
            Not<Incident>(),
            When.On<Vessel>(_ => _.HasReported).IsTrue()
        ];
    }

    public class EitherOrBlock : AvailabilityBlock
    {
        protected override IReadOnlyList<AvailabilityTerm> Lines => [For<Plus>() | (For<Preview>() & Not<Incident>())];
    }

    public class ConditionsOfOneWhen : AvailabilityBlock
    {
        protected override IReadOnlyList<AvailabilityTerm> Lines =>
        [
            When.On<Vessel>(_ => _.Score).IsGreaterThan(3) & When.On<Vessel>(_ => _.CommercialPackage).IsNot(Package.Basic)
        ];
    }

    public class WithAnOpaqueCondition : AvailabilityBlock
    {
        protected override IReadOnlyList<AvailabilityTerm> Lines => [When.Condition<AlwaysOpen>()];
    }

    public class AlwaysOpen : IAvailabilityCondition
    {
        public string Description => "always open";

        public ValueTask<AvailabilityOutcome> Evaluate(Context.IContextValueResolver context, CancellationToken cancellationToken) =>
            ValueTask.FromResult(AvailabilityOutcome.Available);
    }

    [Available<QualityScores>]
    public record ScoresCommand;

    [AvailableFor<Plus, Preview>]
    public record EitherAudienceCommand;

    [AvailableFor<Plus>]
    [AvailableFor<Preview>]
    public record BothAudiencesCommand;

    [AvailableWhen<AlwaysOpen>]
    public record OpaqueCommand;
}
