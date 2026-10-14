// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario;

/// <summary>
/// An account whose balance is accumulated by a reducer for collection observation.
/// </summary>
/// <param name="Id">The account identifier.</param>
/// <param name="Balance">The accumulated balance.</param>
[ReadModel]
public record ObservedReducedAccount(string Id, decimal Balance)
{
    /// <summary>
    /// Observes all reduced accounts.
    /// </summary>
    /// <param name="readModels">The scenario read models.</param>
    /// <returns>The account collection stream.</returns>
    [AllowAnonymous]
    public static ISubject<IEnumerable<ObservedReducedAccount>> All(IReadModels readModels) => readModels.Observe<ObservedReducedAccount>();
}
