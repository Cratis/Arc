// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from './given/a_query_for';
import { given } from '../../given';
import { Globals } from '../../Globals';
import { TestQueryFor } from './given/TestQueries';

describe('when constructing with globals api base path', given(a_query_for, () => {
    let originalApiBasePath: string | undefined;
    let query: TestQueryFor;

    beforeEach(() => {
        originalApiBasePath = Globals.apiBasePath;
        Globals.apiBasePath = '/custom/api';
        query = new TestQueryFor();
    });

    afterEach(() => {
        if (originalApiBasePath !== undefined) {
            Globals.apiBasePath = originalApiBasePath;
        }
    });

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    it('should initialize with globals api base path', () => (query as any)._apiBasePath.should.equal('/custom/api'));
}));
