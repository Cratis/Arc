// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { Constructor } from '@cratis/fundamentals';
import { IdentityProviderContext } from './IdentityProvider';
import { IIdentity } from '@cratis/arc/identity';

/**
 * Hook to get the identity context.
 * @param defaultDetails Optional default details to use if the context is not set.
 * @returns An identity context.
 */
export function useIdentity<TDetails = object>(defaultDetails?: TDetails | undefined | null): IIdentity<TDetails>;

/**
 * Hook to get the identity context with type-safe deserialization.
 * @param type Constructor for the details type to enable type-safe deserialization.
 * @param defaultDetails Optional default details to use if the context is not set.
 * @returns An identity context.
 */
export function useIdentity<TDetails = object>(type: Constructor<TDetails>, defaultDetails?: TDetails | undefined | null): IIdentity<TDetails>;

export function useIdentity<TDetails = object>(
    typeOrDefaultDetails?: Constructor<TDetails> | TDetails | undefined | null,
    defaultDetails?: TDetails | undefined | null
): IIdentity<TDetails> {
    const contextValue = React.useContext(IdentityProviderContext);
    const identity = contextValue.identity as IIdentity<TDetails>;
    
    // Determine if first argument is a Constructor or default details
    // Constructors are functions, but regular functions would be unusual here.
    // We rely on the type system and developer intent - if a function is passed, 
    // it's expected to be a constructor class.
    const isConstructor = typeof typeOrDefaultDetails === 'function';
    const actualDefaultDetails = isConstructor ? defaultDetails : typeOrDefaultDetails;
    
    if (identity.isSet === false && actualDefaultDetails !== undefined) {
        identity.details = actualDefaultDetails!;
    }
    
    return identity;
}
