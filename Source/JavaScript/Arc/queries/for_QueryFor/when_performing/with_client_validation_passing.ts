// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryFor } from '../../QueryFor';
import { QueryValidator } from '../../QueryValidator';
import { ParameterDescriptor } from '../../../reflection/ParameterDescriptor';
import '../../../validation/RuleBuilderExtensions';
import sinon from 'sinon';

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
    parameters: TestParams = { minAge: 0 };

    get requiredRequestParameters(): string[] {
        return [];
    }
}

describe("when performing with client validation passing", () => {
    let query: TestQuery;
    let fetchStub: sinon.SinonStub;

    beforeEach(() => {
        query = new TestQuery(String);
        query.setOrigin('http://localhost');
        query.parameters = { minAge: 18 };

        fetchStub = sinon.stub(global, 'fetch').resolves({
            ok: true,
            json: async () => ({ data: 'result', isSuccess: true, isValid: true })
        } as Response);
    });

    afterEach(() => {
        fetchStub.restore();
    });

    it("should_call_server", async () => {
        await query.perform(query.parameters);
        fetchStub.should.have.been.called;
    });
});
