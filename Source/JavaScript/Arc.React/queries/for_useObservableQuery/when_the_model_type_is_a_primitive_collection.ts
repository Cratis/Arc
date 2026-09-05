// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act } from '@testing-library/react';
import { useObservableQuery } from '../useObservableQuery';
import { FakeStringObservableQuery } from './FakeStringObservableQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { QueryResult, QueryResultWithState, QueryInstanceCache } from '@cratis/arc/queries';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext';

describe('when the model type is a primitive collection', () => {
    let capturedResult: QueryResultWithState<string[]> | undefined;

    beforeEach(() => {
        FakeStringObservableQuery.reset();
        capturedResult = undefined;
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    const render_and_get_callback = () => {
        const TestComponent = () => {
            const [result] = useObservableQuery<string[], FakeStringObservableQuery>(FakeStringObservableQuery);
            capturedResult = result;
            return React.createElement('div', null, 'Test');
        };

        render(
            React.createElement(
                QueryInstanceCacheContext.Provider,
                { value: new QueryInstanceCache() },
                React.createElement(
                    ArcContext.Provider,
                    { value: config },
                    React.createElement(TestComponent)
                )
            )
        );

        return FakeStringObservableQuery.subscribeCallbacks[0];
    };

    const resultFor = (data: string[], changeSet?: object) => ({
        data,
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        paging: { page: 0, size: 0, totalItems: data.length, totalPages: 1 },
        ...(changeSet ? { changeSet } : {})
    } as unknown as QueryResult<string[]>);

    it('should keep the values of the initial payload', async () => {
        const callback = render_and_get_callback();

        await act(async () => {
            callback(resultFor(['first', 'second']));
        });

        capturedResult!.data.should.deep.equal(['first', 'second']);
    });

    it('should keep every item a primitive string', async () => {
        const callback = render_and_get_callback();

        await act(async () => {
            callback(resultFor(['first', 'second']));
        });

        capturedResult!.data.every(_ => typeof _ === 'string').should.be.true;
    });

    it('should keep the values of items added through a change set', async () => {
        const callback = render_and_get_callback();

        await act(async () => {
            callback(resultFor(['first']));
        });

        await act(async () => {
            callback(resultFor([], { added: ['second'], replaced: [], removed: [] }));
        });

        capturedResult!.data.should.deep.equal(['first', 'second']);
    });

    it('should remove an item removed through a change set', async () => {
        const callback = render_and_get_callback();

        await act(async () => {
            callback(resultFor(['first', 'second']));
        });

        await act(async () => {
            callback(resultFor([], { added: [], replaced: [], removed: ['first'] }));
        });

        capturedResult!.data.should.deep.equal(['second']);
    });
});
