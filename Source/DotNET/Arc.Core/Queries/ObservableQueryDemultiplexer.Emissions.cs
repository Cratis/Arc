// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries;

public partial class ObservableQueryDemultiplexer
{
    enum GuardOutcome
    {
        Deliver = 0,
        Skip = 1,
        Stop = 2
    }

    static void AddOwnedResources(StreamingQuerySubscription lifetime, SemaphoreSlim emissionGate, IServiceScope interceptionScope)
    {
        var ownedGate = (SemaphoreSlim?)emissionGate;
        var ownedScope = (IServiceScope?)interceptionScope;
        try
        {
            lifetime.AddResource(ownedGate);
            ownedGate = null;
            lifetime.AddResource(ownedScope);
            ownedScope = null;
        }
        finally
        {
            ownedGate?.Dispose();
            ownedScope?.Dispose();
        }
    }

    static bool IsOrdered(QueryContext? queryContext) =>
        queryContext is not null &&
        queryContext != QueryContext.NotSet &&
        (queryContext.Sorting != Sorting.None || queryContext.Paging.IsPaged);

    static bool ShouldComputeChangeSet(SubjectEmissionState state, bool isFirstEmission) =>
        !state.IsFullMode && (!state.IsDeltaMode || !isFirstEmission);

    static bool HasAnyItems(SubjectEmissionState state, object[] currentItems) =>
        currentItems.Length > 0 || state.PreviousItems?.Any() is true;

    static PropertyInfo? FindItemIdentityProperty(SubjectEmissionState state, object[] currentItems)
    {
        var itemType = (currentItems.FirstOrDefault() ?? state.PreviousItems?.FirstOrDefault())?.GetType();
        itemType ??= state.EnumerableItemType;

        // Use the same case-insensitive Id convention as ChangeSetComputor, including concept-valued Ids.
        // An empty collection with no discoverable item type is a snapshot until an item establishes one.
        return itemType is null ? null : ChangeSetComputor.FindIdentityProperty(itemType);
    }

    static QueryResult CreateDeliverableResult(CorrelationId correlationId, object data, PagingInfo paging) => new()
    {
        CorrelationId = correlationId,
        Data = data,
        IsAuthorized = true,
        ValidationResults = [],
        ExceptionMessages = [],
        ExceptionStackTrace = string.Empty,
        Paging = paging
    };

    static QueryResult CreateSubjectResult(SubjectEmissionState state, object? interceptedData, EmissionChanges changes)
    {
        // Delta mode omits Data on subsequent identity-based emissions; without identity,
        // every emission carries the full snapshot and no change set.
        var omitData = state.IsDeltaMode && !changes.IsFirstEmission && changes.HasStableIdentity && !changes.SendSnapshot;
        var result = CreateDeliverableResult(state.CorrelationId, omitData ? null! : interceptedData!, state.Paging);

        // Only identity-based legacy emissions and subsequent delta emissions carry a change set.
        result.ChangeSet = changes.ChangeSet;

        if (state.SubscriptionQueryContext is { } queryContext && queryContext != QueryContext.NotSet)
        {
            result.Paging = new PagingInfo(
                queryContext.Paging.Page,
                queryContext.Paging.Size,
                queryContext.TotalItems);
        }

        return result;
    }

    /// <summary>
    /// Asks the emission guards, when any are registered, whether an emission may be delivered.
    /// </summary>
    /// <remarks>
    /// This runs before the delta baseline moves. Advancing the baseline first would fold a withheld emission's
    /// changes into "already delivered", so a later allowed emission would compute its ChangeSet against state the
    /// client never received and silently lose those changes.
    /// </remarks>
    /// <param name="state">The <see cref="SubjectEmissionState"/> of the subscription.</param>
    /// <param name="guardServiceProvider">The <see cref="IServiceProvider"/> handed to the guards.</param>
    /// <param name="onUnauthorized">Called when a guard denies the stream.</param>
    /// <param name="token">The <see cref="CancellationToken"/> ending the subscription.</param>
    /// <returns>True when the emission may be delivered; otherwise false.</returns>
    async Task<bool> AdmitSubjectEmission(
        SubjectEmissionState state,
        IServiceProvider guardServiceProvider,
        Func<string, CancellationToken, Task> onUnauthorized,
        CancellationToken token)
    {
        if (!emissionGuards.HasGuards)
        {
            return true;
        }

        var request = new GuardRequest(state.QueryId, state.Identity, state.CorrelationId, guardServiceProvider, !state.HasDeliveredEmission);
        var verdict = await GuardEmission(request, token);
        if (verdict == ObservableQueryEmissionVerdict.DenyAndTerminate)
        {
            state.IsTerminated = true;
        }

        return await ApplyGuardVerdict(verdict, request.QueryId, onUnauthorized, token) == GuardOutcome.Deliver;
    }

