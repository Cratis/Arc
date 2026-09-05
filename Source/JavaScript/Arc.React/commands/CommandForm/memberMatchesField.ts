// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Determines whether a validation result member attributes to a given command form field.
 *
 * A member matches when it is exactly the field name, or a dotted path whose leading segment is the field
 * name (e.g. `email.Value` attributes to the `email` field). The dotted-path case lets a failure that
 * originates in a nested validator — such as a `ConceptValidator<T>`'s `RuleFor(x => x.Value)`, which the
 * server reports as `email.Value` — surface on the field it belongs to.
 * @param members - The members reported by a validation result, or undefined.
 * @param fieldName - The command form field name to match against.
 * @returns True when a member attributes to the field; otherwise false.
 */
export const memberMatchesField = (members: readonly string[] | undefined, fieldName: string): boolean =>
    !!members?.some(member => member === fieldName || member.startsWith(`${fieldName}.`));
