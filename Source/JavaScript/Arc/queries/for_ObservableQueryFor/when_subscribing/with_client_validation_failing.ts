// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_with_validator } from '../given/an_observable_query_with_validator';
import { QueryResult } from '../../QueryResult';
import { given } from '../../../given';

describe('when subscribing with client validation failing', given(an_observable_query_with_validator, context => {
    let result: QueryResult<string>;

    beforeEach(() => {
        context.query.subscribe(_ => result = _, { minAge: -5 });
    });

    it('should push a result to the subscriber', () => {
        result.should.not.be.undefined;
    });

    it('should not be valid', () => {
        result.isValid.should.be.false;
    });

    it('should have error for minAge', () => {
        result.validationResults.some(_ => _.members.includes('minAge')).should.be.true;
    });
}));
