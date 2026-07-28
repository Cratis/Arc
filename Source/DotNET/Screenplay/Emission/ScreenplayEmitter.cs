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
    public ScreenplayEmission Emit(ApplicationModel model, ScreenplayOptions options)
    {
        var diagnostics = new ScreenplayDiagnostics();
        var application = new ApplicationSyntaxBuilder(naming, diagnostics).Build(model, options);

        return new(printer.Print(application), application, diagnostics.All);
    }
}
