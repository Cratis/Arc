// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act } from '@testing-library/react';
import sinon from 'sinon';
import { Globals } from '@cratis/arc';
import { ObservableQueryConnection, QueryInstanceCache, QueryResult, QueryTransportMethod } from '@cratis/arc/queries';
import { ArcContext } from '../../../ArcContext.js';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext.js';

export class an_observable_query_cache {
    cache = new QueryInstanceCache(0);
    connectStub!: sinon.SinonStub;
    disconnectStub!: sinon.SinonStub;
    private directMode = Globals.queryDirectMode;
    private transport = Globals.queryTransportMethod;

    readonly wrapper = ({ children }: { children: React.ReactNode }) =>
        React.createElement(QueryInstanceCacheContext.Provider, { value: this.cache },
            React.createElement(ArcContext.Provider, { value: { microservice: '', origin: 'https://example.com', apiBasePath: '' } }, children));

    establish(retentionMs = 0): void {
        this.cache = new QueryInstanceCache(retentionMs);
        this.directMode = Globals.queryDirectMode;
        this.transport = Globals.queryTransportMethod;
        Globals.queryDirectMode = true;
        Globals.queryTransportMethod = QueryTransportMethod.WebSocket;
        this.connectStub = sinon.stub(ObservableQueryConnection.prototype, 'connect');
        this.disconnectStub = sinon.stub(ObservableQueryConnection.prototype, 'disconnect');
    }

    cleanup(): void {
        this.cache.dispose();
        this.connectStub.restore();
        this.disconnectStub.restore();
        Globals.queryDirectMode = this.directMode;
        Globals.queryTransportMethod = this.transport;
    }

    async push(index: number, data: string[]): Promise<void> {
        const callback = this.connectStub.getCall(index).args[0] as (result: QueryResult<string[]>) => void;
        await act(async () => callback({
            data,
            hasData: data.length > 0,
            paging: { page: 0, size: 0, totalItems: data.length, totalPages: 1 },
            isSuccess: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            isReady: true
        } as QueryResult<string[]>));
    }
}
