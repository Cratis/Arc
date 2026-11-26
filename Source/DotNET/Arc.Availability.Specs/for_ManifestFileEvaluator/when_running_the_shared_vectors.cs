// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Availability.ManifestFile;

namespace Cratis.Arc.Availability.for_ManifestFileEvaluator;

public class when_running_the_shared_vectors : Specification
{
    readonly List<string> _failures = [];
    int _files;
    int _cases;

    async Task Because()
    {
        foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "vectors"), "*.json"))
        {
            _files++;
            using var vectors = JsonDocument.Parse(await File.ReadAllTextAsync(file));
            var root = vectors.RootElement;
            var document = ManifestFileDocument.Parse(root.GetProperty("manifest").GetRawText());
            var evaluator = new ManifestFileEvaluator(document);
            foreach (var vector in root.GetProperty("cases").EnumerateArray())
            {
                _cases++;
                var name = $"{Path.GetFileName(file)}: {vector.GetProperty("name").GetString()}";
                var decision = await evaluator.Decide(vector.GetProperty("part").GetString()!, ContextOf(vector));
                var expect = vector.GetProperty("expect");
                var expectedAvailable = expect.GetProperty("available").GetBoolean();
                var expectedReason = expect.TryGetProperty("reason", out var reason) ? reason.GetString() : null;
                if (decision.IsAvailable != expectedAvailable || (!expectedAvailable && decision.Reason != expectedReason))
                {
                    _failures.Add($"{name} expected {(expectedAvailable ? "available" : expectedReason)} but was {(decision.IsAvailable ? "available" : decision.Reason)}");
                }
            }
        }
    }

    [Fact] void should_find_the_vectors() => _files.ShouldBeGreaterThan(0);
    [Fact] void should_run_cases() => _cases.ShouldBeGreaterThan(30);
    [Fact] void should_agree_with_every_vector() => _failures.ShouldBeEmpty();

    static ManifestContext ContextOf(JsonElement vector)
    {
        var context = vector.GetProperty("context");
        return new(
            Text(context, "tenant"),
            Text(context, "user"),
            Strings(context, "audiences"),
            Strings(context, "suspended"),
            context.TryGetProperty("values", out var values) ? Object(values) : new Dictionary<string, object?>(),
            vector.TryGetProperty("arguments", out var arguments) ? Object(arguments) : null);
    }

    static string Text(JsonElement element, string name) => element.TryGetProperty(name, out var value) ? value.GetString()! : string.Empty;

    static List<string> Strings(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? [.. value.EnumerateArray().Select(_ => _.GetString()!)] : [];

    static Dictionary<string, object?> Object(JsonElement element) =>
        element.EnumerateObject().ToDictionary(_ => _.Name, _ => Value(_.Value));

    static object? Value(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var integer) ? (object)integer : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Object => Object(element),
        _ => null
    };
}
