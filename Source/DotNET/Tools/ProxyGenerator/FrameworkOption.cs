// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Parses the optional framework adapter selection.
/// </summary>
internal static class FrameworkOption
{
    /// <summary>Parses the framework option, defaulting to React when omitted or empty.</summary>
    /// <param name="arguments">The command-line arguments.</param>
    /// <returns>The selected framework.</returns>
    /// <exception cref="ArgumentException">The framework value is not supported.</exception>
    internal static ProxyFramework Parse(IEnumerable<string> arguments)
    {
        var option = arguments.FirstOrDefault(argument => argument == "--framework" || argument.StartsWith("--framework=", StringComparison.Ordinal));
        return option switch
        {
            null or "--framework=" or "--framework=react" => ProxyFramework.React,
            "--framework=none" => ProxyFramework.None,
            _ => throw new ArgumentException("Invalid framework. Expected --framework=none or --framework=react.")
        };
    }
}
