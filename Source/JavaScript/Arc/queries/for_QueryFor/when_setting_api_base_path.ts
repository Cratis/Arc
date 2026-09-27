// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from './given/a_query_for.js';
import { given } from '../../given.js';

describe('when setting api base path', given(a_query_for, context => {
    const apiBasePath = '/api/v1';

    beforeEach(() => {
        context.query.setApiBasePath(apiBasePath);
    });

    it('should set the api base path', () => {
        // Access the private field through type assertion to test internal state
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        ((context.query as any)._apiBasePath).should.equal(apiBasePath);
    });
}));