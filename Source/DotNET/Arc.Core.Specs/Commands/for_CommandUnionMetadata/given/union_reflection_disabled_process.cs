// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Cratis.Arc.Commands.for_CommandUnionMetadata.given;

public static class union_reflection_disabled_process
{
    public static async Task Run(Type specification)
    {
        // The feature switch is cached at type initialization. A child avoids changing switch state in parallel specs.
        var directory = Directory.CreateTempSubdirectory("cratis-arc-union-");
        try
        {
            await RunIsolated(specification, directory.FullName);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    static async Task RunIsolated(Type specification, string directory)
    {
        var assembly = specification.Assembly.Location;
        var config = JsonNode.Parse(await File.ReadAllTextAsync(Path.ChangeExtension(assembly, ".runtimeconfig.json")))!;
        var options = config["runtimeOptions"]!;
        options["configProperties"] ??= new JsonObject();
        options["configProperties"]![CommandUnionFeatureSwitches.ReflectionSupportName] = false;
        var configPath = Path.Combine(directory, "reflection-disabled.runtimeconfig.json");
        await File.WriteAllTextAsync(configPath, config.ToJsonString());
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "exec", "--runtimeconfig", configPath, assembly, "-class", specification.FullName!, "-noLogo", "-noColor" }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            throw;
        }
        if (process.ExitCode != 0) throw new InvalidOperationException($"Isolated switch-off spec failed: {await stdout}\n{await stderr}");
    }
}
#endif
