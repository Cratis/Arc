// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryConnection } from './IObservableQueryConnection.js';

/**
 * Represents a subscription for an observable query.
 */
export class ObservableQuerySubscription<TDataType> {

    /**
     * Initializes a new instance of the {@link ObservableQuerySubscription} class.
     * @param {ObservableQueryConnection<TDataType> _connection The connection to use.
     */
    constructor(private _connection: IObservableQueryConnection<TDataType>) {
    }

    /**
     * Unsubscribe subscription.
     */
    unsubscribe() {
        if (this._connection) {
            this._connection.disconnect();
            this._connection = undefined!;
        }
    }
}
