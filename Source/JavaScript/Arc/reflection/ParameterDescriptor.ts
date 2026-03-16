// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';

/**
 * Represents a parameter descriptor.
 */
export class ParameterDescriptor {
    /**
     * Initializes a new instance of the {@link ParameterDescriptor} class.
     * @param name Name of the parameter.
     * @param type Type of the parameter.
     * @param isEnumerable Whether the parameter is an enumerable (collection) type.
     */
    constructor(readonly name: string, readonly type: Constructor, readonly isEnumerable: boolean = false) {
    }
}
