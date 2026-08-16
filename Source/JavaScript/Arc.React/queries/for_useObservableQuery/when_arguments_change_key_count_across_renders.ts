// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook } from '@testing-library/react';
import { useObservableQuery } from '../useObservableQuery';
import {
    FakeObservableQueryWithOptionalArguments,
    FakeObservableQueryWithOptionalArgumentsArguments
} from './FakeObservableQueryWithOptionalArguments';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { QueryInstanceCache } from '@cratis/arc/queries';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext';

type Props = { args?: FakeObservableQueryWithOptionalArgumentsArguments };

describe('when arguments change key count across renders', () => {
    let cache: QueryInstanceCache;

    beforeEach(() => {
        FakeObservableQueryWithOptionalArguments.reset();
        cache = new QueryInstanceCache();
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    const wrapper = ({ children }: { children: React.ReactNode }) => (
        React.createElement(
            QueryInstanceCacheContext.Provider,
            { value: cache },
            React.createElement(ArcContext.Provider, { value: config }, children)
        )
    );

    it('should establish a new subscription when arguments go from undefined to an object', async () => {
        const { rerender } = renderHook(
            ({ args }: Props) => useObservableQuery(FakeObservableQueryWithOptionalArguments, args),
            {
                wrapper,
                initialProps: { args: undefined } as Props
            }
        );

        await new Promise(resolve => setTimeout(resolve, 0));
        FakeObservableQueryWithOptionalArguments.subscribedArgs.should.have.lengthOf(1);

        rerender({ args: { topicId: 'A' } });

        await new Promise(resolve => setTimeout(resolve, 0));
        FakeObservableQueryWithOptionalArguments.subscribedArgs.should.have.lengthOf(2);
        FakeObservableQueryWithOptionalArguments.subscribedArgs[1]!.should.deep.equal({ topicId: 'A' });
    });

    it('should keep establishing new subscriptions across further key-count round trips', async () => {
        const { rerender } = renderHook(
            ({ args }: Props) => useObservableQuery(FakeObservableQueryWithOptionalArguments, args),
            {
                wrapper,
                initialProps: { args: { topicId: 'A' } } as Props
            }
        );
        await new Promise(resolve => setTimeout(resolve, 0));

        rerender({ args: { topicId: 'B' } });
        await new Promise(resolve => setTimeout(resolve, 0));

        rerender({ args: undefined });
        await new Promise(resolve => setTimeout(resolve, 0));

        rerender({ args: { topicId: 'C' } });
        await new Promise(resolve => setTimeout(resolve, 0));

        FakeObservableQueryWithOptionalArguments.subscribedArgs.should.have.lengthOf(4);
        FakeObservableQueryWithOptionalArguments.subscribedArgs[3]!.should.deep.equal({ topicId: 'C' });
    });
});
