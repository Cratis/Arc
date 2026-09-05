// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Printing;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// Represents an implementation of <see cref="IScreenplayEmitter"/>.
/// </summary>
/// <param name="printer">The <see cref="IScreenplayPrinter"/> that prints the document.</param>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
public class ScreenplayEmitter(IScreenplayPrinter printer, IScreenplayNaming naming) : IScreenplayEmitter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayEmitter"/> class using the standard printer and naming.
    /// </summary>
    public ScreenplayEmitter()
        : this(new ScreenplayPrinter(), new ScreenplayNaming())
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Emitting is an entry point of its own, so the options are resolved here for a host that hands over a model it
    /// already holds - such a host has nothing to fall back on but the domain of that model. A generation is the
    /// other entry point and has already resolved against the assembly it was reading, and options that are resolved
    /// answer with themselves, so one generation still resolves exactly once and the fallback the analysis half used
    /// is the fallback the document is named after.
    /// </remarks>
    public ScreenplayEmission Emit(ApplicationModel model, ScreenplayOptions options)
    {
        var diagnostics = new ScreenplayDiagnostics();
        var application = new ApplicationSyntaxBuilder(naming, diagnostics).Build(model, options.WithDefaults(model.Domain));

        return new(printer.Print(application), application, diagnostics.All);
    }
}
