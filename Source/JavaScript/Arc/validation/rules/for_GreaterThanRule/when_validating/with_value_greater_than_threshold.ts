// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GreaterThanRule } from '../../ComparisonRules';

interface TestType {
    value: number;
}

describe("when validating with value greater than threshold", () => {
    let rule: GreaterThanRule<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof rule.validate>;

    beforeEach(() => {
        rule = new GreaterThanRule<TestType>(t => t.value, 18);
        instance = { value: 25 };
    
        results = rule.validate(instance, 'value');
    });

    it("should return no validation errors", () => {
        results.should.be.empty;
    });
});
