// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Exception that gets thrown when multiple query performers are found for the same query.
/// </summary>
/// <param name="query">The query there is a duplicate of.</param>
public class MultipleQueryPerformersForSameReadModel(QueryName query) : Exception($"Multiple query performers found for query '{query}'")
{
    /// <summary>
    /// Throw if there are multiple handlers handling the same command.
    /// </summary>
    /// <param name="performers">The collection of <see cref="IQueryPerformer"/> to check against.</param>
    /// <exception cref="MultipleQueryPerformersForSameReadModel">The exception that gets thrown if there are multiple.</exception>
    public static void ThrowIfDuplicates(IEnumerable<IQueryPerformer> performers)
    {
        var duplicatePerformer = performers.GroupBy(GetCompositeKey).FirstOrDefault(g => g.Count() > 1)?.FirstOrDefault();
        if (duplicatePerformer is not null)
        {
            throw new MultipleQueryPerformersForSameReadModel(duplicatePerformer.Name);
        }
    }

    static string GetCompositeKey(IQueryPerformer performer) => $"{performer.ReadModelType.FullName}#{performer.Name}";
}