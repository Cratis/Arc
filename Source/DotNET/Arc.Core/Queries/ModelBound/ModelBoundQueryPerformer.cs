// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
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
    /// <param name="performMethod">The method info of the perform method.</param>
    /// <param name="serviceProviderIsService">Service to determine if a type is registered as a service.</param>
    /// <param name="authorizationEvaluator">The authorization evaluator.</param>
    public ModelBoundQueryPerformer(Type readModelType, MethodInfo performMethod, IServiceProviderIsService serviceProviderIsService, IAuthorizationEvaluator authorizationEvaluator)
    {
        Type = readModelType;
        ReadModelType = readModelType;
        Name = performMethod.Name;
        FullyQualifiedName = $"{readModelType.FullName}.{performMethod.Name}";
        Location = readModelType.Namespace?.Split('.') ?? [];

        _dependencies = performMethod.GetParameters().Where(p => serviceProviderIsService.IsService(p.ParameterType));
        _queryParameters = performMethod.GetParameters().Where(p => !serviceProviderIsService.IsService(p.ParameterType));
        Dependencies = _dependencies.Select(p => p.ParameterType);
        Parameters = new(_queryParameters.Select(p => new QueryParameter(p.Name ?? string.Empty, p.ParameterType)));
        AllowsAnonymousAccess = performMethod.IsAnonymousAllowed();
        IsEnumerableResult = ComputeIsEnumerableResult(performMethod, readModelType);
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
    public IEnumerable<Type> Dependencies { get; }

    /// <inheritdoc/>
    public QueryParameters Parameters { get; }

    /// <inheritdoc/>
    public bool AllowsAnonymousAccess { get; }

    /// <inheritdoc/>
    public bool IsEnumerableResult { get; }

    /// <inheritdoc/>
    public bool IsAuthorized(QueryContext context) => _authorizationEvaluator.IsAuthorized(_performMethod);

    /// <inheritdoc/>
    public async ValueTask<object?> Perform(QueryContext context)
    {
        var parameters = _performMethod.GetParameters();
        var dependencies = context.Dependencies?.ToArray() ?? [];
        var queryStringParameters = context.Arguments ?? QueryArguments.Empty;
        var args = GetMethodArguments(parameters, dependencies, queryStringParameters);

        var invocationResult = _performMethod.Invoke(null, args);
        var (_, result) = await AwaitableHelpers.AwaitIfNeeded(invocationResult);
        return result;
    }

    static bool IsNullableOrOptional(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
        {
            return true;
        }

        var type = parameter.ParameterType;

        return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
    }

    static bool ComputeIsEnumerableResult(MethodInfo performMethod, Type readModelType)
    {
        var returnType = performMethod.ReturnType;

        // Unwrap Task<T> if it's an async method
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        // Check if it's a collection of the read model type
        if (returnType.IsArray && returnType.GetElementType() == readModelType)
        {
            return true;
        }

        if (returnType.IsAssignableTo(typeof(IEnumerable<>).MakeGenericType(readModelType)))
        {
            return true;
        }

        // IAsyncEnumerable<T> is considered enumerable for paging purposes
        if (returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>) &&
            returnType.GetGenericArguments()[0] == readModelType)
        {
            return true;
        }

        // ISubject<T> is a streaming/observable result handled via WebSocket,
        // not a pageable collection. Paging parameters should NOT be added for these.
        return false;
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
                if (dependencyIndex < dependencies.Length)
                {
                    args[i] = dependencies[dependencyIndex];
                    dependencyIndex++;
                }
            }
            else
            {
                var matchingQueryParam = queryStringParameters.FirstOrDefault(kvp =>
                    string.Equals(kvp.Key, parameter.Name, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(matchingQueryParam.Key))
                {
                    args[i] = matchingQueryParam.Value.ConvertTo(parameter.ParameterType);
                }
                else if (parameter.HasDefaultValue)
                {
                    args[i] = parameter.DefaultValue;
                }
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