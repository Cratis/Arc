// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Arc.Commands;

/// <summary>
/// Guards the compatibility path for native union declarations without generated metadata.
/// </summary>
internal static class CommandUnionFeatureSwitches
{
    /// <summary>
    /// The runtime switch controlling untrimmed native union reflection.
    /// </summary>
    internal const string ReflectionSupportName = "Cratis.Arc.Commands.UnionReflection.IsSupported";

    /// <summary>
    /// Gets whether untrimmed JIT code may inspect union constructors. The package forces this off for trim/AOT.
    /// </summary>
    [FeatureSwitchDefinition(ReflectionSupportName)]
    [FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]
    [FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
    internal static bool ReflectionIsSupported { get; } =
        !AppContext.TryGetSwitch(ReflectionSupportName, out var supported) || supported;
}
