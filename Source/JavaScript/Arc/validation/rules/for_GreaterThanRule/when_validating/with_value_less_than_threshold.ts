// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GreaterThanRule } from '../../ComparisonRules';

interface TestType {
    value: number;
}

describe("when validating with value less than threshold", () => {
    let rule: GreaterThanRule<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof rule.validate>;

    beforeEach(() => {
        rule = new GreaterThanRule<TestType>(t => t.value, 18);
        instance = { value: 15 };
    
        results = rule.validate(instance, 'value');
    });

    it("should_return_one_validation_error", () => {
        results.should.have.lengthOf(1);
    });

    it("should_include_threshold_in_message", () => {
        results[0].message.should.include('18');
    });
});
