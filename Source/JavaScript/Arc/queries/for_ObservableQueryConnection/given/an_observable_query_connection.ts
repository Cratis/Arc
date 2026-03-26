// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryConnection } from '../../ObservableQueryConnection';
import * as sinon from 'sinon';

export class an_observable_query_connection {
    connection: ObservableQueryConnection<string>;
    url: URL;
    microservice: string;
    mockWebSocket: Record<string, unknown>;

    constructor() {
        this.url = new URL('https://example.com/api/test');
        this.microservice = 'test-microservice';

        // Create a plain mock WebSocket object
        this.mockWebSocket = {
            onopen: null,
            onclose: null,
            onerror: null,
            onmessage: null,
            close: sinon.stub(),
            send: sinon.stub(),
            readyState: WebSocket.OPEN,
        };

        // Create connection with a short ping interval for testing (100ms)
        this.connection = new ObservableQueryConnection<string>(this.url, this.microservice, 100);
    }
}
