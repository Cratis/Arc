// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryFor } from '../../QueryFor';
import { QueryValidator } from '../../QueryValidator';
import { ParameterDescriptor } from '../../reflection/ParameterDescriptor';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import sinon from 'sinon';
import '../../../validation/RuleBuilderExtensions';

interface ITestResult {
    data: string;
}

interface ITestParams {
    searchTerm: string;
    minAge: number;
}

class TestQueryValidator extends QueryValidator<ITestParams> {
    constructor() {
        super();
        this.ruleFor((c: ITestParams) => c.searchTerm).minLength(3).withMessage('Search term must be at least 3 characters');
        this.ruleFor((c: ITestParams) => c.minAge).greaterThanOrEqual(0).withMessage('Age must be positive');
    }
}

class TestQuery extends QueryFor<ITestResult, ITestParams> {
    readonly route: string = '/api/test-query';
    readonly validation: QueryValidator = new TestQueryValidator();
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    defaultValue: ITestResult = { data: '' };
    parameters: ITestParams = { searchTerm: '', minAge: 0 };
    
    constructor() {
        super(Object);
    }
    
    get requiredRequestParameters(): string[] {
        return [];
    }
}

export class a_query_with_validator {
    query: TestQuery;
    fetchStub: sinon.SinonStub;
    fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    constructor() {
        this.query = new TestQuery();
        this.fetchHelper = createFetchHelper();
        this.fetchStub = this.fetchHelper.stubFetch();
    }
}
