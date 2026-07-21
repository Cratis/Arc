// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels.ambiguous.beta;

/// <summary>
/// An unrelated type that happens to carry the same simple name as the alpha one, covering different arguments.
/// </summary>
public class LookupParameters
{
    public string Tenant { get; set; } = string.Empty;
}
