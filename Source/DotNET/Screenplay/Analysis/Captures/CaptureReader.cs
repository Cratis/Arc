// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Captures;

/// <summary>
/// Recovers the stateful once-only capture of foreign events a reactor is the rendering of.
/// </summary>
/// <param name="models">The <see cref="SemanticModels"/> the reactor is read through.</param>
/// <remarks>
/// Recovery is all or nothing: the parameters of the capture are read from the reactor, the reactor is rendered
/// from them by <see cref="CaptureTemplate"/>, and only a reactor identical to its rendering is a capture. A reactor
/// that differs in any way - another comparison, another destination, an extra effect - stays an ordinary reactor.
/// </remarks>
public class CaptureReader(SemanticModels models)
{
    const string OnceOnlyAttribute = "Cratis.Chronicle.Reactors.OnceOnlyAttribute";

    static readonly SymbolDisplayFormat _qualified = SymbolDisplayFormat.FullyQualifiedFormat;

    /// <summary>
    /// Gets the diagnostics explaining why a recovered capture stays in code.
    /// </summary>
    internal ScreenplayDiagnostics Diagnostics { get; init; } = new();

    /// <summary>
    /// Gets the maximum executable model version the generated document may require.
    /// </summary>
    internal SemanticVersion? MaximumExecutableModelVersion { get; init; }

    /// <summary>
    /// Determines whether a type is declared as the reactor of a capture, which is a claim that it is one.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type carries the attributes every capture reactor carries.</returns>
    public static bool Claims(INamedTypeSymbol type) =>
        type.HasAttribute(WellKnownTypeNames.ReactorAttribute) &&
        type.HasAttribute(OnceOnlyAttribute) &&
        type.HasAttribute(WellKnownTypeNames.EventStoreAttribute);

    /// <summary>
    /// Recovers the capture a reactor is the rendering of.
    /// </summary>
    /// <param name="type">The reactor type.</param>
    /// <returns>The <see cref="CaptureModel"/>, or <see langword="null"/> when the reactor is not exactly a capture.</returns>
    public CaptureModel? Read(INamedTypeSymbol type)
    {
        var declaration = Claims(type) && type.DeclaringSyntaxReferences.Length == 1 ? type.DeclaringSyntaxReferences[0].GetSyntax() as ClassDeclarationSyntax : null;
        var shape = declaration is null ? null : ShapeOf(type, declaration);

        if (shape is null || !Tokens(declaration!).SequenceEqual(Tokens(Rendered(shape))))
        {
            return null;
        }

        if (MaximumExecutableModelVersion is { } cap && cap.CompareTo(SemanticVersion.V41) < 0)
        {
            Diagnostics.Warning(
                ScreenplayDiagnosticCodes.ReactionMetadataVersionCap,
                $"The typed capture '{type.Name}' requires ESM v41 and exceeds ScreenplayOptions.MaximumExecutableModelVersion (ESM v{cap}); the whole reactor keeps its file reference",
                type.ToDisplayString());

            return null;
        }

        return ModelOf(shape, type);
    }

    static List<string> Tokens(SyntaxNode node) => [.. node.DescendantTokens().Select(token => token.Text)];

    static ClassDeclarationSyntax Rendered(CaptureShape shape) =>
        CSharpSyntaxTree.ParseText(CaptureTemplate.Render(shape)).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();

    static CaptureModel ModelOf(CaptureShape shape, INamedTypeSymbol reactor)
    {
        var sourceName = shape.SourceType[(shape.SourceType.LastIndexOf('.') + 1)..];
        var appendedName = shape.AppendedType[(shape.AppendedType.LastIndexOf('.') + 1)..];

        return new(reactor.Name, shape.Key, shape.KeyIsEventSource, [sourceName], appendedName, [.. shape.Mappings.Select(mapping => new CaptureMappingModel(mapping.Property, mapping.Field))]);
    }

