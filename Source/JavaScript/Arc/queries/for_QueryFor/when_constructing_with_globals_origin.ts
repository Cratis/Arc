// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from './given/a_query_for';
import { given } from '../../given';
import { Globals } from '../../Globals';
import { TestQueryFor } from './given/TestQueries';

describe('when constructing with globals origin', given(a_query_for, () => {
    let originalOrigin: string | undefined;
    let query: TestQueryFor;

    beforeEach(() => {
        originalOrigin = Globals.origin;
        Globals.origin = 'https://example.com';
        query = new TestQueryFor();
    });

    afterEach(() => {
        if (originalOrigin !== undefined) {
            Globals.origin = originalOrigin;
        }
    });

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    it('should initialize with globals origin', () => (query as any)._origin.should.equal('https://example.com'));
}));
