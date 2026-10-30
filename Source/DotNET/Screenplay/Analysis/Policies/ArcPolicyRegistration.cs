// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Policies;

/// <summary>
/// A native Arc policy registered by its implementation type rather than an inline builder.
/// </summary>
/// <param name="Name">The registered policy name.</param>
/// <param name="PolicyType">The implementation type supplied by the registration.</param>
/// <param name="SemanticModel">The semantic model containing the registration.</param>
/// <param name="Location">The registration's source path.</param>
internal sealed record ArcPolicyRegistration(string Name, ITypeSymbol PolicyType, SemanticModel SemanticModel, string Location)
    : PolicyRegistration(Name, null, SemanticModel, Location);
