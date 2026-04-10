// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { useObservableQuery } from '../useObservableQuery';
import { FakeObservableQuery } from './FakeObservableQuery';
import { FakeSingleObservableQuery } from './FakeSingleObservableQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext';

describe('when initial data is never undefined or null', () => {
    let capturedResult: QueryResultWithState<unknown> | undefined = undefined;
    let renderCount = 0;

    beforeEach(() => {
        FakeObservableQuery.reset();
        FakeSingleObservableQuery.reset();
        capturedResult = undefined;
        renderCount = 0;
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    it('should have defined data for enumerable query on first render', () => {
        const TestComponent = () => {
            const [result] = useObservableQuery(FakeObservableQuery);
            renderCount++;

            if (renderCount === 1) {
                capturedResult = result as QueryResultWithState<unknown>;
            }

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

        (capturedResult!.data !== undefined).should.be.true;
        (capturedResult!.data !== null).should.be.true;
    });

    it('should have defined data for non-enumerable query on first render', () => {
        const TestComponent = () => {
            const [result] = useObservableQuery(FakeSingleObservableQuery);
            renderCount++;

            if (renderCount === 1) {
                capturedResult = result as QueryResultWithState<unknown>;
            }

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

        (capturedResult!.data !== undefined).should.be.true;
        (capturedResult!.data !== null).should.be.true;
    });
});
