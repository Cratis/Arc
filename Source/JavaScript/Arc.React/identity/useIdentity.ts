// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { Constructor } from '@cratis/fundamentals';
import { deserializeIdentityDetails } from '@cratis/arc/identity';
import { IdentityProviderContext } from './IdentityProvider';
import { IIdentityContext } from './IIdentityContext';

/**
 * Caches details already deserialized for a given raw payload, keyed first by the raw payload object
 * and then by the details type deserialized into.
 *
 * Module-level and keyed by object identity - not `React.useMemo` - so that:
 * - Every component reading the same identity with the same type gets back the exact same instance,
 *   which a per-component-instance `useMemo` cannot provide.
 * - This hook stays callable outside a render tree, which the existing specs rely on
 *   (`sinon.stub(React, 'useContext')` invokes it directly, not through a mounted component).
 * - Entries are garbage-collected automatically once the raw payload they are keyed on is replaced
 *   (e.g. by a refresh) and nothing else references it.
 */
const deserializedDetailsCache = new WeakMap<object, Map<Constructor, unknown>>();

/**
 * Resolves the details a caller of {@link useIdentity} should see, given what the provider already
 * did (if anything) and what the caller is asking for.
 * @param {Constructor | undefined} providerDetailsConstructor The type the provider already
 * deserialized `rawDetails` with, if any.
 * @param {Constructor | undefined} type The type the caller asked to deserialize into, if any.
 * @param {unknown} rawDetails The details currently held by the identity context.
 * @param {unknown} defaultDetails The default to fall back to when there are no details to give. Used
 * as-is, never deserialized - the caller already supplies it typed.
 * @returns {TDetails} The resolved details.
 */
function resolveDetails<TDetails>(
    providerDetailsConstructor: Constructor | undefined,
    type: Constructor<TDetails> | undefined,
    rawDetails: unknown,
    defaultDetails: unknown
): TDetails {
    if (!type) {
        // No type was asked for - behave exactly as before this hook could deserialize anything.
        return (rawDetails ?? defaultDetails ?? rawDetails) as TDetails;
    }

    if (rawDetails === null || rawDetails === undefined) {
        return (defaultDetails ?? rawDetails) as TDetails;
    }

    // The provider already deserialized this exact payload with this exact type - `instanceof` alone
    // cannot tell us that reliably (it is false across duplicate copies of @cratis/fundamentals, see
    // duplicateInstanceGuard.ts), but the constructor the provider recorded can.
    if (providerDetailsConstructor === type) {
        return rawDetails as TDetails;
    }

    if (typeof rawDetails !== 'object') {
        return deserializeIdentityDetails(type, rawDetails) as TDetails;
    }

    let byType = deserializedDetailsCache.get(rawDetails);
    if (!byType) {
        byType = new Map<Constructor, unknown>();
        deserializedDetailsCache.set(rawDetails, byType);
    }

    if (!byType.has(type)) {
        byType.set(type, deserializeIdentityDetails(type, rawDetails));
    }

    return byType.get(type) as TDetails;
}

/**
 * Hook to get the identity context with type-safe deserialization.
 * @param type Constructor for the details type to enable type-safe deserialization. Safe to pass even
 * when `<Arc detailsType={...}>`/`IdentityProviderProps.detailsType` already deserialized the identity
 * with this same type - that case is recognized and the existing instance is handed back rather than
 * deserialized a second time (which would be destructive, not merely wasteful).
 * @param defaultDetails Optional default details to use if the context is not set. Used as-is, never
 * deserialized - pass it already typed.
 * @returns An identity context with a {@link IIdentityContext.clearIdentity} action.
 * @remarks
 * Declared before the single-argument overload below on purpose. `TDetails` is unconstrained, so
 * literally any value - including a class reference - is assignable to the single-argument overload's
 * `defaultDetails?: TDetails` parameter; TypeScript resolves overloads in declaration order and stops
 * at the first match, so listing that overload first would make `useIdentity(SomeDetailsType)` silently
 * resolve to it instead, inferring `TDetails` as `typeof SomeDetailsType` rather than the instance type.
 */
export function useIdentity<TDetails = object>(type: Constructor<TDetails>, defaultDetails?: TDetails | undefined | null): IIdentityContext<TDetails>;

/**
 * Hook to get the identity context.
 * @param defaultDetails Optional default details to use if the context is not set.
 * @returns An identity context with a {@link IIdentityContext.clearIdentity} action.
 */
export function useIdentity<TDetails = object>(defaultDetails?: TDetails | undefined | null): IIdentityContext<TDetails>;

export function useIdentity<TDetails = object>(
    typeOrDefaultDetails?: Constructor<TDetails> | TDetails | undefined | null,
    defaultDetails?: TDetails | undefined | null
): IIdentityContext<TDetails> {
    const contextValue = React.useContext(IdentityProviderContext);
    const identity = contextValue.identity as IIdentityContext<TDetails>;

    // Determine if first argument is a Constructor or default details
    // Constructors are functions, but regular functions would be unusual here.
    // We rely on the type system and developer intent - if a function is passed,
    // it's expected to be a constructor class.
    const isConstructor = typeof typeOrDefaultDetails === 'function';
    const type = isConstructor ? typeOrDefaultDetails as Constructor<TDetails> : undefined;
    const actualDefaultDetails = isConstructor ? defaultDetails : typeOrDefaultDetails;

    const details = resolveDetails(contextValue.detailsConstructor, type, identity.details, actualDefaultDetails);

    return {
        ...identity,
        details,
        // Absent means nobody is fetching - a hand-built context rather than the provider - so the
        // identity on it is as resolved as it is ever going to get.
        isLoading: contextValue.isLoading ?? false,
        clearIdentity: contextValue.clearIdentity,
    };
}
