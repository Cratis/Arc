// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
using Cratis.Arc.Authorization;
using Cratis.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Represents a model bound query performer.
/// </summary>
public class ModelBoundQueryPerformer : IQueryPerformer
{
    readonly IEnumerable<ParameterInfo> _dependencies;
    readonly IEnumerable<ParameterInfo> _queryParameters;
    readonly MethodInfo _performMethod;
    readonly IAuthorizationEvaluator _authorizationEvaluator;

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

        _dependencies = performMethod.GetParameters().Where(p => IsDependency(serviceProviderIsService, p));
        _queryParameters = performMethod.GetParameters().Where(p => !IsDependency(serviceProviderIsService, p));
        Dependencies = _dependencies.Select(p => p.ParameterType);
        Parameters = new(_queryParameters.Select(p => new QueryParameter(p.Name ?? string.Empty, p.ParameterType, !IsNullableOrOptional(p))));
        AllowsAnonymousAccess = performMethod.IsAnonymousAllowed();
        SupportsPaging = ComputeSupportsPaging(performMethod);
        _performMethod = performMethod;
        _authorizationEvaluator = authorizationEvaluator;
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
    public bool IsAuthorized(QueryContext context) => _authorizationEvaluator.IsAuthorized(_performMethod);

    /// <inheritdoc/>
    public async ValueTask<object?> Perform(QueryContext context)
    {
        var parameters = _performMethod.GetParameters();
        var dependencies = context.Dependencies?.ToArray() ?? [];
        var queryStringParameters = context.Arguments ?? QueryArguments.Empty;
        var args = GetMethodArguments(parameters, dependencies, queryStringParameters);

        try
        {
            var invocationResult = _performMethod.Invoke(null, args);
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

    static object? ResolveQueryArgument(ParameterInfo parameter, QueryArguments queryStringParameters)
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

        return matchingQueryParam.Value.ConvertTo(parameter.ParameterType);
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
    /// </remarks>
    static bool IsDependency(IServiceProviderIsService serviceProviderIsService, ParameterInfo parameter) =>
        !parameter.ParameterType.IsValueType &&
        serviceProviderIsService.IsService(parameter.ParameterType);

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

    object?[] GetMethodArguments(ParameterInfo[] parameters, object[] dependencies, QueryArguments queryStringParameters)
    {
        var dependencyIndex = 0;
        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            if (_dependencies.Contains(parameter))
            {
                args[i] = ResolveDependency(dependencies, ref dependencyIndex);
            }
            else
            {
                args[i] = ResolveQueryArgument(parameter, queryStringParameters);
            }
        }

        ValidateArguments(parameters, args);
        return args;
    }

    void ValidateArguments(ParameterInfo[] parameters, object?[] args)
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
