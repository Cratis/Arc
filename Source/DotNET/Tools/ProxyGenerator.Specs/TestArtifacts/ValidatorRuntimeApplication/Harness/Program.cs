// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator;

namespace ValidatorRuntimeHarness;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var errors = new List<string>();
        var result = await Generator.Generate(args[0], args[2], 0, Console.WriteLine, errors.Add, skipOutputDeletion: true);
        if (result || !errors.Exists(_ => _.Contains("Fixture validator activation failed", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Public generation did not fail closed: {string.Join(';', errors)}");
        }

        Console.WriteLine("public API returned false with the activation cause");
        VerifyReleased("failure");
        VerifyExtractionFailure(args[0]);
        await VerifySuccessfulGeneration(args[1], Path.Combine(args[2], "valid-output"));
        await VerifySuccessfulGeneration(args[1], Path.Combine(args[2], "repeat-output"));
        return 0;
    }

    static void VerifyExtractionFailure(string assemblyPath)
    {
        var assembly = System.Reflection.Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("ValidatorRuntimeApplication.CreateApplication")!;
        try
        {
            _ = ValidationRulesExtractor.ExtractValidationRules(assembly, type).ToArray();
        }
        catch (Exception exception) when (exception.GetType().Name == "ValidatorRuntimeCouldNotBeLoaded")
        {
            Console.WriteLine("extractor failed closed");
            return;
        }

        throw new InvalidOperationException("Extractor returned empty rules after activation failure");
    }

    static async Task VerifySuccessfulGeneration(string assemblyPath, string outputPath)
    {
        var result = await Generator.Generate(assemblyPath, outputPath, 0, Console.WriteLine, Console.Error.WriteLine);
        if (!result || Directory.GetFiles(outputPath, "*.ts", SearchOption.AllDirectories).Length == 0)
        {
            throw new InvalidOperationException("A valid generation after failure did not produce proxies");
        }

        Console.WriteLine("generated successfully after failure");
        VerifyReleased("success");
    }

    static void VerifyReleased(string phase)
    {
        if (TypeExtensions.Assemblies.Any()) throw new InvalidOperationException($"Metadata context retained after {phase}");
        Console.WriteLine($"metadata context released after {phase}");
    }
}
