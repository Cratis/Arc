// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Determines whether a reflected method can be exposed as a model-bound query.
/// </summary>
internal static class ModelBoundQueryMethod
{
    /// <summary>
    /// Checks whether a method is an ordinary, publicly declared static query candidate.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>True when the method is eligible for query return-type validation.</returns>
    /// <remarks>Attribute names are compared rather than runtime types because proxy generation uses MetadataLoadContext.</remarks>
    internal static bool IsCandidate(MethodInfo method) =>
        method.IsPublic &&
        method.IsStatic &&
        !method.IsSpecialName &&
        !method.Name.Contains('<') &&
        !method.GetCustomAttributesData().Any(_ => _.AttributeType.Name == "CompilerGeneratedAttribute");
}
