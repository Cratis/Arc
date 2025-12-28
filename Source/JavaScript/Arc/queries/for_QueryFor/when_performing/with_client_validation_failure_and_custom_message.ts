// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_with_validator } from '../given/a_query_with_validator';
import { given } from '../../../given';
import { QueryResult } from '../../QueryResult';

describe("when performing with client validation failure and custom message", given(a_query_with_validator, context => {
    let result: QueryResult<object>;

    beforeEach(async () => {
        // Set invalid search term (too short)
        context.query.parameters = { searchTerm: 'ab', minAge: 25 };
        
        // Perform should validate client-side and not call the server
        result = await context.query.perform(context.query.parameters);
    });

    afterEach(() => {
        context.fetchStub.restore();
    });

    it("should_not_call_server", () => context.fetchStub.called.should.be.false);
    it("should_return_invalid_result", () => result.isValid.should.be.false);
    it("should_have_validation_error", () => result.validationResults.length.should.equal(1));
    it("should_have_custom_error_message", () => result.validationResults[0].message.should.equal('Search term must be at least 3 characters'));
    it("should_have_error_for_searchTerm_property", () => result.validationResults[0].members[0].should.equal('searchTerm'));
}));
