// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ObjCRuntime;
using UIKit;

namespace TestApps.Maui;

/// <summary>
/// Mac Catalyst entry point.
/// </summary>
/// <summary>
/// Mac Catalyst entry point. Must be a non-static class so the runtime can instantiate
/// <see cref="AppDelegate"/> via <c>typeof(AppDelegate)</c>.
/// </summary>
public class Program
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    static void Main(string[] args) =>
        UIApplication.Main(args, null, typeof(AppDelegate));
}
