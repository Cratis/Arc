// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay;
using Cratis.Arc.Screenplay.EndToEnd;
using Cratis.Screenplay;

if (args.Length < 2)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  Cratis.Arc.Screenplay.EndToEnd <project-or-solution> <output-file> [expectations-file]");

    return 2;
}

var project = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var expected = args.Length > 2 ? Path.GetFullPath(args[2]) : null;

Console.WriteLine($"Generating the Screenplay document of '{project}'");

var failures = new List<string>();
var loaded = await ProjectCompilation.Of(project, failures);
var compilations = loaded.Compilations;

foreach (var failure in failures)
{
    Console.WriteLine($"  workspace: {failure}");
}

if (compilations.Count == 0)
{
    Console.WriteLine($"'{project}' yielded no compilation, so there is nothing to generate from");

    return 1;
}

foreach (var compilation in compilations)
{
    Console.WriteLine($"  project: {compilation.AssemblyName}");
}

var generated = new ScreenplayGenerator().Generate(compilations, new ScreenplayOptions { Domain = loaded.Name });

Directory.CreateDirectory(Path.GetDirectoryName(output)!);
await File.WriteAllTextAsync(output, generated.Source);

Console.WriteLine($"Wrote {generated.Source.Split('\n').Length} lines to '{output}'");

foreach (var group in generated.Diagnostics.GroupBy(_ => _.Code).OrderBy(_ => _.Key, StringComparer.Ordinal))
{
    Console.WriteLine($"  {group.Key} x{group.Count()}");
}

var errors = generated.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ToList();
foreach (var error in errors)
{
    Console.WriteLine($"  generation error {error.Code}: {error.Message}");
}

var compiled = new ScreenplayCompiler().Compile(generated.Source);
var rejected = compiled.Diagnostics.ToList();
foreach (var diagnostic in rejected)
{
    Console.WriteLine($"  document {diagnostic.Severity} on line {diagnostic.Location.Line}: {diagnostic.Message}");
}

// The document has to compile clean, warnings included. A warning the language reports is the generator writing a
// document that refers to something it never introduces, which is a defect here rather than in the application - and
// it is precisely the class of defect no specification built from source strings can reach.
if (rejected.Count > 0)
{
    Console.WriteLine($"The generated document did not read back clean - {rejected.Count} diagnostic(s)");

    return 1;
}

if (errors.Count > 0)
{
    Console.WriteLine($"Generation reported {errors.Count} error(s)");

    return 1;
}

Console.WriteLine("The generated document reads back clean");

// Reading back clean proves the document is valid, not that it is true. A generator that quietly declined to read
// something writes a smaller document that compiles just as well, so an application whose expectations are declared
// beside it is held to what the document actually says - and to what was reported, because a value recovered and a
// value given up on are both absent from the text and only the report tells them apart.
if (expected is null)
{
    return 0;
}

var unmet = (await Expectations.In(expected)).NotMetBy(generated.Source, generated.Diagnostics).ToList();
foreach (var failure in unmet)
{
    Console.WriteLine($"  expectation: {failure}");
}

if (unmet.Count > 0)
{
    Console.WriteLine($"The generated document did not hold to '{expected}' - {unmet.Count} expectation(s)");

    return 1;
}

Console.WriteLine($"The generated document holds to every expectation in '{expected}'");

return 0;
