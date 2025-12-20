// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryFor } from '../../QueryFor';
import { QueryValidator } from '../../QueryValidator';
import { QueryResult } from '../../QueryResult';
import { ParameterDescriptor } from '../../reflection/ParameterDescriptor';
import '../../../validation/RuleBuilderExtensions';

interface TestParams {
    minAge: number;
}

class TestQueryValidator extends QueryValidator<TestParams> {
    constructor() {
        super();
        this.ruleFor(q => q.minAge).greaterThanOrEqual(0);
    }
}

class TestQuery extends QueryFor<string, TestParams> {
    readonly route = '/test';
    readonly validation = new TestQueryValidator();
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    defaultValue = '';
    parameters: TestParams = { minAge: -1 };

    get requiredRequestParameters(): string[] {
        return [];
    }
}

describe("when performing with client validation failing", () => {
    let query: TestQuery;
    let result: QueryResult<string>;

    beforeEach(async () => {
        query = new TestQuery(String);
        query.parameters = { minAge: -5 };

        result = await query.perform(query.parameters);
    });

    it("should_not_be_success", () => {
        result.isSuccess.should.be.false;
    });

    it("should_not_be_valid", () => {
        result.isValid.should.be.false;
    });

    it("should_have_validation_results", () => {
        result.validationResults.should.not.be.empty;
    });

    it("should_have_error_for_minAge", () => {
        result.validationResults.some(r => r.members.includes('minAge')).should.be.true;
    });
});