    static string? FieldOf(ExpressionSyntax expression) =>
        expression is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 3 } invocation && invocation.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax literal
            ? literal.Token.ValueText
            : null;

    static bool FromOrigin(INamedTypeSymbol source, INamedTypeSymbol reactor) =>
        source.GetAttribute(WellKnownTypeNames.EventStoreAttribute)?.GetArgument(0) is string origin &&
        reactor.GetAttribute(WellKnownTypeNames.EventStoreAttribute)?.GetArgument(0) is string declared &&
        origin == declared;

    static bool KeyedByIdentity(INamedTypeSymbol source, string key) =>
        source.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(property => property.Name.Equals(key, StringComparison.OrdinalIgnoreCase))?.Type.FindBase(WellKnownTypeNames.EventSourceIdOfT) is not null;

    static List<string>? PropertiesOf(INamedTypeSymbol source)
    {
        var properties = source.GetMembers().OfType<IPropertySymbol>().Where(property => !property.IsStatic && !property.IsImplicitlyDeclared && property.DeclaredAccessibility == Accessibility.Public).ToList();

        return properties.Count == 0 || properties.Exists(property => property.Type.SpecialType != SpecialType.System_String && property.Type.AllInterfaces.Any(_ => _.SpecialType == SpecialType.System_Collections_IEnumerable))
            ? null
            : [.. properties.Select(property => property.Name)];
    }

    static CaptureShape ShapeOf(INamedTypeSymbol type, (string Name, INamedTypeSymbol Source) handler, (List<string> Properties, string Key) captured, (INamedTypeSymbol Type, List<CaptureArgument> Arguments) appended) =>
        new(
            type.Name,
            (string)type.GetAttribute(WellKnownTypeNames.EventStoreAttribute)!.GetArgument(0)!,
            type.GetAttribute(WellKnownTypeNames.ReactorAttribute)!.GetArgument(0) as string ?? string.Empty,
            handler.Name,
            handler.Source.ToDisplayString(_qualified),
            captured.Properties,
            captured.Key,
            type.GetMembers("CaptureKeyIsNotTheEventSource").Length > 0,
            appended.Type.ToDisplayString(_qualified),
            appended.Arguments);

    static string? KeyOf(ClassDeclarationSyntax declaration) =>
        declaration.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(method => method.Identifier.Text == "Previous")?
            .DescendantNodes().OfType<LiteralExpressionSyntax>().FirstOrDefault(literal => literal.IsKind(SyntaxKind.StringLiteralExpression))?.Token.ValueText;

    static ObjectCreationExpressionSyntax? AppendedBy(ClassDeclarationSyntax declaration, string handler)
    {
        var append = declaration.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(method => method.Identifier.Text == handler)?
            .DescendantNodes().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault(creation => creation.Type.ToString().EndsWith("EventForEventSourceId", StringComparison.Ordinal));

        return append?.ArgumentList?.Arguments.Count == 2 ? append.ArgumentList.Arguments[1].Expression as ObjectCreationExpressionSyntax : null;
    }

    static List<CaptureArgument>? ArgumentsOf(ObjectCreationExpressionSyntax appended, INamedTypeSymbol appendedType)
    {
        var fields = appended.ArgumentList?.Arguments.Select(argument => FieldOf(argument.Expression)).ToList() ?? [];
        var parameters = appendedType.Constructors
            .FirstOrDefault(constructor => constructor.DeclaredAccessibility == Accessibility.Public && constructor.Parameters.Length == fields.Count)?.Parameters.ToList();

        return parameters is null || fields.Exists(field => field is null)
            ? null
            : [.. parameters.Select((parameter, index) => new CaptureArgument(parameter.Name, parameter.Type.ToDisplayString(_qualified), fields[index]!))];
    }

    CaptureShape? ShapeOf(INamedTypeSymbol type, ClassDeclarationSyntax declaration)
    {
        var handler = HandlerOf(type);
        if (handler is null)
        {
            return null;
        }

        var source = (INamedTypeSymbol)handler.Parameters[0].Type;
        var key = KeyOf(declaration);
        var appended = AppendedBy(declaration, handler.Name);
        var appendedType = TypeOf(declaration, appended);
        var properties = PropertiesOf(source);
        var arguments = appendedType is null || appended is null ? null : ArgumentsOf(appended, appendedType);

        if (key is null)
        {
            return null;
        }

        if (properties is null || !KeyedByIdentity(source, key))
        {
            return null;
        }

        return appendedType is null || arguments is null
            ? null
            : ShapeOf(type, (handler.Name, source), (properties, key), (appendedType, arguments));
    }

    INamedTypeSymbol? TypeOf(ClassDeclarationSyntax declaration, ObjectCreationExpressionSyntax? appended) =>
        appended is null ? null : models.For(declaration.SyntaxTree)?.GetSymbolInfo(appended).Symbol?.ContainingType;

    static IMethodSymbol? HandlerOf(INamedTypeSymbol type)
    {
        var handlers = type.GetMembers().OfType<IMethodSymbol>().Where(method => method.MethodKind == MethodKind.Ordinary && method.DeclaredAccessibility == Accessibility.Public).ToList();

        return handlers is [{ Parameters: [{ Type: INamedTypeSymbol source }, _, _] } handler] && FromOrigin(source, type)
            ? handler : null;
    }
}
