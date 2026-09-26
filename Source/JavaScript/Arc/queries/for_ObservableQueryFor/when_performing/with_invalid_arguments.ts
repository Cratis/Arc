// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for.js';
import { given } from '../../../given.js';
import { QueryResult } from '../../QueryResult.js';

describe('when performing with invalid arguments', given(an_observable_query_for, context => {
    let result: QueryResult<string>;

    beforeEach(async () => {
        result = await context.query.perform({} as { id: string });
    });

    it('should return unsuccessful result', () => {
        result.isSuccess.should.be.false;
    });

    it('should return default value', () => {
        result.data.should.equal('');
    });
}));
