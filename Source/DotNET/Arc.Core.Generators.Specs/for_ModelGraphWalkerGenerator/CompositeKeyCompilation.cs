// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

internal static class CompositeKeyCompilation
{
    internal const string Registry = """
        namespace Cratis.Arc.Chronicle.ReadModels
        {
            public sealed record ProtectedReadModelKeyMember<T>(string Name, System.Func<T, object?> Read);
            public static class ProtectedReadModelKeyAccessors
            {
                public static void Register<T>(params ProtectedReadModelKeyMember<T>[] members) { }
                public static void DeclareKeyTypes(System.Type command, params System.Type[] keys) { }
            }
        }
        namespace Cratis.Chronicle.Keys
        {
            [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Parameter)]
            public sealed class KeyAttribute : System.Attribute { }
        }
        """;

    internal static (string Source, Diagnostic[] Diagnostics) Compile(string name, string models)
    {
        var (_, diagnostics, output) = ModelGraphWalkerCompilation.Compile(name, Registry + models, true);
        var generated = output.SyntaxTrees.FirstOrDefault(tree => tree.FilePath.EndsWith("ProtectedReadModelKeyAccessors.g.cs", StringComparison.Ordinal));

        return (generated?.ToString() ?? string.Empty, diagnostics);
    }
}
