// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Samples.ArcCoreChronicle.Specs.Testing;

/// <summary>
/// Skips a published ArcCoreChronicle acceptance fact only when no executable is configured.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PublishedArcCoreChronicleFactAttribute : FactAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PublishedArcCoreChronicleFactAttribute"/> class.
    /// </summary>
    /// <param name="sourceFilePath">The compiler-provided source file path.</param>
    /// <param name="sourceLineNumber">The compiler-provided source line number.</param>
    public PublishedArcCoreChronicleFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (Environment.GetEnvironmentVariable("CRATIS_ARC_PUBLISHED_ARCCORECHRONICLE") is null)
        {
            Skip = "Set CRATIS_ARC_PUBLISHED_ARCCORECHRONICLE to run published ArcCoreChronicle acceptance specs.";
        }
    }
}
