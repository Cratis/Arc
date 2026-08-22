// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IObservableQueryEmissionGuards"/>.
/// </summary>
/// <param name="types">The <see cref="ITypes"/> used to discover <see cref="IGuardObservableQueryEmission"/> implementations.</param>
/// <param name="logger">The logger.</param>
/// <remarks>
/// The guard types are discovered once; the instances are created per emission from the <em>per-subscription</em>
/// service provider carried on the <see cref="ObservableQueryEmissionContext"/>. Resolving them from the root provider
/// instead would hand a guard whatever scoped collaborator happened to be cached there first, which for a
/// tenant-resolving or session-reading guard is the wrong answer rather than a missing one.
/// </remarks>
[Singleton]
public class ObservableQueryEmissionGuards(ITypes types, ILogger<ObservableQueryEmissionGuards> logger) : IObservableQueryEmissionGuards
{
    readonly Type[] _guardTypes = [.. types.FindMultiple<IGuardObservableQueryEmission>()];

    /// <inheritdoc/>
    public bool HasGuards => _guardTypes.Length > 0;

    /// <inheritdoc/>
    public async Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context)
    {
        var aggregate = ObservableQueryEmissionVerdict.Allow;
        var principalSnapshot = ClonePrincipal(context.Principal);
        var argumentsSnapshot = CloneArguments(context.Arguments);

        foreach (var guardType in _guardTypes)
        {
            ObservableQueryEmissionVerdict verdict;

            IGuardObservableQueryEmission guard;

            try
            {
                guard = (IGuardObservableQueryEmission)ActivatorUtilities.GetServiceOrCreateInstance(context.ServiceProvider, guardType);
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                return ObservableQueryEmissionVerdict.DenyAndTerminate;
            }
            catch (ObjectDisposedException) when (context.CancellationToken.IsCancellationRequested)
            {
                // The per-subscription provider was disposed during subscription teardown.
                // This is ordinary teardown, not an application guard failure.
                return ObservableQueryEmissionVerdict.DenyAndTerminate;
            }
            catch (Exception error) when (!IsFatal(error))
            {
                logger.EmissionGuardFailed(context.QueryName, guardType, error);
                return ObservableQueryEmissionVerdict.DenyAndTerminate;
            }

            try
            {
                var guardContext = context with
                {
                    Arguments = CloneArguments(argumentsSnapshot),
                    Principal = ClonePrincipal(principalSnapshot)
                };
                verdict = await guard.Guard(guardContext);
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                // The subscription ended while the guard was running — a closed tab, not a security failure. The
                // guard is told to observe this token and the documentation tells its author to, so logging an
                // ordinary teardown as "your authorization guard failed" would turn every disconnect into an Error.
                // The verdict is still the closed one: there is nothing left to write to.
                return ObservableQueryEmissionVerdict.DenyAndTerminate;
            }
            catch (Exception error) when (!IsFatal(error))
            {
                // Fail closed. A guard that cannot answer must not become an implicit allow — the application would
                // believe the stream is protected while it keeps flowing. Non-fatal failures are swallowed here on
                // purpose: the callers are async void emission callbacks, where anything escaping is unobserved.
                logger.EmissionGuardFailed(context.QueryName, guardType, error);
                return ObservableQueryEmissionVerdict.DenyAndTerminate;
            }

            if (verdict == ObservableQueryEmissionVerdict.DenyAndTerminate)
            {
                return ObservableQueryEmissionVerdict.DenyAndTerminate;
            }

            // Most restrictive wins — the verdicts are ordered, so the highest one seen so far is the aggregate.
            if (verdict > aggregate)
            {
                aggregate = verdict;
            }
        }

        return aggregate;
    }

    static ClaimsPrincipal? ClonePrincipal(ClaimsPrincipal? principal) =>
        principal is null
            ? null
            : new ClaimsPrincipal(principal.Identities.Select(identity => identity.Clone()));

    static QueryArguments CloneArguments(QueryArguments arguments)
    {
        var clone = new QueryArguments();
        foreach (var (key, value) in arguments)
        {
            clone[key] = value;
        }

        return clone;
    }

    static bool IsFatal(Exception exception) =>
        exception is OutOfMemoryException or
            StackOverflowException or
            AccessViolationException or
            AppDomainUnloadedException or
            BadImageFormatException or
            CannotUnloadAppDomainException or
            InvalidProgramException;
}
