// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Queries;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads a generated query specification directly from its exact Stage-generated shape - a substituted
/// <c>IReadModels</c>, an expected read model construction, an exact call to a declared query, and a single
/// whole-result equality assertion.
/// </summary>
/// <param name="models">The <see cref="SemanticModels"/> every body is read through.</param>
/// <param name="catalog">Every query the application itself declares, matched cross-compilation.</param>
/// <remarks>
/// This is not a scenario stated through <c>ReadModelScenario&lt;T&gt;</c> - there is no given, no when, and the
/// read model comes back from a substitute rather than from replaying events. What is read is exact and narrow by
/// design: one awaited call to one declared query, captured into one result, compared whole against one expected
/// read model construction. Anything else - a second call, a conditional one, an unassigned one, a per-property
/// assertion, a computed argument - is not this shape, and the whole scenario is left out rather than read partly.
/// </remarks>
internal class SpecificationQueryReader(SemanticModels models, IReadOnlyList<SpecificationQueryCatalog.Entry> catalog)
{
    const string ReturnsMethod = "Returns";
    const string GetInstanceByIdMethod = "GetInstanceById";
    const string SubstituteExtensionsType = "NSubstitute.SubstituteExtensions";
    const string ShouldEqualityExtensionsType = "Cratis.Specifications.ShouldEqualityExtensions";
    const string ShouldEqualMethod = "ShouldEqual";
    const string ExpectedParameter = "expected";
    const string ReturnThisParameter = "returnThis";
    const string SubstituteType = "NSubstitute.Substitute";
    const string SubstituteForMethod = "For";

    readonly HeldValues _held = new(models);
    readonly ScreenplayNaming _naming = new();

