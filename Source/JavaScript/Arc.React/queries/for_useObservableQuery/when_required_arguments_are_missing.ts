// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook } from '@testing-library/react';
import { useObservableQuery } from '../useObservableQuery';
import {
    FakeObservableQueryWithRequiredParameters,
    FakeObservableQueryWithRequiredParametersArguments
} from './FakeObservableQueryWithRequiredParameters';
import { ArcContext, ArcConfiguration } from '../../ArcContext';

describe('when required arguments are missing', () => {
    beforeEach(() => {
        FakeObservableQueryWithRequiredParameters.reset();
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    const wrapper = ({ children }: { children: React.ReactNode }) => (
        React.createElement(ArcContext.Provider, { value: config }, children)
    );

    it('should subscribe only after all required arguments have values', async () => {
        const { rerender } = renderHook(
            ({ args }: { args: FakeObservableQueryWithRequiredParametersArguments }) =>
                useObservableQuery(FakeObservableQueryWithRequiredParameters, args),
            {
                wrapper,
                initialProps: { args: { userId: 'user-1', category: '' } }
            }
        );

        // Allow the initial effect to run before asserting no subscription.
        await new Promise(resolve => setTimeout(resolve, 0));
        FakeObservableQueryWithRequiredParameters.subscribeCallCount.should.equal(0);

        rerender({ args: { userId: 'user-1', category: 'cat-1' } });

        await new Promise(resolve => setTimeout(resolve, 0));
        FakeObservableQueryWithRequiredParameters.subscribeCallCount.should.equal(1);
    });
});