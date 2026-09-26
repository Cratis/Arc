// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IHaveParameters } from './IHaveParameters.js';

/**
 * Helper for working with types that have parameters.
 */
export const ParametersHelper = {
    /**
     * Collects the parameter values from an instance based on its parameter descriptors.
     * @param instance The instance to collect parameter values from.
     * @returns A record of parameter names to their values.
     */
    collectParameterValues(instance: IHaveParameters): Record<string, unknown> {
        const values: Record<string, unknown> = {};
        for (const descriptor of instance.parameterDescriptors) {
            const value = (instance as unknown as Record<string, unknown>)[descriptor.name];
            if (value !== undefined && value !== null) {
                values[descriptor.name] = value;
            }
        }
        return values;
    }
};
