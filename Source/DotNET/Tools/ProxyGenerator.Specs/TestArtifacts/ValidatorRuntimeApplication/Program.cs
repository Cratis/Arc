// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace ValidatorRuntimeApplication;

public static class Program
{
    public static void Main() { }

    [ModuleInitializer]
    public static void Initialize()
    {
        // Convention scans must execute in the application's runtime graph, not the generator's graph.
        var scanTarget = Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "ValidatorRuntimeScanTarget.dll"));
        _ = scanTarget.GetTypes();
    }
}
