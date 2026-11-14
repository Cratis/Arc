// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useState } from 'react';
import { act, render, screen } from '@testing-library/react';
import { QueryResult } from '@cratis/arc/queries';
import { ArcContext } from '../../ArcContext.js';
import { clearSuspenseObservableQueryCache, useSuspenseObservableQuery } from '../useSuspenseObservableQuery.js';
import { FakeSuspenseObservableQuery, type FakeSuspenseObservableQueryResult } from './FakeSuspenseObservableQuery.js';

function response(name: string, isReady = true) {
    return new QueryResult<FakeSuspenseObservableQueryResult[]>({
        data: [{ id: '1', name }], isReady, isSuccess: true, isAuthorized: true,
        isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [],
        exceptionStackTrace: '', paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
    }, Object, true);
}

function Consumer({ id }: { id: string }) {
    const [result] = useSuspenseObservableQuery<FakeSuspenseObservableQueryResult[], FakeSuspenseObservableQuery>(FakeSuspenseObservableQuery);
    return React.createElement('div', { 'data-testid': id }, result.data[0]?.name);
}

describe.each([1, 2])('when reconnecting %s mounted suspense observable consumers', (consumerCount) => {
    let rendered: ReturnType<typeof render>;
    let reconnect: () => void;
    let removeSecondConsumer: () => void;

    beforeEach(async () => {
        clearSuspenseObservableQueryCache();
        FakeSuspenseObservableQuery.reset();

        function Harness() {
            const [queryVersion, setQueryVersion] = useState(0);
            const [showSecond, setShowSecond] = useState(consumerCount === 2);
            reconnect = () => setQueryVersion(version => version + 1);
            removeSecondConsumer = () => setShowSecond(false);
            return React.createElement(ArcContext.Provider, { value: { microservice: '', queryVersion, reconnectQueries: reconnect } },
                React.createElement(React.Suspense, { fallback: 'Loading...' },
                    React.createElement(Consumer, { id: 'first', key: 'first' }),
                    showSecond ? React.createElement(Consumer, { id: 'second', key: 'second' }) : null));
        }

        rendered = render(React.createElement(React.StrictMode, null, React.createElement(Harness)));
        await act(async () => {
            FakeSuspenseObservableQuery.subscribeCallbacks[0](response('Initial'));
        });
    });

    afterEach(async () => {
        await act(async () => { rendered.unmount(); });
        clearSuspenseObservableQueryCache();
    });

    it('should replace one shared subscription per version and keep listeners through repeated reconnects', async () => {
        await act(async () => { reconnect(); });
        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(2);
        FakeSuspenseObservableQuery.unsubscribedSubscriptionIndexes.should.deep.equal([0]);
        (screen.getByTestId('first').textContent ?? '').should.equal('Initial');
        (screen.queryByText('Loading...') === null).should.be.true;

        await act(async () => {
            FakeSuspenseObservableQuery.subscribeCallbacks[0](response('Late old data'));
            FakeSuspenseObservableQuery.subscribeCallbacks[1](response('Not ready', false));
        });
        (screen.getByTestId('first').textContent ?? '').should.equal('Initial');

        await act(async () => {
            FakeSuspenseObservableQuery.subscribeCallbacks[1](response('Reconnected'));
        });
        (screen.getByTestId('first').textContent ?? '').should.equal('Reconnected');
        if (consumerCount === 2) {
            (screen.getByTestId('second').textContent ?? '').should.equal('Reconnected');
        }

        await act(async () => { removeSecondConsumer(); });
        FakeSuspenseObservableQuery.unsubscribeCallCount.should.equal(1);
        await act(async () => { reconnect(); });
        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(3);
        FakeSuspenseObservableQuery.unsubscribedSubscriptionIndexes.should.deep.equal([0, 1]);
        await act(async () => {
            FakeSuspenseObservableQuery.subscribeCallbacks[1](response('Late replaced data'));
            FakeSuspenseObservableQuery.subscribeCallbacks[2](response('Reconnected again'));
        });
        (screen.getByTestId('first').textContent ?? '').should.equal('Reconnected again');

        await act(async () => { rendered.unmount(); });
        FakeSuspenseObservableQuery.unsubscribedSubscriptionIndexes.should.deep.equal([0, 1, 2]);
        await act(async () => {
            FakeSuspenseObservableQuery.subscribeCallbacks[2](response('After unmount'));
        });
        clearSuspenseObservableQueryCache();
        FakeSuspenseObservableQuery.unsubscribeCallCount.should.equal(3);
    });
});
