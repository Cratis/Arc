// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Observability;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

public partial class QueryPipeline
{
    static (MemberInfo Target, AuthorizationDeclaration Declaration) ResolveAuthorization(IQueryPerformer performer, IServiceProvider serviceProvider)
    {
        var declarations = serviceProvider.GetService<AuthorizationDeclarations>() ??
            throw new InvalidAuthorizationConfiguration("Authorization declarations are unavailable.");
        var target = QueryAuthorizationTarget.For(performer, declarations);
        var declaration = target switch
        {
            MethodInfo method => declarations.For(method),
            Type type => declarations.For(type),
            _ => throw new InvalidAuthorizationConfiguration($"Unsupported authorization target '{target}'.")
        };
        return (target, declaration);
    }

    static void CaptureSubscriptionScope(PerformanceState state)
    {
        // Capture the filter's effective scope before the performer can mutate its original value.
        // Every emission reconstructs its own copy from this baseline.
        if (state.Context.SubscriptionScope is not { } subscriptionScope)
        {
            return;
        }

        var serializerOptions = state.ServiceProvider.GetService<IOptions<ArcOptions>>()?.Value.JsonSerializerOptions
            ?? new ArcOptions().JsonSerializerOptions;
        state.Context.SubscriptionScopeSnapshot = new ObservableQuerySubscriptionScopeSnapshot(subscriptionScope, serializerOptions);
    }

    static void RecordAuthorizedResult(PerformanceState state)
    {
        var context = state.Context;
        state.Result.AuthorizedPrincipal = context.AuthorizedPrincipal;
        state.Result.AuthorizedArguments = context.Arguments;
        state.Result.AuthorizedQueryContext = context;
        if (context.AuthorizedPrincipal is not null)
        {
            state.Result.AuthorizedTenant = state.ServiceProvider.GetRequiredService<TenantIdAccessor>().Current;
        }
    }

    static void InstallPreparedIdentity(PerformanceState state)
    {
        // Install the prepared identity before any filter is discovered or constructed. Filter constructors can
        // resolve scoped, tenant-bound dependencies that must never be cached under the outer request identity.
        if (state.Prepared?.PrincipalChanged == true && state.Prepared.SelectedPrincipal is { } selectedPrincipal)
        {
            AttachPrincipal(state, selectedPrincipal);
        }
    }

    static void AttachPrincipal(PerformanceState state, System.Security.Claims.ClaimsPrincipal principal) =>
        state.PrincipalLease.Attach(state.ServiceProvider.GetRequiredService<AuthorizationPrincipalScope>().Begin(principal, state.ServiceProvider));

    static void EnsureLegacyFiltersAllowed(PerformanceState state)
    {
        if (state.Prepared?.Declaration.RequiresAsynchronousEvaluation == true)
        {
            throw new InvalidAuthorizationConfiguration("Advanced query authorization requires Arc's staged authorization filters before dependency construction.");
        }
    }

    static void AttachUnpreparedPrincipal(PerformanceState state)
    {
        if (state.Prepared is null && state.Context.AuthorizedPrincipal is { } authorizedPrincipal)
        {
            AttachPrincipal(state, authorizedPrincipal);
        }
    }

    static bool IsAuthorizationCurrent(PerformanceState state)
    {
        // Direct Perform calls do not prepare custom evaluator declarations without recognized attribute metadata.
        // Re-resolve even after a successful no-policy filter: a custom evaluator can change requirements on the
        // same target before invocation. Caching that absence could bypass a newly required policy. This costs
        // another declaration lookup on the no-policy path, but does not run a policy a second time.
        var (target, declaration) = ResolveAuthorization(state.Performer, state.ServiceProvider);
        var execution = state.Context.AuthorizedExecution;
        if (execution is null)
        {
            return !declaration.RequiresAsynchronousEvaluation;
        }

        return execution.IsCurrent(target, state.ServiceProvider.GetRequiredService<ICurrentPrincipalAccessor>(), declaration);
    }

