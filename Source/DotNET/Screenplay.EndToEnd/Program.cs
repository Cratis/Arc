// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.EndToEnd;

const string AuthoringFlag = "--authoring-only-constructs";
const string AuthoringOutputFlag = "--authoring-output";
var authoringOnlyConstructs = args.Contains(AuthoringFlag, StringComparer.Ordinal);
var authoringOutputIndex = Array.IndexOf(args, AuthoringOutputFlag);
var hasAuthoringOutput = authoringOutputIndex >= 0;
var missingAuthoringOutput = hasAuthoringOutput && authoringOutputIndex == args.Length - 1;
var positional = args.Where((arg, index) => arg != AuthoringFlag &&
    (!hasAuthoringOutput || (index != authoringOutputIndex && index != authoringOutputIndex + 1))).ToArray();

if (positional.Length < 2 || missingAuthoringOutput || (hasAuthoringOutput && authoringOnlyConstructs))
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  Cratis.Arc.Screenplay.EndToEnd <project-or-solution> <output-file> [expectations-file] [--authoring-only-constructs] [--authoring-output <file>]");
    Console.WriteLine("  When both documents are checked, expectations apply to the default document only.");

    return 2;
}

var project = Path.GetFullPath(positional[0]);
var output = Path.GetFullPath(positional[1]);
var expected = positional.Length > 2 ? Path.GetFullPath(positional[2]) : null;
var modes = new List<DocumentMode> { new(authoringOnlyConstructs ? "authoring" : "default", output, authoringOnlyConstructs, expected) };
if (hasAuthoringOutput)
{
    modes.Add(new("authoring", Path.GetFullPath(args[authoringOutputIndex + 1]), true, null));
}

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

var passed = 0;
foreach (var mode in modes)
{
    Console.WriteLine($"Checking the {mode.Name} document -> '{mode.Output}'");
    var result = await DocumentCheck.Run(loaded, mode);
    Console.WriteLine(result == 0 ? $"The {mode.Name} document passed" : $"The {mode.Name} document FAILED");
    if (result == 0)
    {
        passed++;
    }
}

var failed = modes.Count - passed;
Console.WriteLine($"Checked {modes.Count} document(s): {passed} passed, {failed} failed");

return failed > 0 ? 1 : 0;
