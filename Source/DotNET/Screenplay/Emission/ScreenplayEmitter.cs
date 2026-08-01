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
    /// This is where the options an emission runs with are resolved, and the only place. What a name falls back to
    /// is a question only an entry point can answer - a host that emits a model it already holds has nothing to fall
    /// back on but the domain of that model - so resolving deeper down meant resolving twice on the way through a
    /// generation, against two answers that only happen to agree.
    /// </remarks>
    public ScreenplayEmission Emit(ApplicationModel model, ScreenplayOptions options)
    {
        var diagnostics = new ScreenplayDiagnostics();
        var application = new ApplicationSyntaxBuilder(naming, diagnostics).Build(model, options.WithDefaults(model.Domain));

        return new(printer.Print(application), application, diagnostics.All);
    }
}
