// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Execution;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandRefusalExtensions.when_classifying;

public class with_unsupported_authorization : Specification
{
    CommandFilters _filters;
    CommandContext _context;
    CommandResult _result;
    System.Diagnostics.ActivitySource _activitySource;

    void Establish()
    {
        var declarations = new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([]));
        var evaluation = new AuthorizationEvaluation(
            declarations,
            Substitute.For<IAuthorizationEvaluator>(),
            Substitute.For<ICurrentPrincipalAccessor>(),
            Substitute.For<IAuthorizationPolicyRuntime>());
        _context = new(CorrelationId.NotSet, typeof(object), new object(), [], new(), ServiceProvider: Substitute.For<IServiceProvider>());
        var filter = Substitute.For<IAuthorizationCommandFilter>();
        filter.OnExecution(_context).Returns(async _ =>
        {
            var allowed = await evaluation.IsAuthorized(
                typeof(CommandContext).GetProperty(nameof(CommandContext.Command))!,
                _context,
                _context.ServiceProvider!,
                CancellationToken.None);

            return allowed ? CommandResult.Success(_context.CorrelationId) : CommandResult.Unauthorized(_context.CorrelationId);
        });
        var source = Substitute.For<IActivitySource<CommandFilters>>();
        _activitySource = new("Cratis.Arc.Test");
        source.ActualSource.Returns(_activitySource);
        _filters = new(new KnownInstancesOf<ICommandFilter>([filter]), source);
    }

    async Task Because() => _result = await _filters.OnExecution(_context);
    void Cleanup() => _activitySource.Dispose();

    [Fact] void should_keep_the_unauthorized_outward_result() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_classify_unsupported_as_an_authorization_refusal() => _result.GetRefusal().ShouldBeNull();
    [Fact] void should_not_need_an_exception_in_the_result() => _result.HasExceptions.ShouldBeFalse();
}
