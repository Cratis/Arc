// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, screen, act } from '@testing-library/react';
import sinon from 'sinon';
import { useSuspenseQuery, clearSuspenseQueryCache } from '../useSuspenseQuery';
import { FakeSuspenseQuery } from './FakeSuspenseQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when loading data successfully', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    beforeEach(() => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: async () => ({
                data: [{ id: '1', name: 'Test Item' }],
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
            })
        } as Response);
    });

    afterEach(() => {
        fetchHelper.restore();
        clearSuspenseQueryCache();
    });

    it('should initially show suspense fallback', async () => {
        const TestComponent = () => {
            const [result] = useSuspenseQuery(FakeSuspenseQuery);
            return React.createElement('div', { 'data-testid': 'content' }, (result.data as any[]).length.toString());
        };

        render(
            React.createElement(
                ArcContext.Provider,
                { value: config },
                React.createElement(
                    React.Suspense,
                    { fallback: React.createElement('div', { 'data-testid': 'loading' }, 'Loading...') },
                    React.createElement(TestComponent)
                )
            )
        );

        screen.getByTestId('loading');

        await act(async () => {});

        screen.getByTestId('content');
    });

    it('should render with data after query resolves', async () => {
        let capturedResult: any = null;

        const TestComponent = () => {
            const [result] = useSuspenseQuery(FakeSuspenseQuery);
            capturedResult = result;
            return React.createElement('div', { 'data-testid': 'content' }, 'loaded');
        };

        render(
            React.createElement(
                ArcContext.Provider,
                { value: config },
                React.createElement(
                    React.Suspense,
                    { fallback: React.createElement('div', null, 'Loading...') },
                    React.createElement(TestComponent)
                )
            )
        );

        await act(async () => {});

        screen.getByTestId('content');

        capturedResult!.should.not.be.null;
        capturedResult!.isPerforming.should.be.false;
        capturedResult!.isSuccess.should.be.true;
        capturedResult!.data.should.have.lengthOf(1);
        capturedResult!.data[0].id.should.equal('1');
    });
});


