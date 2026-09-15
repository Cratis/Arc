// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Serialization;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

/// <summary>
/// Stands in for a generated type discovery provider whose module constructor runs during the assembly closure
/// walk - after a universe has already been built from the providers registered up to that point.
/// </summary>
/// <remarks>
/// The types it reports are nested types with no accessibility modifier, so private - and the type discovery
/// generator emits nothing it could not reference by name, so this assembly's own generated provider does not
/// report them. Finding one in a universe therefore means that universe was built after this provider registered,
/// which is the whole point of the specification. Widening them would quietly take that meaning away.
/// </remarks>
public class TheLateProvider : ICanProvideAssembliesForDiscovery
{
    /// <summary>
    /// Gets the type no other provider reports.
    /// </summary>
    public static Type OnlyTypeItReports => typeof(ReportedByNothingElse);

    /// <summary>
    /// Gets the derived type no other provider reports.
    /// </summary>
    /// <remarks>
    /// A universe alone cannot say whether the derived types were read off it: <c>IDerivedTypes</c> indexes only
    /// what carries <see cref="DerivedTypeAttribute"/>, so proving the derived types are not a separate narrower
    /// snapshot needs a late type this provider reports and that attribute marks.
    /// </remarks>
    public static Type OnlyDerivedTypeItReports => typeof(DerivedFromWhatNothingElseReports);

    /// <summary>
    /// Gets the target type <see cref="OnlyDerivedTypeItReports"/> is a derivative of.
    /// </summary>
    public static Type TargetOfTheOnlyDerivedTypeItReports => typeof(IReportedByNothingElseEither);

    /// <inheritdoc/>
    public IEnumerable<Assembly> Assemblies => [];

    /// <inheritdoc/>
    public IEnumerable<Type> DefinedTypes => [OnlyTypeItReports, OnlyDerivedTypeItReports];

    /// <inheritdoc/>
    public void Initialize()
    {
    }

    interface IReportedByNothingElseEither;

    class ReportedByNothingElse;

    [DerivedType("f3c5b0d4-5a3f-4a2a-9d1e-6c9f2b7a41e8")]
    class DerivedFromWhatNothingElseReports : IReportedByNothingElseEither;
}
