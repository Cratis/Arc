// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { useObservableQuery } from '../useObservableQuery.js';
import { FakeObservableQuery } from './FakeObservableQuery.js';
import { ArcContext, ArcConfiguration } from '../../ArcContext.js';
import { QueryInstanceCache } from '@cratis/arc/queries';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext.js';

describe('when initial data is default value', () => {
    let capturedData: unknown = undefined;
    let renderCount = 0;

    beforeEach(() => {
        FakeObservableQuery.reset();
        capturedData = undefined;
        renderCount = 0;
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    it('should have data as empty array for enumerable query before first response', () => {
        const TestComponent = () => {
            const [result] = useObservableQuery(FakeObservableQuery);
            renderCount++;

            if (renderCount === 1) {
                capturedData = result.data;
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

        (capturedData as unknown[]).should.deep.equal([]);
    });
});
