// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Requires generated-target freshness on direct enrollments without changing the receiving unit.
/// </summary>
/// <param name="scopes">The invocation's live freshness registry.</param>
/// <param name="unit">The actual receiving unit of work.</param>
internal sealed class FreshIdentifierEnrollmentContributor(FreshIdentifierScopes scopes, IUnitOfWork unit) : IUnitOfWorkEnrollmentContributor
{
    /// <inheritdoc/>
    public ConcurrencyScope GetRequiredScope(UnitOfWorkEnrollment enrollment)
    {
        var resolved = scopes.Resolve(enrollment.EventSequenceId, enrollment.EventSourceId, enrollment.SuppliedScope, explicitScope: true);

        return scopes.HasGuard(enrollment.EventSequenceId, enrollment.EventSourceId) ? resolved! : ConcurrencyScope.NotSet;
    }

    /// <inheritdoc/>
    public void Enrolled(UnitOfWorkEnrollment enrollment, ConcurrencyScope effectiveScope) =>
        scopes.Enrolled(unit, enrollment.EventSequenceId, enrollment.EventSourceId);
}