    /// <summary>
    /// Reads a generated query scenario.
    /// </summary>
    /// <param name="type">The type declaring the specification.</param>
    /// <param name="evidence">The exact scenario evidence, when the scenario is read.</param>
    /// <param name="reason">The blocking reason, set only when the type attempted this shape and could not be read exactly.</param>
    /// <returns><see langword="true"/> when the scenario was read exactly.</returns>
    /// <remarks>
    /// A type that attempts nothing of this shape yields neither an evidence nor a reason, so the caller can tell a
    /// specification of another kind - which is silently none of this reader's concern - from one that tried and
    /// fell short of it, which is reported.
    /// </remarks>
    public bool TryRead(INamedTypeSymbol type, out SpecificationQueryEvidence? evidence, out string? reason)
    {
        evidence = null;
        reason = null;

        if (type is not { TypeKind: TypeKind.Class, IsAbstract: false, ContainingType: null })
        {
            return false;
        }

        var steps = SpecificationMembers.StepsOf(type);
        var becauseMethods = SpecificationMembers.MethodsIn(steps, SpecificationMembers.BecauseMethod).ToArray();
        if (becauseMethods.Length == 0 || !SpecificationMembers.AssertionsIn(type).Any())
        {
            return false;
        }

        if (!TryFindQueryCall(
                becauseMethods,
                out var assignment,
                out var invocation,
                out var calledMethod,
                out var matched,
                out var becauseModel,
                out var becauseBody,
                out reason))
        {
            return false;
        }

        if (!type.BaseType.Is(WellKnownTypeNames.Specification))
        {
            reason = "the scenario type does not derive directly from Cratis.Specifications.Specification";
            return false;
        }

        if (!StepsTaken.Always(assignment, becauseBody))
        {
            reason = "the query it calls is only called under a condition, and a generated query scenario captures what always happened";
            return false;
        }

        if (becauseModel.GetSymbolInfo(assignment.Left).Symbol is not IFieldSymbol resultSymbol ||
            resultSymbol.Name != "_result" ||
            resultSymbol.IsStatic ||
            resultSymbol.IsReadOnly ||
            !SymbolEqualityComparer.Default.Equals(resultSymbol.ContainingType, type) ||
            resultSymbol.DeclaringSyntaxReferences.Select(_ => _.GetSyntax()).OfType<VariableDeclaratorSyntax>().SingleOrDefault() is not { Initializer: null } ||
            AssignmentsTo(type, resultSymbol).ToArray() is not [var onlyAssignment] ||
            onlyAssignment != assignment)
        {
            reason = "the query result is not captured once by the exact uninitialized field a generated query scenario declares";
            return false;
        }

        if (matched is null)
        {
            reason = "the query it calls does not match exactly one query the application itself declares";
            return false;
        }

        var inputParameters = matched.Method.Parameters.Where(QueryReader.IsInput).ToArray();
        if (inputParameters is not [var input])
        {
            reason = "the application query does not declare exactly one caller input";
            return false;
        }

        if (input.HasExplicitDefaultValue)
        {
            reason = "the application query input has an explicit default instead of being its required identifier";
            return false;
        }

        if (!IsNonNullableScalar(input.Type))
        {
            reason = "the application query input is nullable or a collection instead of one required scalar identifier";
            return false;
        }

        if (ContainsTransport(matched.Method.ReturnType))
        {
            reason = "the query returns a transport level result, which is not a read model";
            return false;
        }

        if (QueryReturnTypes.IsObservable(matched.Method.ReturnType))
        {
            reason = "the query is observable, and a generated query scenario is of a single answer";
            return false;
        }

        var isCollection = false;
        var unwrapped = QueryReturnTypes.Unwrap(matched.Method.ReturnType, ref isCollection);
        if (isCollection)
        {
            reason = "the query returns many read models, and the generated query scenario shape holds exactly one";
            return false;
        }

        if (unwrapped is not INamedTypeSymbol applicationReadModel ||
            !SymbolEqualityComparer.Default.Equals(applicationReadModel, matched.ReadModel) ||
            applicationReadModel.NullableAnnotation != NullableAnnotation.Annotated)
        {
            reason = "the application query does not return exactly one optional read model";
            return false;
        }

        if (TypeOf(resultSymbol) is not INamedTypeSymbol capturedReadModel ||
            !SymbolEqualityComparer.Default.Equals(capturedReadModel, calledMethod.ContainingType))
        {
            reason = "the captured result is not the exact read model a generated query scenario declares";
            return false;
        }

        if (resultSymbol.NullableAnnotation != NullableAnnotation.Annotated)
        {
            reason = "the captured result is not nullable as a generated query scenario declares";
            return false;
        }

        var resultProperties = matched.ReadModel.DeclaredProperties().ToArray();
        if (resultProperties.Any(property => !IsSupportedResultProperty(property)))
        {
            reason = "the expected read model declares a nullable, collection, or unsupported result property";
            return false;
        }

        var keyProperties = resultProperties.Where(property =>
            string.Equals(_naming.ToPropertyName(property.Name), _naming.ToPropertyName(input.Name), StringComparison.Ordinal) &&
            SymbolEqualityComparer.IncludeNullability.Equals(property.Type, input.Type) &&
            IsNonNullableScalar(property.Type)).ToArray();
        if (keyProperties.Length != 1)
        {
            reason = "the sole required query input does not match exactly one non-nullable scalar read-model property by normalized name and exact type";
            return false;
        }

        if (!TryReadReadModels(invocation, calledMethod, becauseModel, type, out var readModelsSymbol, out reason) ||
            !TryReadArguments(invocation, calledMethod, matched.Method, becauseModel, out var arguments, out var argumentEvidence, out reason))
        {
            return false;
        }

        if (!TryReadAssertion(type, resultSymbol, calledMethod.ContainingType, out var expectedSymbol, out var construction, out reason))
        {
            return false;
        }

        if (construction.SemanticModel.GetTypeInfo(construction.Creation).Type is not INamedTypeSymbol expectedType ||
            !SymbolEqualityComparer.Default.Equals(expectedType, calledMethod.ContainingType))
        {
            reason = "the expected construction is not the exact read model the query returns";
            return false;
        }

        if (!IsExactPrimaryReadModelConstruction(construction, expectedType, matched.ReadModel, resultProperties, out var constructionReason))
        {
            reason = constructionReason;
            return false;
        }

        var draft = new SpecificationDraft();
        var values = new SpecificationValues(new ScreenplayDiagnostics(), new GeneratedIdentities(models));
        var result = values.ReadQuery(construction.Creation, construction.SemanticModel, expectedType, matched.ReadModel, type.Name, type.ToDisplayString(), draft).ToArray();
        if (!HasEveryExpectedValue(expectedType, result))
        {
            reason = "the expected read model omits, repeats, reorders, or computes a value the generated construction states directly";
            return false;
        }

        var applicationProperties = resultProperties;
        if (applicationProperties.Length != result.Length ||
            applicationProperties.Zip(result).Any(pair => !string.Equals(pair.First.Name, pair.Second.Property, StringComparison.Ordinal)))
        {
            reason = "the expected read model values do not match the application read model in declaration order";
            return false;
        }

        if (!TryReadEstablish(
                steps,
                calledMethod.ContainingType,
                readModelsSymbol,
                expectedSymbol,
                arguments.Single(),
                calledMethod.Parameters[input.Ordinal].Type,
                input.Type,
                out reason))
        {
            return false;
        }

        var valueEvidence = new Dictionary<PropertyMappingModel, Location>(ReferenceEqualityComparer.Instance);
        foreach (var (value, source) in argumentEvidence)
        {
            valueEvidence.Add(value, source);
        }

        foreach (var (value, source) in draft.GetValueEvidence())
        {
            valueEvidence.TryAdd(value, source);
        }

        evidence = new(
            type,
            StableSourceOf(type),
            matched.Method,
            invocation.GetLocation(),
            matched.ReadModel,
            construction.Creation.GetLocation(),
            true,
            arguments,
            [.. result],
            valueEvidence);
        return true;
    }

