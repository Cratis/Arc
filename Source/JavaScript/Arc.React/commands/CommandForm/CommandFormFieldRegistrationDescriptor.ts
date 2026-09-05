// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Stable population metadata registered by a bound CommandForm field.
 */
export interface CommandFormFieldRegistrationDescriptor {
    propertyName: string;
    currentValue: unknown;
    noInitialValue: boolean;
    populationKey: unknown;
    populationRevision: number;
    valueAccessorRef: { current: ((instance: unknown) => unknown) | undefined };
    initialValueRef: { current: ((source: unknown) => unknown) | undefined };
}