    async Task<GuardOutcome> EvaluateGuard(
        GuardRequest request,
        Func<string, CancellationToken, Task> onUnauthorized,
        CancellationToken token)
    {
        if (request.ServiceProvider is null)
        {
            return GuardOutcome.Deliver;
        }

        var verdict = await GuardEmission(request, token);
        return await ApplyGuardVerdict(verdict, request.QueryId, onUnauthorized, token);
    }

    async Task<ObservableQueryEmissionVerdict> GuardEmission(GuardRequest request, CancellationToken token)
    {
        var identity = request.Identity;
        var verdict = await emissionGuards.Guard(new ObservableQueryEmissionContext(
            identity.QueryName,
            identity.CreateArguments(),
            identity.Principal,
            request.CorrelationId,
            request.ServiceProvider!,
            request.IsFirstEmission,
            token)
        {
            SubscriptionScopeSnapshot = identity.SubscriptionScopeSnapshot,
            IsSnapshot = request.IsSnapshot
        });

        token.ThrowIfCancellationRequested();
        return verdict;
    }

    async Task<GuardOutcome> ApplyGuardVerdict(
        ObservableQueryEmissionVerdict verdict,
        string queryId,
        Func<string, CancellationToken, Task> onUnauthorized,
        CancellationToken token)
    {
        if (verdict == ObservableQueryEmissionVerdict.DenyAndTerminate)
        {
            logger.EmissionDenied(queryId);
            await onUnauthorized(queryId, token);
            return GuardOutcome.Stop;
        }

        if (verdict == ObservableQueryEmissionVerdict.Suppress)
        {
            logger.EmissionSuppressed(queryId);
            return GuardOutcome.Skip;
        }

        return GuardOutcome.Deliver;
    }

    /// <summary>
    /// Reports a live failure to the subscriber without letting a failing callback escape.
    /// </summary>
    /// <param name="queryId">The id of the query the subscription is for.</param>
    /// <param name="error">The failure to report.</param>
    /// <param name="onError">Callback invoked with the query id, message and token.</param>
    /// <param name="token">The <see cref="CancellationToken"/> ending the subscription.</param>
    /// <returns>True when the subscription had already ended while reporting; otherwise false.</returns>
    async Task<bool> ReportErrorQuietly(
        string queryId,
        Exception error,
        Func<string, string, CancellationToken, Task> onError,
        CancellationToken token)
    {
        try
        {
            await onError(queryId, error.Message, token);
        }
        catch (Exception callbackError) when (IsDisconnect(callbackError, token))
        {
            // The subscription ended while reporting the live failure.
            return true;
        }
        catch (Exception callbackError) when (!IsFatal(callbackError))
        {
            logger.SubscriptionError(queryId, callbackError);
        }

        return false;
    }

