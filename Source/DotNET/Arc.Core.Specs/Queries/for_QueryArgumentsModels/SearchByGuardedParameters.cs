// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels;

/// <summary>
/// An arguments model whose constructor rejects what it is given, standing in for the guard clauses a record in this
/// codebase would normally carry.
/// </summary>
public class SearchByGuardedParameters
{
    public SearchByGuardedParameters() => throw new InvalidOperationException("Guarded");

    public string Name { get; set; } = string.Empty;
}