    static bool IsSnapshotDenied(PerformanceState state) => state.Snapshot is { MembershipDenied: true };

    async Task<QueryResult> PerformCore(QueryRequest request, PreparedAuthorization? prepared, Activity? activity, CancellationToken cancellationToken)
    {
        var correlationId = GetCorrelationId();

        // A query performed from a protected command is not part of that command's decision; its own validators run.
        using var decisionQuery = CommandDecisionPolicy.BeginQuery();
        using var state = new PerformanceState(request, correlationId)
        {
            Prepared = prepared,
            Activity = activity,
            CancellationToken = cancellationToken
        };
        try
        {
            return await RunPerformance(state);
        }
        catch (MissingArgumentForQuery ex)
        {
            OperationActivity.RecordException(activity, ex);
            state.Result.MergeWith(QueryResult.WithValidationError(correlationId, ex.ParameterName, ex.Message));
        }
        catch (Exception ex) when (ex is IValidationFailure)
        {
            OperationActivity.RecordException(activity, ex);
            state.Result.MergeWith(QueryResult.FromException(correlationId, ex));
        }
        catch (AuthorizationIdentityChanged ex)
        {
            OperationActivity.RecordException(activity, ex);
            return QueryResult.Unauthorized(correlationId);
        }
        catch (Exception ex) when (UnauthorizedCause.IsCauseOf(ex))
        {
            OperationActivity.RecordException(activity, ex);
            state.Result.MergeWith(QueryResult.Unauthorized(correlationId));
        }
        catch (InvalidAuthorizationConfiguration ex)
        {
            OperationActivity.RecordException(activity, ex);
            state.Result.MergeWith(QueryResult.Unauthorized(correlationId));
            state.Result.ExceptionMessages = [.. state.Result.ExceptionMessages, ex.Message];
        }
        catch (Exception ex)
        {
            OperationActivity.RecordException(activity, ex);
            state.Result.MergeWith(QueryResult.Error(correlationId, ex));
        }

        return state.Result;
    }

    async Task<QueryResult> RunPerformance(PerformanceState state)
    {
        if (await ValidatePagingIfPaged(state) is { } invalidPaging)
        {
            return invalidPaging;
        }

        if (!queryPerformerProviders.TryGetPerformersFor(state.QueryName, out var queryPerformer))
        {
            return QueryResult.MissingPerformer(state.CorrelationId, state.QueryName);
        }

        state.Performer = queryPerformer;
        BuildContext(state);
        InstallPreparedIdentity(state);
        OperationActivity.AddResolvedTenant(state.Activity, state.ServiceProvider);

        // The authorization steps set ambient (async-local) state, so they run here, in the method that awaits the
        // filters, and not inside a nested async method whose ambient changes would be dropped on return.
        if (queryFilters is IStagedQueryFilters stagedFilters)
        {
            queryContextManager.Set(state.Context);
            state.Result = await stagedFilters.Authorize(state.Context);
            if (!state.Result.IsSuccess)
            {
                return state.Result;
            }

            AttachUnpreparedPrincipal(state);
            state.CancellationToken.ThrowIfCancellationRequested();
            BindDependencies(state);
            state.Result.MergeWith(await stagedFilters.AfterAuthorization(state.Context));
        }
        else
        {
            EnsureLegacyFiltersAllowed(state);

            // Preserve the contract of custom IQueryFilters implementations that expect dependencies on entry.
            BindDependencies(state);
            state.Result = await queryFilters.OnPerform(state.Context);
            if (state.Result.IsSuccess)
            {
                AttachUnpreparedPrincipal(state);
            }
        }

        return await ProduceResult(state);
    }

    async Task<QueryResult?> ValidatePagingIfPaged(PerformanceState state)
    {
        if (!state.Paging.IsPaged)
        {
            return null;
        }

        var pagingValidation = await ValidatePaging(state.Paging, state.CorrelationId);
        return pagingValidation.IsSuccess ? null : pagingValidation;
    }

