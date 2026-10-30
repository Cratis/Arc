// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;

namespace Cratis.Arc.Queries.for_QueryableExtensions.given;

public static class Queryables
{
    /// <summary>
    /// Finds the generic method definition of a <see cref="Queryable"/> operator taking a source and an int or a key selector.
    /// </summary>
    /// <param name="name">Name of the operator.</param>
    /// <returns>The method definition.</returns>
    public static MethodInfo MethodDefinition(string name) =>
        typeof(Queryable).GetMethods().Single(_ =>
            _.Name == name &&
            _.GetParameters().Length == 2 &&
            (_.GetParameters()[1].ParameterType == typeof(int) || IsKeySelector(_.GetParameters()[1].ParameterType)));

    /// <summary>
    /// Finds the generic method definition of the <see cref="Queryable"/> Count operator without predicate.
    /// </summary>
    /// <returns>The method definition.</returns>
    public static MethodInfo CountDefinition() =>
        typeof(Queryable).GetMethods().Single(_ => _.Name == nameof(Queryable.Count) && _.GetParameters().Length == 1);

    public static LambdaExpression KeySelector(MethodCallExpression call) =>
        (LambdaExpression)((UnaryExpression)call.Arguments[1]).Operand;

    static bool IsKeySelector(Type type) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition() == typeof(Expression<>) &&
        type.GetGenericArguments()[0].IsGenericType &&
        type.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(Func<,>);
}