    /// <summary>
    /// Computes the change set for an emission and moves the delta baseline.
    /// </summary>
    /// <remarks>
    /// Enumerable results (excluding single-item and string results) get a ChangeSet. Delta mode skips the
    /// computation on the first emission (the full snapshot is sent instead); full mode skips it entirely.
    /// </remarks>
    /// <param name="state">The <see cref="SubjectEmissionState"/> of the subscription.</param>
    /// <param name="data">The raw emission.</param>
    /// <param name="interceptedData">The emission after interception.</param>
    /// <returns>The <see cref="EmissionChanges"/> to deliver.</returns>
    EmissionChanges ComputeEmissionChanges(SubjectEmissionState state, object? data, object? interceptedData)
    {
        var isFirstEmission = state.PreviousItems is null;
        if (interceptedData is not IEnumerable enumerable || interceptedData is string)
        {
            return new EmissionChanges(null, false, false, isFirstEmission);
        }

        var currentItems = enumerable.Cast<object>().ToArray();
        var identityProperty = FindItemIdentityProperty(state, currentItems);
        if (identityProperty is null)
        {
            WarnAboutMissingIdentity(state, currentItems);
        }

        var changes = new EmissionChanges(null, identityProperty is not null, false, isFirstEmission);
        if (identityProperty is not null && ShouldComputeChangeSet(state, isFirstEmission))
        {
            changes = ComputeChangeSet(state, data, currentItems, identityProperty);
        }

        state.PreviousItems = currentItems;
        return changes;
    }

    void WarnAboutMissingIdentity(SubjectEmissionState state, object[] currentItems)
    {
        if (state.HasWarnedAboutMissingIdentity || !HasAnyItems(state, currentItems))
        {
            return;
        }

        logger.CollectionWithoutIdentity(state.QueryId, state.Identity.QueryName.ToString());
        state.HasWarnedAboutMissingIdentity = true;
    }

    EmissionChanges ComputeChangeSet(SubjectEmissionState state, object? data, object[] currentItems, PropertyInfo identityProperty)
    {
        var previousItems = state.PreviousItems;
        var isFirstEmission = previousItems is null;

        // A provider that watches its data source already knows which items changed, and says so on
        // the emission. Taking it at its word makes the delta cost proportional to what changed rather
        // than to the size of the collection - comparing every item against the previous snapshot to
        // rediscover a single changed row is the expensive part of serving an observable query.
        //
        // The changes are read from the raw emission rather than the intercepted result, because
        // interception may hand back different instances; the identities it names still resolve
        // against the intercepted items.
        var changeSet = data is IHaveKnownChanges { Changes: { } knownChanges } && !isFirstEmission
            ? _changeSetComputor.ComputeFromKnownChanges(knownChanges, currentItems, previousItems)
            : _changeSetComputor.Compute(previousItems, currentItems);

        // A client appends added items and keeps the rest where they were, so a delta cannot say an
        // item moved. Where order is part of the answer and the delta would leave the client with a
        // different order, send the snapshot instead. Unordered queries keep the cheaper delta.
        var sendSnapshot = state.IsDeltaMode && !isFirstEmission && IsOrdered(state.SubscriptionQueryContext) &&
            !ChangeSetOrder.IsReproducedBy(changeSet, previousItems!, currentItems, identityProperty);

        return new EmissionChanges(sendSnapshot ? null : changeSet, true, sendSnapshot, isFirstEmission);
    }

    readonly record struct GuardRequest(
        string QueryId,
        ObservableQuerySubscriptionIdentity Identity,
        CorrelationId CorrelationId,
        IServiceProvider? ServiceProvider,
        bool IsFirstEmission,
        bool IsSnapshot = false);

    readonly record struct EmissionChanges(ChangeSet? ChangeSet, bool HasStableIdentity, bool SendSnapshot, bool IsFirstEmission);

    sealed class SubjectEmissionState
    {
        public required string QueryId { get; init; }
        public required ObservableQuerySubscriptionIdentity Identity { get; init; }
        public required CorrelationId CorrelationId { get; init; }
        public required PagingInfo Paging { get; init; }
        public required Type? EnumerableItemType { get; init; }
        public required string? TransferMode { get; init; }
        public required QueryContext? SubscriptionQueryContext { get; init; }
        public bool IsDeltaMode => string.Equals(TransferMode, "delta", StringComparison.OrdinalIgnoreCase);
        public bool IsFullMode => string.Equals(TransferMode, "full", StringComparison.OrdinalIgnoreCase);
        public IEnumerable<object>? PreviousItems { get; set; }
        public bool HasDeliveredEmission { get; set; }
        public bool HasWarnedAboutMissingIdentity { get; set; }
        public bool IsTerminated { get; set; }
    }
}
