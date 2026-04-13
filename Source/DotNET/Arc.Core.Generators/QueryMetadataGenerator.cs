// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Generators;

/// <summary>
/// Incremental source generator that generates compile-time query metadata for read models.
/// </summary>
/// <remarks>
/// For each assembly that contains types decorated with <c>[ReadModel]</c>, this generator
/// produces a class implementing <c>IQueryMetadata</c>. The class maps fully-qualified query
/// names to their read model <c>Type</c>. A <c>[ModuleInitializer]</c> on the generated class
/// registers it with <c>QueryMetadataRegistry</c> at assembly-load time, enabling AOT-safe
/// query lookup without runtime type scanning.
/// </remarks>
[Generator]
public class QueryMetadataGenerator : IIncrementalGenerator
{
    const string ReadModelAttributeFullName = "Cratis.Arc.Queries.ModelBound.ReadModelAttribute";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var readModelProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is TypeDeclarationSyntax typeDecl && typeDecl.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetReadModelInfo(ctx))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!)
            .Collect();

        context.RegisterSourceOutput(readModelProvider, static (spc, readModels) =>
            GenerateSource(spc, readModels));
    }

    static ReadModelInfo? GetReadModelInfo(GeneratorSyntaxContext context)
    {
        var typeDecl = (TypeDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(typeDecl) is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        if (!HasReadModelAttribute(typeSymbol))
        {
            return null;
        }

        var queryMethods = typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m =>
                m.MethodKind == MethodKind.Ordinary &&
                m.IsStatic &&
                m.DeclaredAccessibility == Accessibility.Public &&
                IsValidQueryMethod(m, typeSymbol))
            .Select(m => m.Name)
            .ToList();

        if (queryMethods.Count == 0)
        {
            return null;
        }

        return new ReadModelInfo(
            typeSymbol.ToDisplayString(),
            queryMethods);
    }

    static bool HasReadModelAttribute(INamedTypeSymbol typeSymbol) =>
        typeSymbol.GetAttributes().Any(a =>
            string.Equals(a.AttributeClass?.ToDisplayString(), ReadModelAttributeFullName, StringComparison.Ordinal));

    static bool IsValidQueryMethod(IMethodSymbol method, INamedTypeSymbol readModelType)
    {
        var returnType = method.ReturnType;

        // Unwrap Task<T>
        if (returnType is INamedTypeSymbol taskType &&
            string.Equals(taskType.Name, "Task", StringComparison.Ordinal) &&
            taskType.TypeArguments.Length == 1 &&
            string.Equals(taskType.ContainingNamespace.ToDisplayString(), "System.Threading.Tasks", StringComparison.Ordinal))
        {
            returnType = taskType.TypeArguments[0];
        }

        if (SymbolEqualityComparer.Default.Equals(returnType, readModelType))
        {
            return true;
        }

        if (IsCollectionOfType(returnType, readModelType))
        {
            return true;
        }

        if (returnType is INamedTypeSymbol asyncEnumerable &&
            string.Equals(asyncEnumerable.Name, "IAsyncEnumerable", StringComparison.Ordinal) &&
            asyncEnumerable.TypeArguments.Length == 1 &&
            SymbolEqualityComparer.Default.Equals(asyncEnumerable.TypeArguments[0], readModelType))
        {
            return true;
        }

        if (returnType is INamedTypeSymbol subject &&
            string.Equals(subject.Name, "ISubject", StringComparison.Ordinal) &&
            subject.TypeArguments.Length == 1)
        {
            var subjectArg = subject.TypeArguments[0];
            if (SymbolEqualityComparer.Default.Equals(subjectArg, readModelType) ||
                IsCollectionOfType(subjectArg, readModelType))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsCollectionOfType(ITypeSymbol type, INamedTypeSymbol elementType)
    {
        if (type is IArrayTypeSymbol arrayType)
        {
            return SymbolEqualityComparer.Default.Equals(arrayType.ElementType, elementType);
        }

        if (type is INamedTypeSymbol namedType)
        {
            if (string.Equals(namedType.Name, "IEnumerable", StringComparison.Ordinal) &&
                namedType.TypeArguments.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(namedType.TypeArguments[0], elementType))
            {
                return true;
            }

            return namedType.AllInterfaces.Any(i =>
                string.Equals(i.Name, "IEnumerable", StringComparison.Ordinal) &&
                i.TypeArguments.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], elementType));
        }

        return false;
    }

    static void GenerateSource(SourceProductionContext context, ImmutableArray<ReadModelInfo> readModels)
    {
        if (readModels.IsEmpty)
        {
            return;
        }

        var sb = new StringBuilder()
            .AppendLine("// <auto-generated/>")
            .AppendLine("#pragma warning disable")
            .AppendLine("using System;")
            .AppendLine("using System.Collections.Generic;")
            .AppendLine("using System.Runtime.CompilerServices;")
            .AppendLine("using Cratis.Arc.Queries.ModelBound;")
            .AppendLine()
            .AppendLine("namespace Cratis.Arc.Queries.ModelBound.Generated;")
            .AppendLine()
            .AppendLine("/// <summary>")
            .AppendLine("/// Compile-time generated query metadata. Do not modify.")
            .AppendLine("/// </summary>")
            .AppendLine("internal sealed class GeneratedQueryMetadata : IQueryMetadata")
            .AppendLine("{")
            .AppendLine("    static readonly Dictionary<string, Type> _queries = new()")
            .AppendLine("    {");

        foreach (var readModel in readModels)
        {
            foreach (var method in readModel.QueryMethodNames)
            {
                sb.Append("        [\"")
                  .Append(readModel.FullyQualifiedTypeName)
                  .Append('.')
                  .Append(method)
                  .Append("\"] = typeof(global::")
                  .Append(readModel.FullyQualifiedTypeName)
                  .AppendLine("),");
            }
        }

        sb.AppendLine("    };")
            .AppendLine()
            .AppendLine("    /// <inheritdoc/>")
            .AppendLine("    public IReadOnlyDictionary<string, Type> Queries => _queries;")
            .AppendLine()
            .AppendLine("    /// <summary>")
            .AppendLine("    /// Registers this metadata with <see cref=\"QueryMetadataRegistry\"/> at module load time.")
            .AppendLine("    /// </summary>")
            .AppendLine("    [ModuleInitializer]")
            .AppendLine("    internal static void Initialize() =>")
            .AppendLine("        QueryMetadataRegistry.Register(new GeneratedQueryMetadata());")
            .AppendLine("}");

        context.AddSource("GeneratedQueryMetadata.g.cs", sb.ToString());
    }
}
