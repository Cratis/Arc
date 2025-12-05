// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from './given/an_observable_query_for';
import { given } from '../../given';
import { Globals } from '../../Globals';
import { TestObservableQuery } from './given/TestQueries';

describe('when constructing with globals origin', given(an_observable_query_for, () => {
    let originalOrigin: string | undefined;
    let query: TestObservableQuery;

    beforeEach(() => {
        originalOrigin = Globals.origin;
        Globals.origin = 'https://example.com';
        query = new TestObservableQuery();
    });

    afterEach(() => {
        if (originalOrigin !== undefined) {
            Globals.origin = originalOrigin;
        }
    });

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    it('should initialize with globals origin', () => (query as any)._origin.should.equal('https://example.com'));
}));
