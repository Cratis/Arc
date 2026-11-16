// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_per_stream_models;

[PerFactSpecificationLifecycle]
public class when_multiple_route_parts_are_invalid : given.a_monthly_account
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_reject_lookup_redirection_before_a_blank_stream_part(bool asynchronous)
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Off));
        Begin(new Off(_account, " ", 100));
        var parameter = typeof(Withdraw).GetMethod(nameof(Withdraw.Redirect))!.GetParameters()[0];

        var failure = asynchronous
            ? await Catch.Exception(async () => await ParameterDependencyResolver.ResolveAsync(_services, parameter, _ => new InvalidOperationException()))
            : Catch.Exception(() => Resolve(parameter));

        failure.ShouldBeOfExactType<InvalidLookUpBy>();
        failure.Message.ShouldContain("the stream belongs to the command's own event source");
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_reject_a_blank_template_part_before_a_stream_type_mismatch(bool asynchronous)
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Off));
        Begin(new Off(_account, " ", 100), "Other");
        var parameter = typeof(Withdraw).GetMethod(nameof(Withdraw.Handle))!.GetParameters()[0];

        var failure = asynchronous
            ? await Catch.Exception(async () => await ParameterDependencyResolver.ResolveAsync(_services, parameter, _ => new InvalidOperationException()))
            : Catch.Exception(() => Resolve(parameter));

        failure.ShouldBeOfExactType<ReadModelStreamPartMissing>();
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }
}
