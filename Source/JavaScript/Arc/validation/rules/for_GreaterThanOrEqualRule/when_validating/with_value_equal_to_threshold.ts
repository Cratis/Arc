// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GreaterThanOrEqualRule } from '../../ComparisonRules';

interface TestType {
    value: number;
}

describe("when validating with value equal to threshold", () => {
    let rule: GreaterThanOrEqualRule<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof rule.validate>;

    beforeEach(() => {
        rule = new GreaterThanOrEqualRule<TestType>(t => t.value, 18);
        instance = { value: 18 };
    
        results = rule.validate(instance, 'value');
    });

    it("should return no validation errors", () => {
        results.should.be.empty;
    });
});
