// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { IIdentity } from './IIdentity';

/**
 * Defines the identity provider.
 */
export abstract class IIdentityProvider {

    /**
     * Gets the current identity by optionally specifying the details type.
     * @param type Optional constructor for the details type to enable type-safe deserialization.
     * @returns The current identity as {@link IIdentity}.
     * @remarks The `extends object` constraint is required for compatibility with JsonSerializer.deserializeFromInstance().
     */
    abstract getCurrent<TDetails extends object = object>(type?: Constructor<TDetails>): Promise<IIdentity<TDetails>>;
}
