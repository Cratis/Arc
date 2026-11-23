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
        var assembly = specification.Assembly.Location;
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!Directory.Exists(Path.Combine(root.FullName, ".git")) && !File.Exists(Path.Combine(root.FullName, ".git"))) root = root.Parent!;
        var directory = Path.Combine(root.FullName, ".ai-work", "3954", "specs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
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
            throw;
        }
        if (process.ExitCode != 0) throw new InvalidOperationException($"Isolated switch-off spec failed: {await stdout}\n{await stderr}");
    }
}
#endif
