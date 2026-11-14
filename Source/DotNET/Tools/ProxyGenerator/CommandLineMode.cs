// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Owns the command-line comparison mode and its output contract.</summary>
/// <param name="arguments">Command-line arguments.</param>
internal sealed class CommandLineMode(string[] arguments)
{
    /// <summary>Gets whether a non-mutating comparison was requested.</summary>
    public bool Check { get; } = arguments.Contains("--check", StringComparer.Ordinal);

    /// <summary>Writes normal generation progress unless comparing artifacts.</summary>
    /// <param name="message">Progress message.</param>
    public void WriteLine(string message = "")
    {
        if (!Check) Console.WriteLine(message);
    }

    /// <summary>Completes a successful comparison.</summary>
    /// <param name="output">The comparison sink.</param>
    /// <returns>The clean or drift exit code.</returns>
    public static int Complete(CheckingGeneratedOutput output)
    {
        Console.WriteLine(output.Report.ToText());
        return output.Report.Ordered().Any() ? ProxyGeneratorExitCodes.Drift : ProxyGeneratorExitCodes.Clean;
    }
}
