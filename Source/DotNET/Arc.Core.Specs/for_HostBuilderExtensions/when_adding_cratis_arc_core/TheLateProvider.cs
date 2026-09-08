// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

/// <summary>
/// Stands in for a generated type discovery provider whose module constructor runs during the assembly closure
/// walk - after a universe has already been built from the providers registered up to that point.
/// </summary>
/// <remarks>
/// The one type it reports is a nested type with no accessibility modifier, so private - and the type discovery
/// generator emits nothing it could not reference by name, so this assembly's own generated provider does not
/// report it. Finding it in a universe therefore means that universe was built after this provider registered,
/// which is the whole point of the specification. Widening it would quietly take that meaning away.
/// </remarks>
public class TheLateProvider : ICanProvideAssembliesForDiscovery
{
    /// <summary>
    /// Gets the type no other provider reports.
    /// </summary>
    public static Type OnlyTypeItReports => typeof(ReportedByNothingElse);

    /// <inheritdoc/>
    public IEnumerable<Assembly> Assemblies => [];

    /// <inheritdoc/>
    public IEnumerable<Type> DefinedTypes => [OnlyTypeItReports];

    /// <inheritdoc/>
    public void Initialize()
    {
    }

    class ReportedByNothingElse;
}
