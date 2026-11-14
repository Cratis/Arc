// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Runs the proxy generator tool as a child process for specs.</summary>
internal static class ToolProcess
{
    /// <summary>Runs the generator against an assembly and captures its exit code and output streams.</summary>
    /// <param name="assembly">The assembly to generate from.</param>
    /// <param name="outputPath">The output folder.</param>
    /// <param name="arguments">Additional command line arguments.</param>
    /// <returns>The exit code, standard output and standard error.</returns>
    internal static async Task<(int ExitCode, string Output, string Error)> Run(string assembly, string outputPath, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in new[] { typeof(Generator).Assembly.Location, assembly, outputPath }.Concat(arguments)) startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await output, await error);
    }
}
