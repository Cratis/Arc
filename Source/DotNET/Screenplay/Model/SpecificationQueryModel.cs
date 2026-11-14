// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents the query a scenario calls and the one read model it expects back.
/// </summary>
/// <param name="Query">The name of the query, as the application declares it.</param>
/// <param name="Arguments">The values the query is called with, in the query's own formal order.</param>
/// <param name="Result">The values of the read model the query is expected to answer with, in its declaration order.</param>
/// <remarks>
/// A query scenario issues no command and appends nothing - the read model is substituted, the query is called, and
/// what follows is the instance it answers with. That is what <c>then query</c> says, so the scenario carries it as a
/// read rather than as a state that followed.
/// </remarks>
public record SpecificationQueryModel(
    string Query,
    IEnumerable<PropertyMappingModel> Arguments,
    IEnumerable<PropertyMappingModel> Result);
