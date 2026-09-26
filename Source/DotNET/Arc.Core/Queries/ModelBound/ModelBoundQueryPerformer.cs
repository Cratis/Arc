// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Cratis.Arc.Authorization;
using Cratis.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Represents a model bound query performer.
/// </summary>
public class ModelBoundQueryPerformer : IQueryPerformer, IFrameworkAuthorizationQueryTarget
{
    readonly ImmutableArray<ParameterInfo> _parameters;
    readonly ImmutableHashSet<int> _dependencyPositions;
    readonly IAuthorizationEvaluator? _authorizationEvaluator;
    readonly Func<QueryContext, bool>? _authorizeFromScope;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelBoundQueryPerformer"/> class.
    /// </summary>
    /// <param name="readModelType">The type of the read model.</param>
    /// <param name="readModelTypeName">The read model type name to use for fully qualified query naming.</param>
    /// <param name="performMethod">The method info of the perform method.</param>
    /// <param name="serviceProviderIsService">Service to determine if a type is registered as a service.</param>
    /// <param name="authorizationEvaluator">The authorization evaluator.</param>
    /// <exception cref="QueryMethodCannotBeGeneric">Thrown when <paramref name="performMethod"/> is generic.</exception>
    public ModelBoundQueryPerformer(Type readModelType, string readModelTypeName, MethodInfo performMethod, IServiceProviderIsService serviceProviderIsService, IAuthorizationEvaluator authorizationEvaluator)
        : this(readModelType, readModelTypeName, performMethod, serviceProviderIsService)
    {
        _authorizationEvaluator = authorizationEvaluator;
    }

    /// <summary>
    /// Creates a performer that resolves authorization from each query's scope, rather than caching the discovery scope.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <param name="readModelTypeName">The qualified read model name.</param>
    /// <param name="performMethod">The query method.</param>
    /// <param name="serviceProviderIsService">The service classification.</param>
    /// <param name="scopeFactory">A fallback for direct performer calls without a query scope.</param>
    /// <param name="resolveEvaluator">Resolves the configured evaluator in a scope.</param>
    public ModelBoundQueryPerformer(
        Type readModelType,
        string readModelTypeName,
        MethodInfo performMethod,
        IServiceProviderIsService serviceProviderIsService,
        IServiceScopeFactory scopeFactory,
        Func<IServiceProvider, IAuthorizationEvaluator> resolveEvaluator)
        : this(readModelType, readModelTypeName, performMethod, serviceProviderIsService)
    {
        _authorizeFromScope = context =>
        {
            if (context.ServiceProvider is { } services)
            {
                return resolveEvaluator(services).IsAuthorized(AuthorizationMethod);
            }

            using var scope = scopeFactory.CreateScope();
            return resolveEvaluator(scope.ServiceProvider).IsAuthorized(AuthorizationMethod);
        };
    }

    ModelBoundQueryPerformer(Type readModelType, string readModelTypeName, MethodInfo performMethod, IServiceProviderIsService serviceProviderIsService)
    {
        // Fail while wiring up rather than on every request: invoking an open generic throws a bare BCL message that
        // says nothing about which read model method is at fault.
        if (performMethod.ContainsGenericParameters)
        {
            throw new QueryMethodCannotBeGeneric(performMethod);
        }

        Type = readModelType;
        ReadModelType = readModelType;
        Name = performMethod.Name;
        FullyQualifiedName = $"{readModelTypeName}.{performMethod.Name}";
        Location = readModelType.Namespace?.Split('.') ?? [];

        // Check for Path attribute on method or type
        var pathAttribute = performMethod.GetCustomAttributes(true)
            .FirstOrDefault(a => a.GetType().Name == "PathAttribute") ??
            readModelType.GetCustomAttributes(true)
            .FirstOrDefault(a => a.GetType().Name == "PathAttribute");

        if (pathAttribute != null)
        {
            var pathProperty = pathAttribute.GetType().GetProperty("Path");
            CustomRoute = pathProperty?.GetValue(pathAttribute) as string;
        }

        _parameters = performMethod.GetParameters().ToImmutableArray();
        var dependencyPositions = ImmutableHashSet.CreateBuilder<int>();
        var dependencyTypes = ImmutableArray.CreateBuilder<Type>();
        var queryParameters = ImmutableArray.CreateBuilder<QueryParameter>();
        foreach (var parameter in _parameters)
        {
            if (IsDependency(serviceProviderIsService, parameter))
            {
                dependencyPositions.Add(parameter.Position);
                dependencyTypes.Add(parameter.ParameterType);
            }
            else
            {
                queryParameters.Add(new QueryParameter(parameter.Name ?? string.Empty, parameter.ParameterType, !IsNullableOrOptional(parameter)));
            }
        }

        _dependencyPositions = dependencyPositions.ToImmutable();
        Dependencies = dependencyTypes.ToImmutable();
        Parameters = new(queryParameters.ToImmutable());
        AllowsAnonymousAccess = performMethod.IsAnonymousAllowed();
        SupportsPaging = ComputeSupportsPaging(performMethod);
        AuthorizationMethod = performMethod;
    }

