// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator;
using TypeExtensions = Cratis.Arc.ProxyGenerator.TypeExtensions;

namespace ValidatorRuntimeHarness;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length > 3 && args[3] == "optional")
        {
            VerifyOptionalLookup(args[0]);
        }
        else
        {
            await VerifyGenerationFailure(args[0], args[2]);
            VerifyReleased("failure");
            VerifyExtractionFailure(args[0]);
        }

        await VerifySuccessfulGeneration(args[1], Path.Combine(args[2], "valid-output"));
        await VerifySuccessfulGeneration(args[1], Path.Combine(args[2], "repeat-output"));
        return 0;
    }

    static async Task VerifyGenerationFailure(string assemblyPath, string outputPath)
    {
        try
        {
            await Generator.Generate(assemblyPath, outputPath, 0, Console.WriteLine, Console.Error.WriteLine);
        }
        catch (Exception exception) when (exception.GetType().Name == "ValidationRulesCouldNotBeExtracted" &&
            exception.Message.Contains("Fixture validator activation failed", StringComparison.Ordinal))
        {
            Console.WriteLine("public API threw with the activation cause");
            return;
        }

        throw new InvalidOperationException("Public generation did not preserve its validation failure contract");
    }

    static void VerifyExtractionFailure(string assemblyPath)
    {
        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("ValidatorRuntimeApplication.CreateApplication")!;
        try
        {
            _ = ValidationRulesExtractor.ExtractValidationRules(assembly, type).ToArray();
        }
        catch (Exception exception) when (exception.GetType().Name == "ValidationRulesCouldNotBeExtracted")
        {
            Console.WriteLine("extractor failed closed");
            return;
        }

        throw new InvalidOperationException("Extractor returned empty rules after activation failure");
    }

    static void VerifyOptionalLookup(string assemblyPath)
    {
        var runtimeAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        var applicationAssemblies = Directory.GetFiles(Path.GetDirectoryName(assemblyPath)!, "*.dll");
        using var context = new MetadataLoadContext(new PathAssemblyResolver(runtimeAssemblies.Concat(applicationAssemblies)));
        var assembly = context.LoadFromAssemblyPath(assemblyPath);
        var type = assembly.GetType("ValidatorRuntimeApplication.CreateApplication")!;
        File.Delete(assemblyPath);
        if (!assembly.ReflectionOnly || RuntimeValidatorAssemblies.For(assembly) is not null ||
            !ReferenceEquals(RuntimeValidatorAssemblies.For(type), type))
        {
            throw new InvalidOperationException("Optional runtime lookup did not preserve its fallback contract");
        }

        Console.WriteLine("optional runtime lookup preserved its fallback contract");
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
