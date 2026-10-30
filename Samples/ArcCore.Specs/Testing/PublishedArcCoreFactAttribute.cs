// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Samples.ArcCore.Specs.Testing;

/// <summary>
/// Skips a published ArcCore acceptance fact only when no executable is configured.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PublishedArcCoreFactAttribute : FactAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PublishedArcCoreFactAttribute"/> class.
    /// </summary>
    /// <param name="sourceFilePath">The compiler-provided source file path.</param>
    /// <param name="sourceLineNumber">The compiler-provided source line number.</param>
    public PublishedArcCoreFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (Environment.GetEnvironmentVariable("CRATIS_ARC_PUBLISHED_ARCCORE") is null)
        {
            Skip = "Set CRATIS_ARC_PUBLISHED_ARCCORE to run published ArcCore acceptance specs.";
        }
    }
}
