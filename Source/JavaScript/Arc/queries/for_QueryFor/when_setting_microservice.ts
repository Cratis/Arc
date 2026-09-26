// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from './given/a_query_for.js';
import { given } from '../../given.js';

describe('when setting microservice', given(a_query_for, context => {
    const microservice = 'my-microservice';

    beforeEach(() => {
        context.query.setMicroservice(microservice);
    });

    it('should set the microservice', () => {
        // Access the private field through type assertion to test internal state
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        ((context.query as any)._microservice).should.equal(microservice);
    });
}));