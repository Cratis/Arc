// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Strings;
using FluentValidation;

namespace Cratis.Arc.Validation;

#pragma warning disable CS0618 // Type or member is obsolete (Related to FluentValidation and the Transform method)
#pragma warning disable IDE0004 // Remove unnecessary cast (We need to do this to access the correct RuleFor())

/// <summary>
/// Represents a base validator that we use for discovery.
/// </summary>
/// <typeparam name="T">Type of object the validator is for.</typeparam>
public class BaseValidator<T> : AbstractValidator<T>
{
    /// <summary>
    /// Define a condition for when the context is a command.
    /// </summary>
    /// <param name="callback">Callback for defining the rules when it is a command.</param>
    /// <returns><see cref="IConditionBuilder"/> for building on the condition.</returns>
    public IConditionBuilder WhenCommand(Action callback) => When((model, context) => context.IsCommand(), callback);

    /// <summary>
    /// Define a condition for when the context is a command.
    /// </summary>
    /// <param name="callback">Callback for defining the rules when it is a command.</param>
    /// <returns><see cref="IConditionBuilder"/> for building on the condition.</returns>
    public IConditionBuilder WhenQuery(Action callback) => When((model, context) => context.IsQuery(), callback);

    /// <summary>
    /// Defines a validation rule for a property that is a <see cref="ConceptAs{TValue}"/>, unwrapping the
    /// inner value so that standard FluentValidation rules (e.g. <c>NotEmpty</c>) operate on the primitive.
    /// </summary>
    /// <remarks>
    /// A single generic overload is used intentionally. C# type inference resolves <typeparamref name="TValue"/>
    /// exclusively from the class hierarchy (<c>MyConcept : ConceptAs&lt;Guid&gt;</c> → <c>TValue = Guid</c>)
    /// without considering user-defined implicit conversions. This eliminates the CS0121 ambiguity that
    /// arises when a concept type has implicit operators to multiple <c>ConceptAs&lt;string&gt;</c> subtypes
    /// (e.g. <c>EventSourceId</c> and <c>ReadModelKey</c>).
    /// </remarks>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <typeparam name="TValue">The primitive type wrapped by the concept.</typeparam>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, TValue> RuleFor<TValue>(Expression<Func<T, ConceptAs<TValue>>> expression)
        where TValue : IComparable
    {
        if (expression.Body is ParameterExpression)
        {
            var transformExpression = CreateTransformExpression(expression);
            return ((AbstractValidator<T>)this).RuleFor(transformExpression);
        }

        var propertyName = GetPropertyName(expression);
        var valueExpression = CreateValueExpression(expression);
        return ((AbstractValidator<T>)this).RuleFor(valueExpression).OverridePropertyName(propertyName);
    }

    static Expression<Func<T, TProperty>> CreateValueExpression<TProperty>(Expression<Func<T, ConceptAs<TProperty>>> expression)
        where TProperty : IComparable
    {
        var parameter = expression.Parameters[0];
        var body = expression.Body;

        // Check if concept is null, and if so return default (null for reference types)
        // This prevents NullReferenceException when accessing .Value on a null concept
        var nullCheck = Expression.NotEqual(body, Expression.Constant(null, body.Type));
        var valueProperty = Expression.Property(body, nameof(ConceptAs<TProperty>.Value));
        var defaultValue = Expression.Default(typeof(TProperty));
        var conditional = Expression.Condition(nullCheck, valueProperty, defaultValue);

        return Expression.Lambda<Func<T, TProperty>>(conditional, parameter);
    }

    static Expression<Func<T, TProperty>> CreateTransformExpression<TProperty>(Expression<Func<T, ConceptAs<TProperty>>> expression)
        where TProperty : IComparable
    {
        var parameter = expression.Parameters[0];

        // Create: arg => expression.Compile().Invoke(arg) != null ? expression.Compile().Invoke(arg).Value : default
        var compiled = expression.Compile();
        var invokeExpression = Expression.Invoke(Expression.Constant(compiled), parameter);
        var nullCheck = Expression.NotEqual(invokeExpression, Expression.Constant(null, invokeExpression.Type));
        var valueProperty = Expression.Property(invokeExpression, nameof(ConceptAs<TProperty>.Value));
        var defaultValue = Expression.Default(typeof(TProperty));
        var conditional = Expression.Condition(nullCheck, valueProperty, defaultValue);

        return Expression.Lambda<Func<T, TProperty>>(conditional, parameter);
    }

    static string GetPropertyName<TProperty>(Expression<Func<T, ConceptAs<TProperty>>> expression)
        where TProperty : IComparable
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name.ToCamelCase();
        }

        throw new ArgumentException("Expression must be a member expression", nameof(expression));
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore IDE0004 // Remove unnecessary cast (We need to do this to access the correct RuleFor())
