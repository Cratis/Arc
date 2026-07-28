// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Represents an implementation of <see cref="IScreenplayGenerator"/>.
/// </summary>
/// <param name="analyzer">The <see cref="IApplicationModelAnalyzer"/> recovering the model from the source.</param>
/// <param name="emitter">The <see cref="IScreenplayEmitter"/> turning the model into a document.</param>
/// <remarks>
/// The package ships no dependency injection of its own, so a consumer that just wants to generate a document says
/// <c>new ScreenplayGenerator()</c> and gets everything wired. The constructor taking collaborators exists for
/// specifications and for hosts that want to substitute one half.
/// </remarks>
public class ScreenplayGenerator(IApplicationModelAnalyzer analyzer, IScreenplayEmitter emitter) : IScreenplayGenerator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayGenerator"/> class with everything wired up.
    /// </summary>
    /// <remarks>
    /// This is the single place the default halves are chosen, so a host that substitutes one of them changes
    /// nothing else.
    /// </remarks>
    public ScreenplayGenerator()
        : this(new ApplicationModelAnalyzer(), new ScreenplayEmitter())
    {
    }

    /// <inheritdoc/>
    public ScreenplayGenerationResult Generate(Compilation compilation, ScreenplayOptions options)
    {
        var resolved = options.WithDefaults(compilation.AssemblyName);
        var analysis = analyzer.Analyze(compilation, resolved);
        var emission = emitter.Emit(analysis.Model, resolved);

        return new(emission.Source, analysis.Model, [.. analysis.Diagnostics, .. emission.Diagnostics]);
    }
}
