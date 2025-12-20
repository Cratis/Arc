// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { LessThanRule } from '../../ComparisonRules';

interface TestType {
    value: number;
}

describe("when validating with value less than threshold", () => {
    let rule: LessThanRule<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof rule.validate>;

    beforeEach(() => {
        rule = new LessThanRule<TestType>(t => t.value, 18);
        instance = { value: 15 };
    
        results = rule.validate(instance, 'value');
    });

    it("should_return_no_validation_errors", () => {
        results.should.be.empty;
    });
});
