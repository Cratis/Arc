// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RuleBuilder } from './RuleBuilder';
import { NotEmptyRule, NotNullRule } from './rules/NotEmptyRule';
import { MinLengthRule, MaxLengthRule, LengthRule } from './rules/LengthRules';
import { EmailRule } from './rules/EmailRule';
import { RegexRule } from './rules/RegexRule';
import { GreaterThanRule, GreaterThanOrEqualRule, LessThanRule, LessThanOrEqualRule } from './rules/ComparisonRules';

/**
 * Extension methods for {@link RuleBuilder} to add validation rules in a fluent manner.
 */

/**
 * Add a rule that the property must not be empty (not null, undefined, empty string, or empty array).
 * @template T The type being validated.
 * @template TProperty The type of the property.
 * @param this The rule builder instance.
 * @returns The rule builder for chaining.
 */
export function notEmpty<T, TProperty>(this: RuleBuilder<T, TProperty>): RuleBuilder<T, TProperty> {
    const propertyAccessor = (this as any)._propertyAccessor;
    return this.addRule(new NotEmptyRule<T>(propertyAccessor));
}

/**
 * Add a rule that the property must not be null or undefined.
 * @template T The type being validated.
 * @template TProperty The type of the property.
 * @param this The rule builder instance.
 * @returns The rule builder for chaining.
 */
export function notNull<T, TProperty>(this: RuleBuilder<T, TProperty>): RuleBuilder<T, TProperty> {
    const propertyAccessor = (this as any)._propertyAccessor;
    return this.addRule(new NotNullRule<T>(propertyAccessor));
}

/**
 * Add a rule that the string property must have at least the specified length.
 * @template T The type being validated.
 * @param this The rule builder instance.
 * @param length The minimum length.
 * @returns The rule builder for chaining.
 */
export function minLength<T>(this: RuleBuilder<T, string>, length: number): RuleBuilder<T, string> {
    const propertyAccessor = (this as any)._propertyAccessor;
    return this.addRule(new MinLengthRule<T>(propertyAccessor, length));
}

/**
 * Add a rule that the string property must have at most the specified length.
 * @template T The type being validated.
 * @param this The rule builder instance.
 * @param length The maximum length.
 * @returns The rule builder for chaining.
 */
export function maxLength<T>(this: RuleBuilder<T, string>, length: number): RuleBuilder<T, string> {
    const propertyAccessor = (this as any)._propertyAccessor;
    return this.addRule(new MaxLengthRule<T>(propertyAccessor, length));
}

/**
 * Add a rule that the string property must have a length between min and max.
 * @template T The type being validated.
 * @param this The rule builder instance.
 * @param min The minimum length.
 * @param max The maximum length.
 * @returns The rule builder for chaining.
 */
export function length<T>(this: RuleBuilder<T, string>, min: number, max: number): RuleBuilder<T, string> {
    const propertyAccessor = (this as any)._propertyAccessor;
    return this.addRule(new LengthRule<T>(propertyAccessor, min, max));
}

/**
 * Add a rule that the string property must be a valid email address.
 * @template T The type being validated.
 * @param this The rule builder instance.
 * @returns The rule builder for chaining.
 */
export function emailAddress<T>(this: RuleBuilder<T, string>): RuleBuilder<T, string> {
    const propertyAccessor = (this as any)._propertyAccessor;
    return this.addRule(new EmailRule<T>(propertyAccessor));
}

/**
 * Add a rule that the string property must match the specified regular expression.
 * @template T The type being validated.
 * @param this The rule builder instance.
 * @param pattern The regular expression pattern.
 * @param errorMessage Optional error message.
 * @returns The rule builder for chaining.
 */
export function matches<T>(this: RuleBuilder<T, string>, pattern: RegExp, errorMessage?: string): RuleBuilder<T, string> {
    const propertyAccessor = (this as any)._propertyAccessor;
    return this.addRule(new RegexRule<T>(propertyAccessor, pattern, errorMessage));
}

/**
 * Add a rule that the number property must be greater than the specified value.
 * @template T The type being validated.
 * @param this The rule builder instance.
 * @param threshold The value to compare against.
 * @returns The rule builder for chaining.
 */
export function greaterThan<T>(this: RuleBuilder<T, number>, threshold: number): RuleBuilder<T, number> {
    const propertyAccessor = (this as any)._propertyAccessor;
    return this.addRule(new GreaterThanRule<T>(propertyAccessor, threshold));
}

/**
 * Add a rule that the number property must be greater than or equal to the specified value.
 * @template T The type being validated.
 * @param this The rule builder instance.
 * @param threshold The value to compare against.
 * @returns The rule builder for chaining.
 */
export function greaterThanOrEqual<T>(this: RuleBuilder<T, number>, threshold: number): RuleBuilder<T, number> {
    const propertyAccessor = (this as any)._propertyAccessor;
    return this.addRule(new GreaterThanOrEqualRule<T>(propertyAccessor, threshold));
}

/**
 * Add a rule that the number property must be less than the specified value.
 * @template T The type being validated.
 * @param this The rule builder instance.
 * @param threshold The value to compare against.
 * @returns The rule builder for chaining.
 */
export function lessThan<T>(this: RuleBuilder<T, number>, threshold: number): RuleBuilder<T, number> {
    const propertyAccessor = (this as any)._propertyAccessor;
    return this.addRule(new LessThanRule<T>(propertyAccessor, threshold));
}

/**
 * Add a rule that the number property must be less than or equal to the specified value.
 * @template T The type being validated.
 * @param this The rule builder instance.
 * @param threshold The value to compare against.
 * @returns The rule builder for chaining.
 */
export function lessThanOrEqual<T>(this: RuleBuilder<T, number>, threshold: number): RuleBuilder<T, number> {
    const propertyAccessor = (this as any)._propertyAccessor;
    return this.addRule(new LessThanOrEqualRule<T>(propertyAccessor, threshold));
}

// Add the extension methods to the RuleBuilder prototype
RuleBuilder.prototype.notEmpty = notEmpty;
RuleBuilder.prototype.notNull = notNull;
RuleBuilder.prototype.minLength = minLength;
RuleBuilder.prototype.maxLength = maxLength;
RuleBuilder.prototype.length = length;
RuleBuilder.prototype.emailAddress = emailAddress;
RuleBuilder.prototype.matches = matches;
RuleBuilder.prototype.greaterThan = greaterThan;
RuleBuilder.prototype.greaterThanOrEqual = greaterThanOrEqual;
RuleBuilder.prototype.lessThan = lessThan;
RuleBuilder.prototype.lessThanOrEqual = lessThanOrEqual;

// Extend the RuleBuilder interface to include these methods
declare module './RuleBuilder' {
    interface RuleBuilder<T, TProperty> {
        notEmpty(): RuleBuilder<T, TProperty>;
        notNull(): RuleBuilder<T, TProperty>;
    }

    interface RuleBuilder<T, string> {
        minLength(length: number): RuleBuilder<T, string>;
        maxLength(length: number): RuleBuilder<T, string>;
        length(min: number, max: number): RuleBuilder<T, string>;
        emailAddress(): RuleBuilder<T, string>;
        matches(pattern: RegExp, errorMessage?: string): RuleBuilder<T, string>;
    }

    interface RuleBuilder<T, number> {
        greaterThan(threshold: number): RuleBuilder<T, number>;
        greaterThanOrEqual(threshold: number): RuleBuilder<T, number>;
        lessThan(threshold: number): RuleBuilder<T, number>;
        lessThanOrEqual(threshold: number): RuleBuilder<T, number>;
    }
}
