// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor } from '../../ObservableQueryFor.js';
import { QueryValidator } from '../../QueryValidator.js';
import { ParameterDescriptor } from '../../../reflection/ParameterDescriptor.js';
import '../../../validation/RuleBuilderExtensions.js';

export interface ITestParams {
    minAge: number;
}

export class TestObservableQueryValidator extends QueryValidator<ITestParams> {
    constructor() {
        super();
        this.ruleFor((c: ITestParams) => c.minAge).greaterThanOrEqual(0).withMessage('Age must be positive');
    }
}

export class TestObservableQuery extends ObservableQueryFor<string, ITestParams> {
    readonly route = '/test';
    readonly validation = new TestObservableQueryValidator();
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    readonly defaultValue = '';

    constructor() {
        super(String, false);
    }

    get requiredRequestParameters(): string[] {
        return [];
    }
}

export class an_observable_query_with_validator {
    query: TestObservableQuery;

    constructor() {
        this.query = new TestObservableQuery();
    }
}
