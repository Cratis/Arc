// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Extension methods for working with <see cref="MethodInfo"/>.
/// </summary>
public static class MethodInfoExtensions
{
    /// <summary>
    /// Get the response model for a method.
    /// </summary>
    /// <param name="method">Method to inspect.</param>
    /// <returns>Tuple indicating if there is a response and the response model.</returns>
    public static (bool HasResponse, ModelDescriptor ResponseModel) GetResponseModel(this MethodInfo method)
    {
        var hasResponse = false;
        var responseModel = ModelDescriptor.Empty;

        if (method.ReturnType.IsAssignableTo<Task>() && method.ReturnType.IsGenericType)
        {
            var responseType = method.ReturnType.GetGenericArguments()[0];
            (hasResponse, responseModel) = GetResponseFromType(responseType);
        }
        else if (method.ReturnType != TypeExtensions._voidType && method.ReturnType != TypeExtensions._taskType)
        {
            (hasResponse, responseModel) = GetResponseFromType(method.ReturnType);
        }

        return (hasResponse, responseModel);
    }

    /// <summary>
    /// Get the command type from a method's parameters.
    /// </summary>
    /// <param name="method">Method to inspect.</param>
    /// <returns>The command type if found, otherwise null.</returns>
    public static Type? GetCommandType(this MethodInfo method)
    {
        var parameters = method.GetParameters();
        return parameters.FirstOrDefault()?.ParameterType;
    }

    /// <summary>
    /// Get the query type from a method.
    /// </summary>
    /// <param name="method">Method to inspect.</param>
    /// <returns>The query type if this is a query method, otherwise null.</returns>
    public static Type? GetQueryType(this MethodInfo method)
    {
        // For queries, the method itself represents the query, so we use the declaring type
        return method.DeclaringType;
    }

    static (bool HasResponse, ModelDescriptor ResponseModel) GetResponseFromType(Type type)
    {
        if (type.IsGenericType && type.FullName!.StartsWith("System.ValueTuple"))
        {
            var bestType = type.GetBestTupleType();
            return (true, bestType.ToModelDescriptor());
        }

        if (type.IsOneOf())
        {
            var bestType = type.GetBestOneOfResponseType();
            if (bestType is not null)
            {
                return (true, bestType.ToModelDescriptor());
            }

            return (false, ModelDescriptor.Empty);
        }

        return (true, type.ToModelDescriptor());
    }
}
