// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Cratis.Arc;

/// <summary>
/// Holds the feature switches Arc reads from <see cref="AppContext"/>. An application sets them with MSBuild
/// properties, which the Cratis.Arc package turns into runtime configuration options, so a trimmed or NativeAOT
/// publish can remove the code behind a switch that is off.
/// </summary>
static class ArcFeatureSwitches
{
    /// <summary>
    /// The name of the switch for MVC controllers, set by the <c>CratisArcControllersSupport</c> MSBuild property.
    /// </summary>
    internal const string ControllersSupportName = "Cratis.Arc.Controllers.IsSupported";

    /// <summary>
    /// Gets whether MVC controllers are supported, as the switch was when Arc first read it. On unless the
    /// application turns the switch off.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the guard for the trimmer and the trim and AOT analyzers: when the application sets the switch off, a
    /// trimmed or NativeAOT publish replaces this property with <see langword="false"/> and removes the code it
    /// guards. It is an auto-property on purpose; the analyzers check the body of a feature guard with a getter
    /// body and cannot prove that an <see cref="AppContext"/> read guards unreferenced or dynamic code.
    /// </para>
    /// <para>
    /// The trimmer only recognizes the switch from .NET 9 on. On .NET 8 an embedded <c>ILLink.Substitutions.xml</c>
    /// declares the same substitution.
    /// </para>
    /// </remarks>
#if NET9_0_OR_GREATER
    [FeatureSwitchDefinition(ControllersSupportName)]
    [FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]
    [FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
#endif
    internal static bool ControllersAreSupported { get; } = ControllersSupportIsOn;

    /// <summary>
    /// Gets whether the switch for MVC controllers is on right now. On unless the application turns it off.
    /// </summary>
    /// <remarks>
    /// Arc decides whether to register MVC from this as well as <see cref="ControllersAreSupported"/>, so a switch
    /// set with <see cref="AppContext.SetSwitch"/> before Arc is added applies even after the cached value was read.
    /// </remarks>
    internal static bool ControllersSupportIsOn => IsOn(ControllersSupportName);

    /// <summary>
    /// Gets whether a switch that is on by default is on: it is, unless it is set to <see langword="false"/>.
    /// </summary>
    /// <param name="switchName">The name of the switch.</param>
    /// <returns>True unless the switch is set to <see langword="false"/>.</returns>
    internal static bool IsOn(string switchName) =>
        !AppContext.TryGetSwitch(switchName, out var isOn) || isOn;
}
