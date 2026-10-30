// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.EndToEnd;

/// <summary>
/// Generates and checks one document from a loaded application.
/// </summary>
public static class DocumentCheck
{
    /// <summary>
    /// Generates, verifies, binds and checks the expectations of a document.
    /// </summary>
    /// <param name="loaded">The application to generate from.</param>
    /// <param name="mode">The document to generate and check.</param>
    /// <returns>Zero when the document passes every check, otherwise one.</returns>
    public static async Task<int> Run(LoadedApplication loaded, DocumentMode mode)
    {
        var generated = new ScreenplayGenerator().Generate(loaded.Compilations, new ScreenplayOptions { Domain = loaded.Name, AuthoringOnlyConstructs = mode.AuthoringOnlyConstructs });

        Directory.CreateDirectory(Path.GetDirectoryName(mode.Output)!);
        await File.WriteAllTextAsync(mode.Output, generated.Source);

        Console.WriteLine($"Wrote {generated.Source.Split('\n').Length} lines to '{mode.Output}'");

        foreach (var group in generated.Diagnostics.GroupBy(_ => _.Code).OrderBy(_ => _.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"  {group.Key} x{group.Count()}");
        }

        // SP0056 is non-fatal for consumers, but any occurrence is a generator regression in this CI gate.
        var errors = generated.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error || _.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ToList();
        foreach (var error in errors)
        {
            Console.WriteLine($"  generation error {error.Code}: {error.Message}");
        }

        var compiled = new ScreenplayVerifier().Verify(generated.Source);
        var rejected = compiled.Diagnostics.Where(_ => _.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error).ToList();
        foreach (var diagnostic in compiled.Diagnostics)
        {
            Console.WriteLine($"  document {diagnostic.Severity} on line {diagnostic.Location.Line}: {diagnostic.Message}");
        }

        // The document has to compile clean, warnings included. A warning the language reports is the generator writing a
        // document that refers to something it never introduces, which is a defect here rather than in the application - and
        // it is precisely the class of defect no specification built from source strings can reach. Information, such as
        // PLAY0469 describing an existing payload copy of the command's identity, is printed without rejecting valid output.
        if (rejected.Count > 0)
        {
            Console.WriteLine($"The generated document did not read back clean - {rejected.Count} diagnostic(s)");

            return 1;
        }

        var bindingErrors = compiled.UnexpectedBindingErrors(mode.AuthoringOnlyConstructs);
        foreach (var diagnostic in compiled.BindingDiagnostics)
        {
            Console.WriteLine($"  binding {diagnostic.Severity} {diagnostic.Code} in '{mode.Output}' on line {diagnostic.Location.Line}, column {diagnostic.Location.Column}: {diagnostic.Message}");
        }

        if (bindingErrors.Count > 0)
        {
            Console.WriteLine($"The generated document did not bind clean - {bindingErrors.Count} unexpected error(s)");

            return 1;
        }

        if (errors.Count > 0)
        {
            Console.WriteLine($"Generation reported {errors.Count} error(s)");

            return 1;
        }

        Console.WriteLine("The generated document compiles and has no unexpected binding errors");

        // Reading back clean proves the document is valid, not that it is true. A generator that quietly declined to read
        // something writes a smaller document that compiles just as well, so an application whose expectations are declared
        // beside it is held to what the document actually says - and to what was reported, because a value recovered and a
        // value given up on are both absent from the text and only the report tells them apart.
        if (mode.Expected is null)
        {
            return 0;
        }

        var unmet = (await Expectations.In(mode.Expected)).NotMetBy(generated.Source, generated.Diagnostics).ToList();
        foreach (var failure in unmet)
        {
            Console.WriteLine($"  expectation: {failure}");
        }

        if (unmet.Count > 0)
        {
            Console.WriteLine($"The generated document did not hold to '{mode.Expected}' - {unmet.Count} expectation(s)");

            return 1;
        }

        Console.WriteLine($"The generated document holds to every expectation in '{mode.Expected}'");

        return 0;
    }
}
