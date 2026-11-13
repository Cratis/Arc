// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Contexts;
using Cratis.Choreography.Availability.Context;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents an <see cref="IContextValueSource"/> that resolves the values a model provides with the Choreography engine.
/// </summary>
/// <remarks>
/// It reads through the <see cref="AvailabilityRequestContext"/> of the request, so a value resolves once for the request and the
/// availability gates, command handlers, query performers, policies and validation rules all see the same one.
/// </remarks>
/// <param name="requestContext">The <see cref="AvailabilityRequestContext"/> of the request.</param>
/// <param name="options">The <see cref="ArcAvailabilityOptions"/>, which decide the level a part is read in.</param>
public class ChoreographyContextValueSource(AvailabilityRequestContext requestContext, ArcAvailabilityOptions options) : IContextValueSource
{
    /// <inheritdoc/>
    public async ValueTask<ContextValueRead> Read(Type? part, string name, CancellationToken cancellationToken)
    {
        var scope = part is null ? ContextScope.Application : options.ScopeFor(part);
        try
        {
            var outcome = await requestContext.ResolverFor(scope).Resolve($"$context.{name}", cancellationToken);
            return outcome.IsPresent ? ContextValueRead.Present(outcome.Value) : ContextValueRead.Absent;
        }
        catch (Choreography.Availability.Context.RequiredContextValueAbsentException ex)
        {
            throw new Contexts.RequiredContextValueAbsentException(ex.Name, ex);
        }
        catch (ContextValueProviderFailedException ex)
        {
            throw new ContextValueUnavailableException(ex.Name, ex);
        }
    }
}
