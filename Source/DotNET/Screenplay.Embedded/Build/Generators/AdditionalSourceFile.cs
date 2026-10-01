// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// An additional file the generators of a project are given.
/// </summary>
/// <param name="path">The full path of the file.</param>
/// <remarks>
/// It is read the way the compiler reads it - from the file on disk, decoded as UTF-8 unless the file says
/// otherwise - and read once, since a generator asking for the same text twice must not be able to see the file
/// change underneath a single run.
/// </remarks>
public sealed class AdditionalSourceFile(string path) : AdditionalText
{
    readonly Lazy<SourceText> _text = new(() => Read(path), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <inheritdoc/>
    public override string Path { get; } = path;

    /// <inheritdoc/>
    public override SourceText GetText(CancellationToken cancellationToken = default) => _text.Value;

    /// <summary>
    /// Reads the file.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>The <see cref="SourceText"/>.</returns>
    static SourceText Read(string path)
    {
        using var stream = File.OpenRead(path);

        return SourceText.From(stream, Encoding.UTF8);
    }
}
