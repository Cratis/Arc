// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_CommandReadModelKeyGeneration;

internal static class CommandKeyCompilation
{
    internal const string Registry = """
        namespace Cratis.Arc.Chronicle.ReadModels
        {
            public sealed record CommandReadModelKeyProperty(string Name, System.Type PropertyType, System.Func<object, object?> Read);
            public static class CommandReadModelKeys
            {
                public static readonly System.Collections.Generic.Dictionary<System.Type, CommandReadModelKeyProperty[]> Commands = new();
                public static readonly System.Collections.Generic.Dictionary<System.Type, System.Type?> Models = new();
                public static void RegisterCommand(System.Type command, params CommandReadModelKeyProperty[] properties) => Commands[command] = properties;
                public static void DeclareModelKey(System.Type model, System.Type? keyType) => Models[model] = keyType;
            }
        }
        """;

    internal static (string Source, Diagnostic[] Diagnostics, Compilation Output) Compile(string name, string models)
    {
        var (_, diagnostics, output) = ModelGraphWalkerCompilation.Compile(name, Registry + models, true);
        var generated = output.SyntaxTrees.FirstOrDefault(tree => tree.FilePath.EndsWith("CommandReadModelKeys.g.cs", StringComparison.Ordinal));

        return (generated?.ToString() ?? string.Empty, diagnostics, output);
    }

    internal static IDictionary Registered(Assembly assembly, string field) =>
        (IDictionary)assembly.GetType("Cratis.Arc.Chronicle.ReadModels.CommandReadModelKeys")!.GetField(field)!.GetValue(null)!;
}
