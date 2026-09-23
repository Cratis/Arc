// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Analyzer that reports authorization attributes whose effect on an Arc artifact is not what they say.
/// </summary>
/// <remarks>
/// <para>
/// ARC0019 reports <c>[AllowAnonymous]</c> declared together with <c>[Authorize]</c> or <c>[Roles]</c> on one
/// declaration. The two contradict each other, and Arc resolves the contradiction differently depending on which
/// of its anonymous evaluators is asked first — throwing in one order and admitting anonymous callers in the other.
/// </para>
/// <para>
/// ARC0020 reports an ASP.NET Core authorization attribute on a model-bound command or read model in a project that
/// does not use Arc's ASP.NET Core integration. That integration is what enforces the ASP.NET Core attributes; without
/// it they are read by nothing.
/// </para>
/// <para>
/// ARC0021 reports a <c>Policy</c> or <c>AuthenticationSchemes</c> on an authorization attribute of a model-bound
/// command or read model. Arc enforces authentication and roles; it does not evaluate either of these, so the
/// artifact is less protected than the attribute reads.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AuthorizationAttributeAnalyzer : DiagnosticAnalyzer
{
    const string ArcAllowAnonymous = "Cratis.Arc.Authorization.AllowAnonymousAttribute";
    const string ArcAuthorize = "Cratis.Arc.Authorization.AuthorizeAttribute";
    const string AspNetAllowAnonymous = "Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute";
    const string AspNetAuthorize = "Microsoft.AspNetCore.Authorization.AuthorizeAttribute";
    const string CommandAttribute = "Cratis.Arc.Commands.ModelBound.CommandAttribute";
    const string ReadModelAttribute = "Cratis.Arc.Queries.ModelBound.ReadModelAttribute";
    const string AspNetIntegration = "Cratis.Arc.Authorization.AspNetAnonymousEvaluator";
    const string PolicyProperty = "Policy";
    const string AuthenticationSchemesProperty = "AuthenticationSchemes";

    enum AuthorizationKind
    {
        None = 0,
        ArcAllowAnonymous = 1,
        ArcAuthorize = 2,
        AspNetAllowAnonymous = 3,
        AspNetAuthorize = 4
    }

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.ARC0019_ConflictingAuthorizationOnDeclaration,
        DiagnosticDescriptors.ARC0020_AspNetAuthorizationAttributeOnModelBoundArtifact,
        DiagnosticDescriptors.ARC0021_AuthorizationSettingNotEvaluated
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var aspNetIntegrationPresent = start.Compilation.GetTypeByMetadataName(AspNetIntegration) is not null;
            start.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, aspNetIntegrationPresent), SymbolKind.NamedType, SymbolKind.Method);
        });
    }

    static void AnalyzeSymbol(SymbolAnalysisContext context, bool aspNetIntegrationPresent)
    {
        var classified = context.Symbol.GetAttributes()
            .Select(attribute => (Attribute: attribute, Kind: Classify(attribute.AttributeClass)))
            .Where(_ => _.Kind != AuthorizationKind.None)
            .ToList();

        if (classified.Count == 0)
        {
            return;
        }

        var isModelBound = IsModelBoundArtifactOrMember(context.Symbol);

        ReportConflict(context, classified, isModelBound);

        if (!isModelBound)
        {
            return;
        }

        if (!aspNetIntegrationPresent)
        {
            ReportIgnoredAspNetAttributes(context, classified);
        }

        ReportUnevaluatedSettings(context, classified);
    }

    static void ReportUnevaluatedSettings(
        SymbolAnalysisContext context,
        List<(AttributeData Attribute, AuthorizationKind Kind)> classified)
    {
        foreach (var (attribute, _) in classified.Where(_ => _.Kind is AuthorizationKind.ArcAuthorize or AuthorizationKind.AspNetAuthorize))
        {
            var name = attribute.AttributeClass!.Name.Replace("Attribute", string.Empty);
            foreach (var setting in UnevaluatedSettingsOf(attribute))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ARC0021_AuthorizationSettingNotEvaluated,
                    LocationOf(attribute, context.Symbol),
                    name,
                    context.Symbol.Name,
                    setting));
            }
        }
    }

    static IEnumerable<string> UnevaluatedSettingsOf(AttributeData attribute)
    {
        // ASP.NET Core's [Authorize("PolicyName")] carries the policy as its single constructor argument.
        // [Roles] takes a params array, and reading Value on an array constant throws, so only scalar
        // constants are inspected.
        var policyFromConstructor = attribute.ConstructorArguments.Length == 1 &&
            IsNonEmptyString(attribute.ConstructorArguments[0]) &&
            string.Equals(attribute.AttributeClass?.ToDisplayString(), AspNetAuthorize, StringComparison.Ordinal);

        var named = attribute.NamedArguments
            .Where(argument => IsNonEmptyString(argument.Value))
            .Select(argument => argument.Key)
            .ToList();

        if (policyFromConstructor || named.Contains(PolicyProperty))
        {
            yield return PolicyProperty;
        }

        if (named.Contains(AuthenticationSchemesProperty))
        {
            yield return AuthenticationSchemesProperty;
        }
    }

    static void ReportConflict(
        SymbolAnalysisContext context,
        List<(AttributeData Attribute, AuthorizationKind Kind)> classified,
        bool isModelBound)
    {
        var allowAnonymous = classified.Where(_ => _.Kind is AuthorizationKind.ArcAllowAnonymous or AuthorizationKind.AspNetAllowAnonymous).ToList();
        var restricting = classified.Where(_ => _.Kind is AuthorizationKind.ArcAuthorize or AuthorizationKind.AspNetAuthorize).ToList();

        if (allowAnonymous.Count == 0 || restricting.Count == 0)
        {
            return;
        }

        // A pure ASP.NET Core declaration on an MVC controller is MVC's business, not Arc's.
        var involvesArc = isModelBound || classified.Exists(_ => _.Kind is AuthorizationKind.ArcAllowAnonymous or AuthorizationKind.ArcAuthorize);
        if (!involvesArc)
        {
            return;
        }

        foreach (var (attribute, _) in allowAnonymous)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARC0019_ConflictingAuthorizationOnDeclaration,
                LocationOf(attribute, context.Symbol),
                context.Symbol.Name,
                restricting[0].Attribute.AttributeClass!.Name.Replace("Attribute", string.Empty)));
        }
    }

    static void ReportIgnoredAspNetAttributes(
        SymbolAnalysisContext context,
        List<(AttributeData Attribute, AuthorizationKind Kind)> classified)
    {
        foreach (var (attribute, kind) in classified.Where(_ => _.Kind is AuthorizationKind.AspNetAllowAnonymous or AuthorizationKind.AspNetAuthorize))
        {
            var name = attribute.AttributeClass!.Name.Replace("Attribute", string.Empty);
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARC0020_AspNetAuthorizationAttributeOnModelBoundArtifact,
                LocationOf(attribute, context.Symbol),
                name,
                context.Symbol.Name,
                kind == AuthorizationKind.AspNetAllowAnonymous ? "AllowAnonymous" : "Authorize"));
        }
    }

    static AuthorizationKind Classify(INamedTypeSymbol? attributeClass)
    {
        for (var current = attributeClass; current is not null; current = current.BaseType)
        {
            switch (current.ToDisplayString())
            {
                case ArcAllowAnonymous: return AuthorizationKind.ArcAllowAnonymous;
                case ArcAuthorize: return AuthorizationKind.ArcAuthorize;
                case AspNetAllowAnonymous: return AuthorizationKind.AspNetAllowAnonymous;
                case AspNetAuthorize: return AuthorizationKind.AspNetAuthorize;
            }
        }

        return AuthorizationKind.None;
    }

    static bool IsModelBoundArtifactOrMember(ISymbol symbol)
    {
        var type = symbol as INamedTypeSymbol ?? symbol.ContainingType;
        return type?.GetAttributes().Any(attribute =>
        {
            var name = attribute.AttributeClass?.ToDisplayString();
            return string.Equals(name, CommandAttribute, StringComparison.Ordinal) ||
                   string.Equals(name, ReadModelAttribute, StringComparison.Ordinal);
        }) == true;
    }

    static bool IsNonEmptyString(TypedConstant constant) =>
        constant.Kind != TypedConstantKind.Array && constant.Value is string { Length: > 0 };

    static Location LocationOf(AttributeData attribute, ISymbol symbol) =>
        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault() ?? Location.None;
}