    static Location StableSourceOf(INamedTypeSymbol type) =>
        type.DeclaringSyntaxReferences
            .Select(_ => _.GetSyntax().GetLocation())
            .Where(_ => _.IsInSource)
            .OrderBy(_ => NormalizedPath(_.SourceTree?.FilePath), StringComparer.Ordinal)
            .ThenBy(_ => _.SourceSpan.Start)
            .First();

    static string NormalizedPath(string? path) =>
        (path ?? string.Empty).Replace('\\', '/').Normalize();

    static bool IsNonNullableScalar(ITypeSymbol type) =>
        type.NullableAnnotation != NullableAnnotation.Annotated &&
        type is not INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } &&
        CollectionElements.ElementOf(type) is null;

    static bool IsExactStagePrimitive(INamedTypeSymbol type)
    {
        var name = type.FullMetadataName();
        return string.Equals(name, "System.Guid", StringComparison.Ordinal) ||
            string.Equals(name, "System.String", StringComparison.Ordinal) ||
            string.Equals(name, "System.Int32", StringComparison.Ordinal) ||
            string.Equals(name, "System.Decimal", StringComparison.Ordinal) ||
            string.Equals(name, "System.Boolean", StringComparison.Ordinal) ||
            string.Equals(name, "System.DateOnly", StringComparison.Ordinal) ||
            string.Equals(name, "System.DateTimeOffset", StringComparison.Ordinal);
    }

    static bool IsExactPrimaryReadModelConstruction(
        HeldConstruction construction,
        INamedTypeSymbol expectedType,
        INamedTypeSymbol sourceType,
        IPropertySymbol[] sourceProperties,
        out string reason)
    {
        reason = "the expected read model is not constructed through its exact positional-record primary constructor";
        var localProperties = expectedType.DeclaredProperties().ToArray();
        if (!expectedType.IsRecord || !sourceType.IsRecord)
        {
            return false;
        }

        if (construction.Creation.Initializer is not null ||
            construction.Creation.ArgumentList?.Arguments is not { } arguments ||
            arguments.Count != sourceProperties.Length ||
            arguments.Any(_ => _.NameColon is not null))
        {
            return false;
        }

        if (construction.SemanticModel.GetSymbolInfo(construction.Creation).Symbol is not IMethodSymbol selectedConstructor ||
            !SymbolEqualityComparer.Default.Equals(selectedConstructor.ContainingType, expectedType))
        {
            return false;
        }

        if (sourceType.InstanceConstructors.SingleOrDefault(IsPrimaryRecordConstructor) is not { } sourceConstructor)
        {
            return false;
        }

        if (selectedConstructor.Parameters.Length != sourceConstructor.Parameters.Length ||
            localProperties.Length != selectedConstructor.Parameters.Length ||
            sourceProperties.Length != sourceConstructor.Parameters.Length ||
            !ParametersMatchProperties(selectedConstructor.Parameters, localProperties) ||
            !ParametersMatchProperties(sourceConstructor.Parameters, sourceProperties))
        {
            return false;
        }

        return selectedConstructor.Parameters.Zip(sourceConstructor.Parameters).All(pair =>
            string.Equals(pair.First.Name, pair.Second.Name, StringComparison.Ordinal) &&
            TypesHaveSameIdentity(pair.First.Type, pair.Second.Type));
    }

    static bool IsPrimaryRecordConstructor(IMethodSymbol constructor) =>
        constructor.DeclaringSyntaxReferences.Any(_ => _.GetSyntax() is RecordDeclarationSyntax { ParameterList: not null });

    static bool ParametersMatchProperties(
        IReadOnlyList<IParameterSymbol> parameters,
        IReadOnlyList<IPropertySymbol> properties) =>
        parameters.Zip(properties).All(pair =>
            string.Equals(pair.First.Name, pair.Second.Name, StringComparison.Ordinal) &&
            SymbolEqualityComparer.IncludeNullability.Equals(pair.First.Type, pair.Second.Type));

    static bool TypesHaveSameIdentity(ITypeSymbol first, ITypeSymbol second) =>
        first.NullableAnnotation == second.NullableAnnotation &&
        string.Equals(
            first.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            second.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            StringComparison.Ordinal);

    /// <summary>
    /// Finds the single awaited call a generated query scenario captures, from every <c>Because</c> in the chain.
    /// </summary>
    /// <param name="becauseMethods">Every authored <c>Because</c> method in the specification chain.</param>
    /// <param name="assignment">The exact result assignment.</param>
    /// <param name="invocation">The exact query invocation.</param>
    /// <param name="method">The exactly bound called method.</param>
    /// <param name="matched">The authoritative application query match, when one exists.</param>
    /// <param name="semanticModel">The semantic model owning the call.</param>
    /// <param name="body">The exact <c>Because</c> body.</param>
    /// <param name="reason">The blocking reason when the attempted shape is not exact.</param>
    /// <returns><see langword="true"/> when exactly one awaited assignment is found.</returns>
    /// <remarks>
    /// Every awaited call to a declared query's own read model is gathered first, whether or not it is captured by
    /// an assignment - so that a second call, or one nobody captures, is what makes the scenario ambiguous rather
    /// than something silently ignored. A type with no such call at all is not attempting this shape, and is left
    /// for whatever else reads it.
    /// </remarks>
    bool TryFindQueryCall(
        IReadOnlyList<IMethodSymbol> becauseMethods,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AssignmentExpressionSyntax? assignment,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out InvocationExpressionSyntax? invocation,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IMethodSymbol? method,
        out SpecificationQueryCatalog.Entry? matched,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SemanticModel? semanticModel,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SyntaxNode? body,
        out string? reason)
    {
        assignment = null;
        invocation = null;
        method = null;
        matched = null;
        semanticModel = null;
        body = null;
        reason = null;

        var found = new List<QueryAttempt>();
        foreach (var because in becauseMethods)
        {
            foreach (var candidateBody in HandlerBodies.Of(because))
            {
                if (models.For(candidateBody.SyntaxTree) is not { } candidateModel)
                {
                    continue;
                }

                foreach (var awaited in candidateBody.DescendantNodesAndSelf().OfType<AwaitExpressionSyntax>())
                {
                    if (MappingSourceReader.Unwrap(awaited.Expression) is not InvocationExpressionSyntax candidateInvocation ||
                        DotNetInvocations.MethodFor(candidateInvocation, candidateModel) is not { IsStatic: true } candidateMethod)
                    {
                        continue;
                    }

                    var matches = catalog.Where(entry => DotNetMethodSignatures.Matches(candidateMethod, entry.Signature)).ToArray();
                    found.Add(new(awaited, candidateInvocation, candidateMethod, candidateModel, candidateBody, matches));
                }
            }
        }

        var exact = found.Where(_ => _.Matches.Count > 0).ToArray();
        var attempts = exact.Length > 0 ? exact : found.Where(_ => LooksLikeStageQueryCall(_.Method)).ToArray();
        if (attempts.Length == 0)
        {
            return false;
        }

        if (attempts.Length > 1)
        {
            reason = "it captures more than one query call, and a generated query scenario captures exactly one";
            return false;
        }

        var attempt = attempts[0];
        if (attempt.Matches.Count > 1)
        {
            reason = "the query it calls matches more than one query the application declares";
            return false;
        }

        if (becauseMethods is not [var singleBecause] ||
            singleBecause.IsStatic ||
            !singleBecause.IsAsync ||
            singleBecause.Parameters.Length != 0 ||
            !singleBecause.ReturnType.Is("System.Threading.Tasks.Task") ||
            singleBecause.DeclaringSyntaxReferences.Select(_ => _.GetSyntax()).OfType<MethodDeclarationSyntax>().SingleOrDefault() is not
            {
                Body: null,
                ExpressionBody.Expression: AssignmentExpressionSyntax
                {
                    RawKind: (int)SyntaxKind.SimpleAssignmentExpression,
                    Right: AwaitExpressionSyntax
                    {
                        Expression: InvocationExpressionSyntax exactInvocation
                    } exactAwait
                } exactAssignment
            } exactBecause ||
            attempt.Await != exactAwait ||
            attempt.Invocation != exactInvocation ||
            attempt.Body != exactAssignment ||
            exactBecause.ExpressionBody?.Expression != exactAssignment)
        {
            reason = "Because is not exactly one parameterless expression-bodied awaited query assignment";
            return false;
        }

        assignment = exactAssignment;
        invocation = attempt.Invocation;
        method = attempt.Method;
        matched = attempt.Matches.SingleOrDefault();
        semanticModel = attempt.SemanticModel;
        body = exactAssignment;
        return true;
    }

    /// <summary>
    /// Reads the exact substituted <c>IReadModels</c> field handed to the query.
    /// </summary>
    /// <param name="invocation">The exact query invocation.</param>
    /// <param name="method">The method bound in the specification compilation.</param>
    /// <param name="semanticModel">The semantic model owning the invocation.</param>
    /// <param name="specification">The exact scenario type declaring the field.</param>
    /// <param name="readModels">The exact substituted field.</param>
    /// <param name="reason">The blocking reason when the collaborator is not exact.</param>
    /// <returns><see langword="true"/> when the exact field is read.</returns>
    bool TryReadReadModels(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        SemanticModel semanticModel,
        INamedTypeSymbol specification,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IFieldSymbol? readModels,
        out string? reason)
    {
        readModels = null;
        reason = null;
        var parameters = method.Parameters.Where(parameter => parameter.Type.Is(WellKnownTypeNames.ReadModels)).ToArray();
        if (parameters is not [var parameter] ||
            DotNetInvocations.ArgumentForParameter(invocation, method, parameter.Name, semanticModel)?.Expression is not { } argument ||
            semanticModel.GetSymbolInfo(MappingSourceReader.Unwrap(argument)).Symbol is not IFieldSymbol
            {
                IsReadOnly: true,
                Type.TypeKind: TypeKind.Interface
            } field ||
            field.Name != "_readModels" ||
            field.IsStatic ||
            !field.Type.Is(WellKnownTypeNames.ReadModels) ||
            field.Type.NullableAnnotation == NullableAnnotation.Annotated ||
            !SymbolEqualityComparer.Default.Equals(field.ContainingType, specification) ||
            field.DeclaringSyntaxReferences.Select(_ => _.GetSyntax()).OfType<VariableDeclaratorSyntax>().SingleOrDefault() is not
            {
                Initializer.Value: InvocationExpressionSyntax initializer
            } ||
            models.For(initializer.SyntaxTree) is not { } initializerModel ||
            DotNetInvocations.MethodFor(initializer, initializerModel) is not { } substitute ||
            DotNetInvocations.DefinitionOf(substitute) is not
            {
                IsStatic: true,
                Name: SubstituteForMethod,
                TypeParameters.Length: 1
            } substituteDefinition ||
            substituteDefinition.ContainingType.ToDisplayString() != SubstituteType ||
            substitute.TypeArguments is not [var substitutedType] ||
            !SymbolEqualityComparer.Default.Equals(substitutedType, field.Type) ||
            initializer.ArgumentList.Arguments.Count != 0 ||
            AssignmentsTo(specification, field).Any())
        {
            reason = "the query is not handed the exact readonly directly initialized NSubstitute IReadModels field a generated query scenario declares";
            return false;
        }

        readModels = field;
        return true;
    }

    /// <summary>
    /// Gets every authored assignment to one field in the exact specification type.
    /// </summary>
    /// <param name="type">The authored specification type.</param>
    /// <param name="field">The field to inspect.</param>
    /// <returns>The exact assignments.</returns>
    IEnumerable<AssignmentExpressionSyntax> AssignmentsTo(INamedTypeSymbol type, IFieldSymbol field) =>
        type.DeclaringSyntaxReferences
            .Select(_ => _.GetSyntax())
            .SelectMany(_ => _.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            .Where(assignment => models.For(assignment.SyntaxTree) is { } model &&
                SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(assignment.Left).Symbol, field));

    /// <summary>
    /// Determines whether a computed call still returns a model-bound read model and therefore attempted this shape.
    /// </summary>
    /// <param name="method">The computed method call.</param>
    /// <returns><see langword="true"/> when its answer is a model-bound read model.</returns>
    bool ReturnsReadModel(IMethodSymbol method)
    {
        var collection = false;
        return QueryReturnTypes.Unwrap(method.ReturnType, ref collection) is { } returned && QueryReader.IsReadModel(returned);
    }

    /// <summary>
    /// Reads the exact input arguments a query call gives, in the query's own formal parameter order.
    /// </summary>
    /// <param name="invocation">The exact query invocation.</param>
    /// <param name="calledMethod">The method bound in the specification compilation.</param>
    /// <param name="applicationMethod">The exact matched application method.</param>
    /// <param name="semanticModel">The semantic model owning the invocation.</param>
    /// <param name="arguments">The exact values in application formal parameter order.</param>
    /// <param name="evidence">The exact authored argument locations.</param>
    /// <param name="reason">The blocking reason when an argument is not exact.</param>
    /// <returns><see langword="true"/> when every input argument is exact.</returns>
    bool TryReadArguments(
        InvocationExpressionSyntax invocation,
        IMethodSymbol calledMethod,
        IMethodSymbol applicationMethod,
        SemanticModel semanticModel,
        out IReadOnlyList<PropertyMappingModel> arguments,
        out IReadOnlyList<(PropertyMappingModel Value, Location Source)> evidence,
        out string? reason)
    {
        arguments = [];
        evidence = [];
        reason = null;

        var values = new List<PropertyMappingModel>();
        var located = new List<(PropertyMappingModel, Location)>();
        var sources = new MappingSourceReader(new ScreenplayDiagnostics());
        foreach (var (parameter, index) in applicationMethod.Parameters.Select((parameter, index) => (parameter, index)).Where(_ => QueryReader.IsInput(_.parameter)))
        {
            var calledParameter = calledMethod.Parameters[index];
            if (DotNetInvocations.ArgumentForParameter(invocation, calledMethod, calledParameter.Name, semanticModel)?.Expression is not { } argument)
            {
                reason = $"the query argument '{parameter.Name}' is not given by exactly one exact authored argument";
                return false;
            }

            if (sources.ReadQueryLiteral(argument, semanticModel, calledParameter.Type, parameter.Type) is not { } literal)
            {
                reason = $"the query argument '{parameter.Name}' is not an exact Stage-authored scalar or concept value";
                return false;
            }

            var value = new PropertyMappingModel(parameter.Name, literal);
            values.Add(value);
            located.Add((value, argument.GetLocation()));
        }

        arguments = values;
        evidence = located;
        return true;
    }

    /// <summary>
    /// Reads the single exact whole-result equality assertion a generated query scenario declares.
    /// </summary>
    /// <param name="type">The authored specification type.</param>
    /// <param name="resultSymbol">The exact field receiving the query result.</param>
    /// <param name="readModel">The exact read model the called query is declared on.</param>
    /// <param name="expectedSymbol">The exact expected field.</param>
    /// <param name="construction">The direct expected read-model construction.</param>
    /// <param name="reason">The blocking reason when the assertion is not exact.</param>
    /// <returns><see langword="true"/> when the exact whole-result assertion is read.</returns>
    /// <remarks>
    /// The assertion has to name the exact result symbol as its receiver, or it is not this assertion - a property
    /// of the result, or the expected read model written the other way around, both fail that the same way a
    /// lookalike helper of the same name does.
    /// </remarks>
    bool TryReadAssertion(
        INamedTypeSymbol type,
        ISymbol resultSymbol,
        INamedTypeSymbol readModel,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ISymbol? expectedSymbol,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out HeldConstruction? construction,
        out string? reason)
    {
        expectedSymbol = null;
        construction = null;
        reason = null;

        var facts = SpecificationMembers.AssertionsIn(type).ToArray();
        if (facts.Length != 1)
        {
            reason = "it does not declare exactly one assertion, and a generated query scenario declares exactly one";
            return false;
        }

        var fact = facts[0];
        if (fact.IsStatic ||
            !fact.ReturnsVoid ||
            fact.Parameters.Length != 0 ||
            fact.DeclaringSyntaxReferences.Select(_ => _.GetSyntax()).OfType<MethodDeclarationSyntax>().SingleOrDefault() is not
            {
                Body: null,
                ExpressionBody.Expression: InvocationExpressionSyntax assertion
            } ||
            models.For(assertion.SyntaxTree) is not { } assertionModel ||
            DotNetInvocations.MethodFor(assertion, assertionModel) is not { } assertionMethod ||
            !IsExactEquality(assertionMethod))
        {
            reason = "the single assertion is not exactly one expression-bodied whole-result ShouldEqual call";
            return false;
        }

        var receiver = DotNetInvocations.ReceiverFor(assertion, assertionMethod, assertionModel);
        if (receiver is null ||
            assertionModel.GetSymbolInfo(Unwrap(receiver)).Symbol is not { } receiverSymbol ||
            !SymbolEqualityComparer.Default.Equals(receiverSymbol, resultSymbol))
        {
            reason = "the exact query result field is not the receiver of the whole-result equality";
            return false;
        }

        if (DotNetInvocations.ArgumentForParameter(assertion, assertionMethod, ExpectedParameter, assertionModel)?.Expression is not { } expectedExpression)
        {
            reason = "the expected read model is not given by an exact authored argument";
            return false;
        }

        if (assertionModel.GetSymbolInfo(Unwrap(expectedExpression)).Symbol is not IFieldSymbol { IsReadOnly: true } foundExpectedSymbol ||
            foundExpectedSymbol.Name != "_expected" ||
            foundExpectedSymbol.IsStatic ||
            !SymbolEqualityComparer.Default.Equals(foundExpectedSymbol.ContainingType, type) ||
            !SymbolEqualityComparer.Default.Equals(foundExpectedSymbol.Type, readModel) ||
            foundExpectedSymbol.Type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            reason = "the expected read model is not the exact readonly non-nullable field a generated query scenario declares";
            return false;
        }

        if (_held.ConstructionOf(expectedExpression, assertionModel) is not { } foundConstruction ||
            foundConstruction.Creation.Initializer is not null ||
            foundExpectedSymbol.DeclaringSyntaxReferences.Select(_ => _.GetSyntax()).OfType<VariableDeclaratorSyntax>().SingleOrDefault()?.Initializer?.Value is not { } declaredExpected ||
            MappingSourceReader.Unwrap(declaredExpected) != foundConstruction.Creation ||
            AssignmentsTo(type, foundExpectedSymbol).Any())
        {
            reason = "the expected read model is not constructed directly in its field declaration";
            return false;
        }

        expectedSymbol = foundExpectedSymbol;
        construction = foundConstruction;
        return true;
    }

    /// <summary>
    /// Confirms an optional substitute setup, when present, exactly corroborates the query call and the expected
    /// read model - never states a fact of its own.
    /// </summary>
    /// <param name="steps">The specification chain carrying setup.</param>
    /// <param name="readModel">The exact returned read model.</param>
    /// <param name="readModelsSymbol">The exact substitute handed to the query.</param>
    /// <param name="expectedSymbol">The expected field corroborated by setup.</param>
    /// <param name="key">The exact first query argument.</param>
    /// <param name="expectedKeyType">The exact key type in the specification compilation.</param>
    /// <param name="sourceExpectedKeyType">The corresponding exact application-declared key type.</param>
    /// <param name="reason">The blocking reason when setup conflicts.</param>
    /// <returns><see langword="true"/> when setup is absent or corroborates exactly.</returns>
    bool TryReadEstablish(
        INamedTypeSymbol steps,
        INamedTypeSymbol readModel,
        IFieldSymbol readModelsSymbol,
        ISymbol expectedSymbol,
        PropertyMappingModel key,
        ITypeSymbol expectedKeyType,
        ITypeSymbol sourceExpectedKeyType,
        out string? reason)
    {
        reason = null;
        var establishMethods = SpecificationMembers.MethodsIn(steps, SpecificationMembers.EstablishMethod).ToArray();
        if (establishMethods.Length == 0)
        {
            return true;
        }

        if (establishMethods is not [var establish] ||
            establish.IsStatic ||
            !establish.ReturnsVoid ||
            establish.Parameters.Length != 0 ||
            establish.DeclaringSyntaxReferences.Select(_ => _.GetSyntax()).OfType<MethodDeclarationSyntax>().SingleOrDefault() is not
            {
                Body: null,
                ExpressionBody.Expression: InvocationExpressionSyntax returnsInvocation
            } ||
            models.For(returnsInvocation.SyntaxTree) is not { } establishModel ||
            returnsInvocation.ArgumentList.Arguments is not [_] ||
            DotNetInvocations.MethodFor(returnsInvocation, establishModel) is not { Name: ReturnsMethod } returnsMethod ||
            DotNetInvocations.DefinitionOf(returnsMethod).ContainingType.ToDisplayString() != SubstituteExtensionsType ||
            DotNetInvocations.ReceiverFor(returnsInvocation, returnsMethod, establishModel) is not InvocationExpressionSyntax getInstanceInvocation ||
            DotNetInvocations.MethodFor(getInstanceInvocation, establishModel) is not { Name: GetInstanceByIdMethod } getInstanceMethod ||
            !getInstanceMethod.ContainingType.Is(WellKnownTypeNames.ReadModels))
        {
            reason = "Establish is not exactly one parameterless expression-bodied GetInstanceById<T>(key).Returns(expected) setup";
            return false;
        }
        if (getInstanceMethod.TypeArguments is not [INamedTypeSymbol configuredReadModel] ||
            !SymbolEqualityComparer.Default.Equals(configuredReadModel, readModel) ||
            getInstanceInvocation.ArgumentList.Arguments.Count != 1)
        {
            reason = "the substitute setup configures a different read model than the query returns";
            return false;
        }

        if (DotNetInvocations.ReceiverFor(getInstanceInvocation, getInstanceMethod, establishModel) is not { } readModelsExpression ||
            establishModel.GetSymbolInfo(MappingSourceReader.Unwrap(readModelsExpression)).Symbol is not { } establishedReadModels ||
            !SymbolEqualityComparer.Default.Equals(establishedReadModels, readModelsSymbol))
        {
            reason = "the substitute setup configures a different IReadModels field than the query is handed";
            return false;
        }

        if (DotNetInvocations.ArgumentForParameter(returnsInvocation, returnsMethod, ReturnThisParameter, establishModel)?.Expression is not { } returnsExpression ||
            establishModel.GetSymbolInfo(Unwrap(returnsExpression)).Symbol is not { } returnsSymbol ||
            !SymbolEqualityComparer.Default.Equals(returnsSymbol, expectedSymbol))
        {
            reason = "the substitute setup returns a different expected read model than the assertion compares against";
            return false;
        }

        if (key.Source is not LiteralSource literalKey ||
            getInstanceMethod.Parameters.Length == 0 ||
            DotNetInvocations.ArgumentForParameter(getInstanceInvocation, getInstanceMethod, getInstanceMethod.Parameters[0].Name, establishModel)?.Expression is not CastExpressionSyntax
            {
                Type: var castType,
                Expression: var keyExpression
            } ||
            !establishModel.GetTypeInfo(castType).Type.Is(WellKnownTypeNames.EventSourceId) ||
            new MappingSourceReader(new ScreenplayDiagnostics()).ReadQueryLiteral(keyExpression, establishModel, expectedKeyType, sourceExpectedKeyType) is not { } establishKey ||
            !Equals(establishKey.Value, literalKey.Value))
        {
            reason = "the substitute setup keys the read model by a different value than the query is called with";
            return false;
        }

        return true;
    }

    bool IsExactEquality(IMethodSymbol method)
    {
        var definition = DotNetInvocations.DefinitionOf(method);
        return definition.ContainingType.ToDisplayString() == ShouldEqualityExtensionsType &&
            definition.Name == ShouldEqualMethod &&
            definition.IsExtensionMethod &&
            definition.TypeParameters.Length == 1 &&
            definition.Parameters is [var actual, var expected] &&
            SymbolEqualityComparer.Default.Equals(actual.Type, definition.TypeParameters[0]) &&
            SymbolEqualityComparer.Default.Equals(expected.Type, definition.TypeParameters[0]);
    }

    bool LooksLikeStageQueryCall(IMethodSymbol method) =>
        method.Parameters.Count(_ => _.Type.Is(WellKnownTypeNames.ReadModels)) == 1 &&
        method.Parameters.Count(QueryReader.IsInput) == 1 &&
        (QueryReader.IsReadModel(method.ContainingType) || ReturnsReadModel(method));

    bool IsSupportedResultProperty(IPropertySymbol property)
    {
        if (!IsNonNullableScalar(property.Type))
        {
            return false;
        }

        if (property.Type.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        if (property.Type is not INamedTypeSymbol named)
        {
            return false;
        }

        if (IsExactStagePrimitive(named))
        {
            return true;
        }

        return named.FindBase(WellKnownTypeNames.ConceptAs) is { TypeArguments: [var backing] } &&
            IsNonNullableScalar(backing) &&
            (backing.TypeKind == TypeKind.Enum ||
             (backing is INamedTypeSymbol namedBacking && IsExactStagePrimitive(namedBacking)));
    }

    ITypeSymbol TypeOf(IFieldSymbol symbol) => symbol.Type;

    bool HasEveryExpectedValue(INamedTypeSymbol type, PropertyMappingModel[] values)
    {
        var properties = type.DeclaredProperties().ToArray();
        return properties.Length == values.Length &&
            properties.Zip(values).All(pair => string.Equals(pair.First.Name, pair.Second.Property, StringComparison.Ordinal));
    }

    bool ContainsTransport(ITypeSymbol type)
    {
        var current = type;
        while (true)
        {
            if (QueryReturnTypes.IsTransport(current))
            {
                return true;
            }

            if (current is not INamedTypeSymbol { TypeArguments: [var wrapped] })
            {
                return false;
            }

            current = wrapped;
        }
    }

    ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (true)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    expression = parenthesized.Expression;
                    break;
                case PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressed:
                    expression = suppressed.Operand;
                    break;
                default:
                    return expression;
            }
        }
    }

    sealed record QueryAttempt(
        AwaitExpressionSyntax Await,
        InvocationExpressionSyntax Invocation,
        IMethodSymbol Method,
        SemanticModel SemanticModel,
        SyntaxNode Body,
        IReadOnlyList<SpecificationQueryCatalog.Entry> Matches);
}