    void BuildContext(PerformanceState state)
    {
        var coercedArguments = CoerceArguments(state.Arguments, state.Performer);
        state.Context = new QueryContext(state.QueryName, state.CorrelationId, state.Paging, state.Sorting, coercedArguments, ServiceProvider: state.ServiceProvider, CancellationToken: state.CancellationToken)
        {
            PreparedAuthorization = state.Prepared,
            ReceivedAt = OperationContextScope.Current ?? default,
            SliceLocation = SliceLocationFor(state.Performer)
        };
    }

    void BindDependencies(PerformanceState state)
    {
        AuthorizationExecutionScopes.MarkWorkStarted(state.ServiceProvider);
        var dependencies = state.Performer.Dependencies.Select(dependencyType => ResolveDependency(state.ServiceProvider, dependencyType)).ToArray();
        state.Context = state.Context with { Dependencies = dependencies };
        queryContextManager.Set(state.Context);
        state.Snapshot = OperationSnapshot.Begin(state.ServiceProvider, () => state.Context.AuthorizedPrincipalSnapshot);
        state.SnapshotLease.Attach(state.Snapshot);
    }

    async Task<QueryResult> ProduceResult(PerformanceState state)
    {
        if (IsSnapshotDenied(state))
        {
            return QueryResult.Unauthorized(state.CorrelationId);
        }

        if (!state.Result.IsSuccess)
        {
            return state.Result;
        }

        CaptureSubscriptionScope(state);
        RecordAuthorizedResult(state);
        state.CancellationToken.ThrowIfCancellationRequested();

        // The single choke point: a denied snapshot never reaches the performer.
        if (!IsAuthorizationCurrent(state) || IsSnapshotDenied(state))
        {
            return QueryResult.Unauthorized(state.CorrelationId);
        }

        return await InvokePerformer(state);
    }

    async Task<QueryResult> InvokePerformer(PerformanceState state)
    {
        var data = await state.Performer.Perform(state.Context);
        if (data is null)
        {
            return state.Result;
        }

        var rendererResult = queryRenderers.Render(state.QueryName, data, state.ServiceProvider);
        if (rendererResult is null)
        {
            return QueryResult.Error(state.CorrelationId, "No renderer result");
        }

        state.Result.Data = await ApplyInterceptors(state.Performer.ReadModelType, rendererResult.Data, state.ServiceProvider);
        state.Result.Paging = state.Context.Paging == Paging.NotPaged
            ? PagingInfo.NotPaged
            : new PagingInfo(state.Context.Paging.Page, state.Context.Paging.Size, rendererResult.TotalItems);
        return state.Result;
    }

    /// <summary>
    /// Holds the mutable state of one performance of a query as it moves through the pipeline steps.
    /// </summary>
    /// <param name="request">The <see cref="QueryRequest"/> being performed.</param>
    /// <param name="correlationId">The correlation id of the performance.</param>
    sealed class PerformanceState(QueryRequest request, CorrelationId correlationId) : IDisposable
    {
        public required PreparedAuthorization? Prepared { get; init; }

        public required Activity? Activity { get; init; }

        public required CancellationToken CancellationToken { get; init; }

        public CorrelationId CorrelationId { get; } = correlationId;

        public FullyQualifiedQueryName QueryName => request.QueryName;

        public QueryArguments Arguments => request.Arguments;

        public Paging Paging => request.Paging;

        public Sorting Sorting => request.Sorting;

        public IServiceProvider ServiceProvider => request.ServiceProvider;

        public AuthorizationPrincipalLease PrincipalLease { get; } = new();

        public AuthorizationPrincipalLease SnapshotLease { get; } = new();

        public QueryResult Result { get; set; } = QueryResult.Success(correlationId);

        public OperationSnapshot? Snapshot { get; set; }

        public IQueryPerformer Performer { get; set; } = null!;

        public QueryContext Context { get; set; } = null!;

        /// <inheritdoc/>
        public void Dispose()
        {
            SnapshotLease.Dispose();
            PrincipalLease.Dispose();
        }
    }
}
