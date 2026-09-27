// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ParameterDescriptor } from './ParameterDescriptor.js';

/**
 * Defines something that has parameters.
 */
export interface IHaveParameters {
    readonly parameterDescriptors: ParameterDescriptor[];
}
