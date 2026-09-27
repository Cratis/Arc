// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from '../given/a_query_for.js';
import { given } from '../../../given.js';
import { QueryResult } from '../../QueryResult.js';

describe('with invalid arguments', given(a_query_for, context => {
    let result: QueryResult<string>;

    beforeEach(async () => {
        // Call perform without required arguments
        result = await context.query.perform();
    });

    it('should return no success result', () => {
        result.isSuccess.should.be.false;
    });

    it('should return default value', () => {
        result.data.should.equal('');
    });

    it('should return result without data', () => {
        // For validation failures, the result doesn't have proper data
        result.data.should.equal('');
        result.isSuccess.should.be.false;
    });
}));