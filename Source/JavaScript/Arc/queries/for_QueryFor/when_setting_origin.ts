// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from './given/a_query_for.js';
import { given } from '../../given.js';

describe('when setting origin', given(a_query_for, context => {
    const origin = 'https://api.example.com';

    beforeEach(() => {
        context.query.setOrigin(origin);
    });

    it('should set the origin', () => {
        // Access the private field through type assertion to test internal state
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        ((context.query as any)._origin).should.equal(origin);
    });
}));