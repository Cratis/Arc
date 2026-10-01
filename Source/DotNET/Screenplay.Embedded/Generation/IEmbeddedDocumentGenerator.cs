// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Defines a system that generates every Screenplay document an assembly embeds.
/// </summary>
/// <remarks>
/// The compilation is handed in rather than loaded, exactly as the generator this builds on takes it, so the same
/// generation runs from an MSBuild task and from a specification that compiled source in memory.
/// </remarks>
public interface IEmbeddedDocumentGenerator
{
    /// <summary>
    /// Generates every document the compilation is embedded as.
    /// </summary>
    /// <param name="compilation">The compilation to generate from.</param>
    /// <param name="options">The options to generate with.</param>
    /// <returns>The <see cref="EmbeddedDocumentGeneration"/>.</returns>
    EmbeddedDocumentGeneration Generate(Compilation compilation, EmbeddedDocumentOptions options);

    /// <summary>
    /// Generates every document a recovered model is embedded as.
    /// </summary>
    /// <param name="model">The model of the whole application.</param>
    /// <param name="options">The options to generate with.</param>
    /// <param name="analyzed">Everything recovering the model reported.</param>
    /// <returns>The <see cref="EmbeddedDocumentGeneration"/>.</returns>
    EmbeddedDocumentGeneration Generate(
        ApplicationModel model,
        EmbeddedDocumentOptions options,
        IEnumerable<ScreenplayDiagnostic> analyzed);
}
