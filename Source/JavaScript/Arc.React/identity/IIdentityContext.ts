// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IIdentity } from '@cratis/arc/identity';

/**
 * Extends {@link IIdentity} with actions available from the React identity context.
 */
export interface IIdentityContext<TDetails = object> extends IIdentity<TDetails> {
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
