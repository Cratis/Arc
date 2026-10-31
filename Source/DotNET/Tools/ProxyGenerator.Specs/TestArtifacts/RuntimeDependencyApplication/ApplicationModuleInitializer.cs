// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Cratis.Arc.ProxyGenerator.Specs.RuntimeDependencyApplication;

internal static class ApplicationModuleInitializer
{
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries", Justification = "Reproduces application module initializers that scan runtime assembly copies.")]
    internal static void Initialize()
    {
        var current = typeof(ApplicationModuleInitializer).Assembly;

        // This regression stays meaningful only while the generator loads a second runtime copy of the project assembly.
        var copies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(_ => _.GetName().Name == current.GetName().Name)
            .OrderBy(_ => _ == current);
        foreach (var copy in copies)
        {
            copy.GetType(typeof(ApplicationTimings).FullName!)!.GetMethod(nameof(ApplicationTimings.Touch))!.Invoke(null, null);
        }
    }
}
