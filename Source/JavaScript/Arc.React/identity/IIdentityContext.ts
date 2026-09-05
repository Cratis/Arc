// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IIdentity } from '@cratis/arc/identity';

/**
 * Extends {@link IIdentity} with actions available from the React identity context.
 */
export interface IIdentityContext<TDetails = object> extends IIdentity<TDetails> {
    /**
     * Whether the identity is still being resolved.
     *
     * {@link IIdentity.isSet} alone cannot answer "is there a user?" - it reads false both before the
     * first fetch has answered and after it answered with an anonymous identity. Only this flag
     * separates the two, so anything that gates rendering on who the user is must consult it first;
     * treating the pre-fetch state as anonymous makes a signed-in user flash the signed-out UI.
     *
     * This is a React lifecycle concern and deliberately does not exist on the framework-agnostic
     * {@link IIdentity}.
     */
    isLoading: boolean;

    /**
     * Clears the identity cookie and resets the identity state to not-set.
     *
     * Call this when the user logs out to ensure subsequent requests and WebSocket
     * connections do not carry stale credentials. Typically followed by
     * {@link ArcConfiguration.reconnectQueries} to re-establish query connections
     * without the old credentials.
     */
    clearIdentity: () => void;
}
