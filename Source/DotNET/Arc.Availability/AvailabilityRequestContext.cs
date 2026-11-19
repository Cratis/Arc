// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;
using Cratis.Choreography.Availability.Context;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents the availability context of one request: the audiences that apply and the context values, each resolved
/// lazily and once per request.
/// </summary>
/// <param name="catalog">The <see cref="ContextValueCatalog"/> of the application.</param>
/// <param name="audienceResolver">The <see cref="IAudienceResolver"/>.</param>
/// <param name="subjectProvider">The <see cref="IAvailabilitySubjectProvider"/>.</param>
public class AvailabilityRequestContext(ContextValueCatalog catalog, IAudienceResolver audienceResolver, IAvailabilitySubjectProvider subjectProvider)
{
    readonly Dictionary<string, ContextValueResolver> _resolvers = [];
    readonly Lock _lock = new();
    Task<EffectiveAudiences>? _audiences;
    AudienceSubject? _subject;

    AudienceSubject Subject => _subject ??= subjectProvider.GetSubject();

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
        }
    }

    Dictionary<string, object?> CreateAmbientValues()
    {
        var subject = Subject;
        return new()
        {
            ["tenant"] = subject.Tenant,
            ["identity"] = new Dictionary<string, object?> { ["user"] = subject.User, ["isSystem"] = subject.IsSystem }
        };
    }

    Task<EffectiveAudiences> ResolveAudiences(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            // A cancelled first caller must not poison the resolution for the rest of the request.
            if (_audiences?.IsCanceled ?? true)
            {
                _audiences = audienceResolver.Resolve(Subject, cancellationToken).AsTask();
            }

            return _audiences;
        }
    }
}
