// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';

/**
 * Represents a property descriptor.
 */
export class PropertyDescriptor {
    /**
     * Initializes a new instance of the {@link PropertyDescriptor} class.
     * @param name Name of the property.
     * @param type Type of the property.
     * @param isOptional Whether the property is optional (nullable).
     */
    constructor(readonly name: string, readonly type: Constructor, readonly isOptional: boolean = false) {
    }
}
