// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

/// <summary>
/// A hosted query can be turned away before the pipeline proper runs. It is still traced and measured.
/// </summary>
public class with_a_traced_hosted_query_and_no_authorization_evaluation : given.a_traced_query_pipeline
{
    void Establish()
    {
        _queryPerformer.Type.Returns(typeof(ProtectedQuery));
        _queryPerformer.Name.Returns((QueryName)"Load");
        WithKnownQuery();
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([new PolicyDeclaration()])));
        _serviceProvider.GetService(typeof(AuthorizationEvaluation)).Returns((object?)null);
    }

    async Task Because() => _result = await _pipeline.PerformHosted(_queryName, QueryArguments.Empty, Paging.NotPaged, Sorting.None, _serviceProvider, CancellationToken.None);

    [Fact] void should_report_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_add_the_authorization_outcome() => QuerySpan.GetTagItem(WellKnownTelemetryNames.QueryOutcome).ShouldEqual(WellKnownOperationOutcomes.Authorization);
    [Fact] void should_record_the_exception_type() => QuerySpan.Events.Single(_ => _.Name == WellKnownTelemetryNames.ExceptionEvent).Tags.Single().Value.ShouldEqual(typeof(InvalidAuthorizationConfiguration).FullName);
    [Fact] void should_add_the_query_name() => QuerySpan.GetTagItem(WellKnownTelemetryNames.QueryName).ShouldEqual(_queryName.Value);
    [Fact] void should_record_the_duration_as_an_authorization_outcome() => Duration.Tags[WellKnownTelemetryNames.QueryOutcome].ShouldEqual(WellKnownOperationOutcomes.Authorization);

    public class ProtectedQuery
    {
        public object Load() => new();
    }

    class PolicyDeclaration : IAuthorizationAttributeEvaluator
    {
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method) =>
            method.DeclaringType == typeof(ProtectedQuery) && method.Name == nameof(ProtectedQuery.Load)
                ? [AuthorizationRequirement.FromAttribute(null, "Protected", null)] : [];
    }
}