    /// <inheritdoc/>
    public QueryName Name { get; }

    /// <inheritdoc/>
    public FullyQualifiedQueryName FullyQualifiedName { get; }

    /// <inheritdoc/>
    public Type Type { get; }

    /// <inheritdoc/>
    public Type ReadModelType { get; }

    /// <inheritdoc/>
    public IEnumerable<string> Location { get; }

    /// <inheritdoc/>
    public string? CustomRoute { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Dependencies { get; }

    /// <inheritdoc/>
    public QueryParameters Parameters { get; }

    /// <inheritdoc/>
    public bool AllowsAnonymousAccess { get; }

    /// <inheritdoc/>
    public bool SupportsPaging { get; }

    /// <inheritdoc/>
    public MethodInfo AuthorizationMethod { get; }

    /// <inheritdoc/>
    public bool HasIndependentLegacyVerdict => _authorizeFromScope is null;

    /// <inheritdoc/>
    public bool IsAuthorized(QueryContext context) => _authorizeFromScope is not null
        ? _authorizeFromScope(context)
        : _authorizationEvaluator!.IsAuthorized(AuthorizationMethod);

    /// <inheritdoc/>
    public async ValueTask<object?> Perform(QueryContext context)
    {
        var dependencies = context.Dependencies?.ToArray() ?? [];
        var queryStringParameters = context.Arguments ?? QueryArguments.Empty;
        var args = GetMethodArguments(dependencies, queryStringParameters);

        try
        {
            var invocationResult = AuthorizationMethod.Invoke(null, args);
            var (_, result) = await AwaitableHelpers.AwaitIfNeeded(invocationResult);
            return result;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Rethrow the inner exception preserving its original stack trace.
            // The trailing 'throw' is unreachable but required because the C# compiler
            // does not propagate [DoesNotReturn] through async state machines.
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    static object? ResolveDependency(object[] dependencies, ref int dependencyIndex)
    {
        if (dependencyIndex < dependencies.Length)
        {
            return dependencies[dependencyIndex++];
        }

        return null;
    }

    static object? ResolveQueryArgument(ParameterInfo parameter, QueryArguments queryStringParameters, FullyQualifiedQueryName queryName)
    {
        var matchingQueryParam = queryStringParameters.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, parameter.Name, StringComparison.OrdinalIgnoreCase));

        var parameterWasProvided = !string.IsNullOrEmpty(matchingQueryParam.Key);
        if (!parameterWasProvided)
        {
            return parameter.HasDefaultValue ? parameter.DefaultValue : null;
        }

        var valueIsEmpty = string.IsNullOrEmpty(matchingQueryParam.Value?.ToString());
        if (valueIsEmpty && !CanRepresentEmptyString(parameter.ParameterType))
        {
            return parameter.HasDefaultValue ? parameter.DefaultValue : null;
        }

        return matchingQueryParam.Value.ConvertQueryArgument(parameter.ParameterType, parameter.Name ?? "unknown", queryName);
    }

    static bool CanRepresentEmptyString(Type type) =>
        type == typeof(string) || (type.IsConcept() && type.GetConceptValueType() == typeof(string));

    /// <summary>
    /// Determines whether a parameter is an injected dependency rather than an argument the caller supplies.
    /// </summary>
    /// <param name="serviceProviderIsService">Used to ask the container whether the type is a registered service.</param>
    /// <param name="parameter">The <see cref="ParameterInfo"/> to classify.</param>
    /// <returns>True when the parameter should be resolved from the container; otherwise false.</returns>
    /// <remarks>
    /// Asking the container alone is not sufficient. <c>AddSelfBindings</c> self-registers every discovered concrete
    /// type, and an enum is concrete - so <c>IsService</c> answers true for it and an enum query argument was
    /// classified as a dependency, then failed to resolve at request time with a container error naming the enum.
    /// A value type is never something the caller injects here, so it is excluded before the container is consulted.
    /// <para>
    /// The same is true, for a different reason, of an <c>IEnumerable&lt;T&gt;</c> whose element type is a
    /// primitive, a concept, or an enum: the BCL's default <see cref="IServiceProviderIsService"/> answers true for
    /// <em>any</em> <c>IEnumerable&lt;T&gt;</c> unconditionally, because the container can always satisfy it with an
    /// empty collection. Left unchecked, a query parameter like <c>IEnumerable&lt;int&gt; ids</c> is classified as a
    /// dependency, dropped from <see cref="Parameters"/>, and silently injected as an empty collection instead of
    /// the caller-supplied values - see <see cref="ConverterExtensions.IsEnumerableOfQueryArgumentElement"/> for the
    /// classification this excludes and why it cannot simply be shared with the proxy generator's mirror predicate.
    /// A collection-typed <em>service</em> parameter (e.g. <c>IEnumerable&lt;ISomeService&gt;</c>) is unaffected -
    /// its element type is neither a primitive, a concept, nor an enum, so it still defers to the container exactly
    /// as before.
    /// </para>
    /// <para>
    /// A concept (<c>ConceptAs&lt;T&gt;</c>) is excluded for the same reason as an enum: it is a concrete reference
    /// type, so self-binding registers it and <c>IsService</c> answers true. It was then resolved from the container
    /// instead of bound from the request, and failed trying to construct it from its primitive value. A concept is
    /// always a caller-supplied argument, as it already is inside a collection and in the generated proxy. A nullable
    /// concept (<c>T?</c>) shares the same runtime type, so it is covered too.
    /// </para>
    /// </remarks>
    static bool IsDependency(IServiceProviderIsService serviceProviderIsService, ParameterInfo parameter) =>
        !IsNeverADependency(parameter.ParameterType) &&
        serviceProviderIsService.IsService(parameter.ParameterType);

    static bool IsNeverADependency(Type type) =>
        type.IsValueType || type.IsConcept() || type.IsEnumerableOfQueryArgumentElement(out _) || type.IsNestedQueryArgumentCollection();

    static bool IsNullableOrOptional(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
        {
            return true;
        }

        var type = parameter.ParameterType;

        // Value types are only optional when wrapped in Nullable<T>.
        if (type.IsValueType)
        {
            return Nullable.GetUnderlyingType(type) is not null;
        }

        // Concept types are value-like wrappers and are never implicitly nullable.
        // Use T? (nullable annotation) or a default value to make a concept parameter optional.
        if (type.IsConcept())
        {
            var nullabilityInfo = new NullabilityInfoContext().Create(parameter);
            return nullabilityInfo.WriteState is NullabilityState.Nullable;
        }

        // Other reference types (e.g. plain string) remain implicitly optional.
        return true;
    }

    static bool ComputeSupportsPaging(MethodInfo performMethod)
    {
        var returnType = performMethod.ReturnType;

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        return returnType.IsAssignableTo(typeof(IQueryable));
    }

    object?[] GetMethodArguments(object[] dependencies, QueryArguments queryStringParameters)
    {
        var dependencyIndex = 0;
        var args = new object?[_parameters.Length];
        for (var i = 0; i < _parameters.Length; i++)
        {
            var parameter = _parameters[i];

            if (_dependencyPositions.Contains(parameter.Position))
            {
                args[i] = ResolveDependency(dependencies, ref dependencyIndex);
            }
            else
            {
                args[i] = ResolveQueryArgument(parameter, queryStringParameters, FullyQualifiedName);
            }
        }

        ValidateArguments(_parameters, args);
        return args;
    }

    void ValidateArguments(ImmutableArray<ParameterInfo> parameters, object?[] args)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var arg = args[i];

            if (arg is null && !IsNullableOrOptional(parameter))
            {
                throw new MissingArgumentForQuery(parameter.Name ?? "unknown", parameter.ParameterType, FullyQualifiedName);
            }
        }
    }
}
