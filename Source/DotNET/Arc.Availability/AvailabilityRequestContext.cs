// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Suspensions;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents the availability context of one request: the audiences that apply and the context values, each resolved
/// lazily and once per request.
/// </summary>
/// <param name="catalog">The <see cref="ContextValueCatalog"/> of the application.</param>
/// <param name="audienceResolver">The <see cref="IAudienceResolver"/>.</param>
/// <param name="subjectProvider">The <see cref="IAvailabilitySubjectProvider"/>.</param>
[IgnoreConvention]
public class AvailabilityRequestContext(ContextValueCatalog catalog, IAudienceResolver audienceResolver, IAvailabilitySubjectProvider subjectProvider)
{
    readonly Dictionary<string, ContextValueResolver> _resolvers = [];
    readonly Lock _lock = new();
    readonly ConditionalWeakTable<AvailabilityInvocation, AvailabilityRequestContext> _invocations = new();
    Task<EffectiveAudiences>? _audiences;
    AvailabilityInvocation? _invocation;
    Task<SuspendedParts>? _suspended;
    AudienceSubject? _subject;

    /// <summary>
    /// Gets the subject of the request.
    /// </summary>
    public AudienceSubject Subject => _subject ??= subjectProvider.GetSubject();

    /// <summary>
    /// Gets what is suspended for the subject of the request, resolved once per request.
    /// </summary>
    /// <param name="source">The <see cref="ISuspensionSource"/> to ask.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation.</param>
    /// <returns>The <see cref="SuspendedParts"/>.</returns>
    public Task<SuspendedParts> Suspended(ISuspensionSource source, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            // A cancelled first caller must not poison the resolution for the rest of the request.
            if (_suspended is not { IsCanceled: false, IsFaulted: false })
            {
                _suspended = ResolveSuspended(source, Subject, cancellationToken);
            }

            return _suspended;
        }
    }

    /// <summary>
    /// Gets the resolver of context values for a scope. Values are resolved once per request.
    /// </summary>
    /// <param name="scope">The <see cref="ContextScope"/> the request is decided in.</param>
    /// <returns>The <see cref="ContextValueResolver"/>.</returns>
    public ContextValueResolver ResolverFor(ContextScope scope)
    {
        var key = string.Join('/', scope.Path);
        lock (_lock)
        {
            if (!_resolvers.TryGetValue(key, out var resolver))
            {
                resolver = catalog.CreateResolver(scope, CreateAmbientValues(), async cancellationToken => await ResolveAudiences(cancellationToken));
                _resolvers[key] = resolver;
            }

            return resolver;
        }
    }

    /// <summary>
    /// Forgets what was resolved, so the next decision reads the current state. The subject, who the request is, is kept.
    /// </summary>
    /// <remarks>
    /// A decision lives for one request. A client session that observes availability lives longer, and calls this when
    /// what the decisions are made from has changed.
    /// </remarks>
    public void Reset()
    {
        lock (_lock)
        {
            _resolvers.Clear();
            _audiences = null;
            _suspended = null;
        }
    }

    /// <summary>Gets the isolated resolver owner for one explicitly bound invocation.</summary>
    /// <param name="invocation">The identity shared by that invocation's gate and value readers.</param>
    /// <returns>An execution context that does not share metadata or another invocation's resolved values.</returns>
    internal AvailabilityRequestContext ForInvocation(AvailabilityInvocation invocation) =>
        _invocations.GetValue(invocation, key => new(catalog, audienceResolver, subjectProvider) { _invocation = key });

    static async Task<SuspendedParts> ResolveSuspended(ISuspensionSource source, AudienceSubject subject, CancellationToken cancellationToken) =>
        SuspendedParts.From(await source.GetDecisions(subject, cancellationToken));

    Dictionary<string, object?> CreateAmbientValues()
    {
        var subject = Subject;
        var ambient = new Dictionary<string, object?>
        {
            ["tenant"] = subject.Tenant,
            ["identity"] = new Dictionary<string, object?> { ["user"] = subject.User, ["isSystem"] = subject.IsSystem }
        };
        if (_invocation is { } invocation)
        {
            ambient["receivedAt"] = invocation.ReceivedAt;
            if (invocation.Arguments is { } arguments)
            {
                ambient["arguments"] = arguments;
            }
        }

        return ambient;
    }

    Task<EffectiveAudiences> ResolveAudiences(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            // A cancelled first caller must not poison the resolution for the rest of the request.
            if (_audiences is not { IsCanceled: false, IsFaulted: false })
            {
                _audiences = audienceResolver.Resolve(Subject, cancellationToken).AsTask();
            }

            return _audiences;
        }
    }
}
