// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Generators;

/// <summary>
/// Incremental source generator that generates compile-time query metadata for read models.
/// </summary>
/// <remarks>
/// For each assembly that contains types decorated with <c>[ReadModel]</c>, this generator
/// produces a generated initializer that registers fully-qualified query names with their read
/// model <c>Type</c> in <c>QueryMetadataRegistry</c> at assembly-load time, enabling AOT-safe
/// query lookup without runtime type scanning.
/// </remarks>
[Generator]
public class QueryMetadataGenerator : IIncrementalGenerator
{
    const string ReadModelAttributeFullName = "Cratis.Arc.Queries.ModelBound.ReadModelAttribute";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var declarations = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is TypeDeclarationSyntax,
            static (syntax, _) => syntax.SemanticModel.GetDeclaredSymbol(syntax.Node) as INamedTypeSymbol)
            .Where(static type => type is not null).Collect();
        context.RegisterSourceOutput(declarations, static (output, types) =>
        {
            foreach (var type in types.OfType<INamedTypeSymbol>().Distinct(SymbolEqualityComparer.Default).OfType<INamedTypeSymbol>())
            {
                foreach (var diagnostic in ModelBoundQueryMethodSymbols.Validate(type, method => IsValidQueryMethod(method, type)))
                {
                    output.ReportDiagnostic(diagnostic);
                }
            }
        });

        var readModelProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, _) => GetReadModelInfo(ctx))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!)
            .Collect();

        context.RegisterSourceOutput(readModelProvider, static (spc, readModels) =>
            GenerateSource(spc, readModels));
    }

    /// <summary>
    /// Determines whether a method returns what a query of a read model returns.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="readModelType">The read model type.</param>
    /// <returns>Whether the method can be a query of the read model.</returns>
    internal static bool IsValidQueryMethod(IMethodSymbol method, INamedTypeSymbol readModelType)
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

    static AnnotatedReadModel? GetReadModelInfo(GeneratorSyntaxContext context)
    {
        var typeDecl = (TypeDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(typeDecl) is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        if (!HasReadModelAttribute(typeSymbol) && !ModelBoundQueryMethodSymbols.RequiresQueryAttribute(typeSymbol))
        {
            return null;
        }

        var queryMethods = ModelBoundQueryMethodSymbols.GetMethods(
                typeSymbol,
                m => ModelBoundQueryMethodSymbols.IsCandidate(m) && IsValidQueryMethod(m, typeSymbol))
            .ToList();

        if (queryMethods.Count == 0)
        {
            return null;
        }

        return new AnnotatedReadModel(
            new ReadModelInfo(typeSymbol.ToDisplayString(), queryMethods.ConvertAll(method => method.Name)),
            string.Join("\n", queryMethods.Select(method => DataAnnotationMemberTypes.ForQuery(typeSymbol, method, context.SemanticModel.Compilation)).Where(registration => registration.Length > 0)));
    }

    static bool HasReadModelAttribute(INamedTypeSymbol typeSymbol) =>
        typeSymbol.GetAttributes().Any(a =>
            string.Equals(a.AttributeClass?.ToDisplayString(), ReadModelAttributeFullName, StringComparison.Ordinal));

    static void GenerateSource(SourceProductionContext context, ImmutableArray<AnnotatedReadModel> readModels)
    {
        context.AddSource("CratisArcGeneratedMarker.g.cs", GenerateMarkerType());

        if (readModels.IsEmpty)
        {
            return;
        }

        var sb = new StringBuilder()
            .AppendLine("// <auto-generated/>")
            .AppendLine("#pragma warning disable")
            .AppendLine("#nullable enable")
            .AppendLine("using System;")
            .AppendLine("using System.Runtime.CompilerServices;")
            .AppendLine("using Cratis.Arc.Queries.ModelBound;")
            .AppendLine()
            .AppendLine("namespace Cratis.Arc.Queries.ModelBound.Generated;")
            .AppendLine()
            .AppendLine("/// <summary>")
            .AppendLine("/// Compile-time generated query metadata registration. Do not modify.")
            .AppendLine("/// </summary>")
            .AppendLine("internal static class GeneratedQueryMetadata")
            .AppendLine("{")
            .AppendLine("    [ModuleInitializer]")
            .AppendLine("    internal static void Initialize()")
            .AppendLine("    {");

        foreach (var readModel in readModels.GroupBy(_ => _.Metadata.FullyQualifiedTypeName).Select(_ => _.First()))
        {
            foreach (var method in readModel.Metadata.QueryMethodNames)
            {
                sb.Append("        QueryMetadataRegistry.Register(\"")
                  .Append(readModel.Metadata.FullyQualifiedTypeName)
                  .Append('.')
                  .Append(method)
                  .Append("\", typeof(global::")
                  .Append(readModel.Metadata.FullyQualifiedTypeName)
                  .AppendLine("));");
            }

            if (readModel.Annotations.Length > 0)
            {
                sb.AppendLine(readModel.Annotations);
            }
        }

        sb.AppendLine("    }")
            .AppendLine("}");

        context.AddSource("GeneratedQueryMetadata.g.cs", sb.ToString());
    }

    static string GenerateMarkerType() => new StringBuilder()
        .AppendLine("// <auto-generated/>")
        .AppendLine("#pragma warning disable")
        .AppendLine()
        .AppendLine("namespace Cratis.Arc.Generated;")
        .AppendLine()
        .AppendLine("/// <summary>")
        .AppendLine("/// Marker type that confirms Arc source generators have run for the assembly.")
        .AppendLine("/// </summary>")
        .AppendLine("public static class GeneratedMarker")
        .AppendLine("{")
        .AppendLine("}")
        .ToString();

    sealed class AnnotatedReadModel(ReadModelInfo metadata, string annotations)
    {
        internal ReadModelInfo Metadata { get; } = metadata;
        internal string Annotations { get; } = annotations;
    }
}
