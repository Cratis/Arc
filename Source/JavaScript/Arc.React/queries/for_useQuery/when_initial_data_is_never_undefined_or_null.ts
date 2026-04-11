// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import sinon from 'sinon';
import { useQuery } from '../useQuery';
import { FakeQuery } from './FakeQuery';
import { FakeSingleQuery } from './FakeSingleQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext';

describe('when initial data is never undefined or null', () => {
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let capturedResult: QueryResultWithState<unknown> | undefined = undefined;
    let renderCount = 0;

    beforeEach(() => {
        fetchHelper = createFetchHelper();
        const fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: async () => ({ data: [], isSuccess: true, isAuthorized: true, isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [], exceptionStackTrace: '', paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 } })
        } as Response);
        capturedResult = undefined;
        renderCount = 0;
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    it('should have defined data for enumerable query on first render', () => {
        const TestComponent = () => {
            const [result] = useQuery(FakeQuery);
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
            const [result] = useQuery(FakeSingleQuery);
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
