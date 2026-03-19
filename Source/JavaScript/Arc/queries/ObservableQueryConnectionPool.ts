// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryConnection } from './IObservableQueryConnection';
import { DataReceived } from './ObservableQueryConnection';

/**
 * Tracks the number of active subscribers assigned to each pool slot.
 */
interface PoolSlot {
    queryCount: number;
}

/**
 * Represents a pool of observable query connection factories distributed round-robin across N slots.
 *
 * Each call to {@link acquire} picks the slot with the fewest active queries (falling back to
 * round-robin when counts are equal) and creates a fresh connection for that subscriber via the
 * provided factory. The connection is a dedicated instance per subscriber; the pool only tracks
 * slot utilisation so future multiplexed transports (e.g. WebSocket hub) can share connections
 * across a fixed number of physical hub connections.
 *
 * When {@link size} is 1 (the default), all queries are counted against a single slot — the
 * natural behaviour for a single centralised hub connection.
 *
 * @template TDataType The type of data received from the connections.
 */
export class ObservableQueryConnectionPool<TDataType> {
    private readonly _slots: PoolSlot[];

    /**
     * Initialises a new {@link ObservableQueryConnectionPool}.
     * @param {number} size Number of logical slots (hub connections) in the pool.
     * @param {() => IObservableQueryConnection<TDataType>} factory Factory that produces a new connection instance per subscriber.
     */
    constructor(
        size: number,
        private readonly _factory: () => IObservableQueryConnection<TDataType>
    ) {
        this._slots = Array.from({ length: Math.max(1, size) }, () => ({ queryCount: 0 }));
    }

    /**
     * Picks the least-loaded pool slot, creates a fresh connection via the factory, and starts
     * delivering data to the provided callback.
     * @param {DataReceived<TDataType>} dataReceived Callback invoked for each message.
     * @param {object} queryArguments Arguments forwarded to the connection.
     * @returns A cleanup function that decrements the slot counter and disconnects the connection.
     */
    acquire(dataReceived: DataReceived<TDataType>, queryArguments?: object): () => void {
        // Pick the slot with the fewest active queries.
        const slot = this._slots.reduce((min, current) => current.queryCount < min.queryCount ? current : min, this._slots[0]);
        slot.queryCount++;

        // Each subscriber gets its own dedicated connection instance.
        const connection = this._factory();
        connection.connect(dataReceived, queryArguments);

        return () => {
            connection.disconnect();
            slot.queryCount = Math.max(0, slot.queryCount - 1);
        };
    }
}
