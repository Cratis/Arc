// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cratis.Arc.Queries.ControllerBased;

/// <summary>
/// Represents a query performer for controller-based query actions.
/// </summary>
/// <param name="actionDescriptor">The controller action descriptor.</param>
/// <param name="serviceProviderIsService">Service to determine if a type is registered as a service.</param>
/// <param name="authorizationEvaluator">The authorization evaluator.</param>
public class ControllerQueryPerformer(
    ControllerActionDescriptor actionDescriptor,
    IServiceProviderIsService serviceProviderIsService,
    IAuthorizationEvaluator authorizationEvaluator) : IQueryPerformer
{
    readonly MethodInfo _performMethod = actionDescriptor.MethodInfo;

    /// <inheritdoc/>
    public QueryName Name { get; } = actionDescriptor.MethodInfo.Name;

    /// <inheritdoc/>
    public FullyQualifiedQueryName FullyQualifiedName { get; } =
        $"{actionDescriptor.ControllerTypeInfo.FullName}.{actionDescriptor.MethodInfo.Name}";

    /// <inheritdoc/>
    public Type Type { get; } = actionDescriptor.ControllerTypeInfo.AsType();

    /// <inheritdoc/>
    public Type ReadModelType { get; } = actionDescriptor.ControllerTypeInfo.AsType();

    /// <inheritdoc/>
    public IEnumerable<string> Location { get; } = actionDescriptor.ControllerTypeInfo.Namespace?.Split('.') ?? [];

    /// <inheritdoc/>
    public IEnumerable<Type> Dependencies { get; } = [typeof(IServiceProvider)];

    /// <inheritdoc/>
    public QueryParameters Parameters { get; } = new(actionDescriptor.MethodInfo.GetParameters()
        .Where(_ => !IsDependencyParameter(_, serviceProviderIsService))
        .Select(_ => new QueryParameter(_.Name ?? string.Empty, _.ParameterType)));

    /// <inheritdoc/>
    public bool AllowsAnonymousAccess { get; } = actionDescriptor.MethodInfo.IsAnonymousAllowed();

    /// <inheritdoc/>
    public bool SupportsPaging { get; } = ComputeSupportsPaging(actionDescriptor.MethodInfo);

    /// <inheritdoc/>
    public bool IsAuthorized(QueryContext context) => authorizationEvaluator.IsAuthorized(_performMethod);

    /// <inheritdoc/>
    public async ValueTask<object?> Perform(QueryContext context)
    {
        var serviceProvider = context.Dependencies?.OfType<IServiceProvider>().FirstOrDefault()
            ?? throw new InvalidOperationException($"A service provider dependency was not available when performing controller query '{FullyQualifiedName}'.");

        var controller = CreateControllerInstance(serviceProvider);
        try
        {
            var args = GetMethodArguments(_performMethod.GetParameters(), context.Arguments ?? QueryArguments.Empty, serviceProvider);
            var invocationResult = _performMethod.Invoke(controller, args);
            var (_, result) = await AwaitableHelpers.AwaitIfNeeded(invocationResult);
            return UnwrapMvcResult(result);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
        finally
        {
            switch (controller)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }

#pragma warning disable SA1204 // Static members should appear before non-static members
    static bool ComputeSupportsPaging(MethodInfo performMethod)
    {
        var returnType = performMethod.ReturnType;

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        return returnType.IsAssignableTo(typeof(IQueryable));
    }

    static bool IsDependencyParameter(ParameterInfo parameter, IServiceProviderIsService serviceProviderIsService)
    {
        if (parameter.ParameterType == typeof(CancellationToken))
        {
            return true;
        }

        var bindingSource = parameter.GetCustomAttributes(inherit: true)
            .OfType<IBindingSourceMetadata>()
            .FirstOrDefault()?
            .BindingSource;

        if (bindingSource == BindingSource.Services)
        {
            return true;
        }

        if (bindingSource is not null)
        {
            return false;
        }

        return serviceProviderIsService.IsService(parameter.ParameterType);
    }

    object CreateControllerInstance(IServiceProvider serviceProvider)
    {
        var constructors = Type
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(_ => _.GetParameters().Length)
            .ToArray();

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var args = new object?[parameters.Length];
            var allResolved = true;

            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];

                if (parameter.ParameterType == typeof(IServiceProvider))
                {
                    args[index] = serviceProvider;
                    continue;
                }

                var resolved = serviceProvider.GetService(parameter.ParameterType);
                if (resolved is null)
                {
                    if (parameter.HasDefaultValue)
                    {
                        args[index] = parameter.DefaultValue;
                        continue;
                    }

                    allResolved = false;
                    break;
                }

                args[index] = resolved;
            }

            if (allResolved)
            {
                return constructor.Invoke(args);
            }
        }

        throw new InvalidOperationException($"Unable to construct controller '{Type.FullName}' for query '{FullyQualifiedName}'.");
    }

    object?[] GetMethodArguments(ParameterInfo[] parameters, QueryArguments queryArguments, IServiceProvider serviceProvider)
    {
        var args = new object?[parameters.Length];

        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];

            if (IsDependencyParameter(parameter, serviceProviderIsService))
            {
                args[index] = ResolveDependencyParameter(parameter, serviceProvider);
                continue;
            }

            args[index] = ResolveQueryArgument(parameter, queryArguments);
            if (args[index] is null && !IsNullableOrOptional(parameter))
            {
                throw new MissingArgumentForQuery(parameter.Name ?? "unknown", parameter.ParameterType, FullyQualifiedName);
            }
        }

        return args;
    }

    static object ResolveDependencyParameter(ParameterInfo parameter, IServiceProvider serviceProvider)
    {
        if (parameter.ParameterType == typeof(CancellationToken))
        {
            return serviceProvider.GetService<IHttpRequestContextAccessor>()?.Current?.RequestAborted ?? CancellationToken.None;
        }

        if (parameter.ParameterType == typeof(IServiceProvider))
        {
            return serviceProvider;
        }

        return serviceProvider.GetRequiredService(parameter.ParameterType);
    }

    static object? ResolveQueryArgument(ParameterInfo parameter, QueryArguments queryArguments)
    {
        var parameterWasProvided = queryArguments.TryGetValue(parameter.Name ?? string.Empty, out var value);
        if (!parameterWasProvided)
        {
            return parameter.HasDefaultValue ? parameter.DefaultValue : null;
        }

        var valueIsEmpty = string.IsNullOrEmpty(value?.ToString());
        if (valueIsEmpty && !CanRepresentEmptyString(parameter.ParameterType))
        {
            return parameter.HasDefaultValue ? parameter.DefaultValue : null;
        }

        return value?.ConvertTo(parameter.ParameterType);
    }

    static bool CanRepresentEmptyString(Type type) => type == typeof(string);

    static bool IsNullableOrOptional(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
        {
            return true;
        }

        var type = parameter.ParameterType;
        if (type.IsValueType)
        {
            return Nullable.GetUnderlyingType(type) is not null;
        }

        return true;
    }

    static object? UnwrapMvcResult(object? result)
    {
        if (result is null)
        {
            return null;
        }

        if (result is ObjectResult objectResult)
        {
            return objectResult.Value;
        }

        if (result is JsonResult jsonResult)
        {
            return jsonResult.Value;
        }

        var resultType = result.GetType();
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(ActionResult<>))
        {
            var nestedResult = resultType.GetProperty("Result")?.GetValue(result);
            if (nestedResult is not null)
            {
                return UnwrapMvcResult(nestedResult);
            }

            return resultType.GetProperty("Value")?.GetValue(result);
        }

        return result;
    }
#pragma warning restore SA1204 // Static members should appear before non-static members
}
