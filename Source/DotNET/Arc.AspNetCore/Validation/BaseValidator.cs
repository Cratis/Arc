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
    /// Defines a validation rules for a property based on <see cref="ConceptAs{T}"/> for the actual concept type.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <typeparam name="TProperty">Type of the concept.</typeparam>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, TProperty> RuleForConcept<TProperty>(Expression<Func<T, TProperty>> expression) => RuleFor(expression);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for string.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, string> RuleFor(Expression<Func<T, ConceptAs<string>>> expression)
    {
        // If the expression is just the parameter itself (x => x), use the traditional approach
        // of transforming the value, which unwraps the concept to its primitive type
        if (expression.Body is ParameterExpression)
        {
            var transformExpression = CreateTransformExpression(expression);
            return ((AbstractValidator<T>)this).RuleFor(transformExpression);
        }

        var propertyName = GetPropertyName(expression);
        var valueExpression = CreateValueExpression(expression);
        return ((AbstractValidator<T>)this).RuleFor(valueExpression).OverridePropertyName(propertyName);
    }

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for bool.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, bool> RuleFor(Expression<Func<T, ConceptAs<bool>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for Guid.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, Guid> RuleFor(Expression<Func<T, ConceptAs<Guid>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for DateOnly.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, DateOnly> RuleFor(Expression<Func<T, ConceptAs<DateOnly>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for TimeOnly.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, TimeOnly> RuleFor(Expression<Func<T, ConceptAs<TimeOnly>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for DateTime.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, DateTime> RuleFor(Expression<Func<T, ConceptAs<DateTime>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for DateTimeOffset.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, DateTimeOffset> RuleFor(Expression<Func<T, ConceptAs<DateTimeOffset>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for float.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, float> RuleFor(Expression<Func<T, ConceptAs<float>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for double.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, double> RuleFor(Expression<Func<T, ConceptAs<double>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for decimal.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, decimal> RuleFor(Expression<Func<T, ConceptAs<decimal>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for sbyte.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, sbyte> RuleFor(Expression<Func<T, ConceptAs<sbyte>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for short.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, short> RuleFor(Expression<Func<T, ConceptAs<short>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for int.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, int> RuleFor(Expression<Func<T, ConceptAs<int>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for long.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, long> RuleFor(Expression<Func<T, ConceptAs<long>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for byte.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, byte> RuleFor(Expression<Func<T, ConceptAs<byte>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for ushort.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, ushort> RuleFor(Expression<Func<T, ConceptAs<ushort>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for uint.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, uint> RuleFor(Expression<Func<T, ConceptAs<uint>>> expression)
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

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for ulong.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, ulong> RuleFor(Expression<Func<T, ConceptAs<ulong>>> expression)
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