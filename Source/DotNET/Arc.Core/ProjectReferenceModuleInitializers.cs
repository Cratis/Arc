// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Reflection;

namespace Cratis.Arc;

/// <summary>
/// Receives the project reference modules of an executable from the code the Arc generators emit into it.
/// </summary>
/// <remarks>
/// Not intended to be called directly. Generated code names a type in every project reference, so the module
/// initializers that register their generated metadata can run without loading assemblies by name at runtime.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ProjectReferenceModuleInitializers
{
    /// <summary>
    /// Registers the project reference modules of an executable.
    /// </summary>
    /// <param name="registeringAssembly">The executable the generated code is in.</param>
    /// <param name="projectReferenceModules">Gets the module of each project reference generated code could name a type in, by assembly name.</param>
    /// <param name="assembliesWithoutReachableTypes">Names of the project references generated code could not name a type in.</param>
    /// <remarks>
    /// Nothing runs here; the module initializers run when Arc is added to the application.
    /// </remarks>
    public static void Register(
        Assembly registeringAssembly,
        IReadOnlyDictionary<string, Func<Module>> projectReferenceModules,
        IEnumerable<string> assembliesWithoutReachableTypes)
    {
        ArgumentNullException.ThrowIfNull(registeringAssembly);
        ArgumentNullException.ThrowIfNull(projectReferenceModules);
        ArgumentNullException.ThrowIfNull(assembliesWithoutReachableTypes);

        GeneratedMetadataRegistration.Default.Register(registeringAssembly, projectReferenceModules, assembliesWithoutReachableTypes);
    }
}
