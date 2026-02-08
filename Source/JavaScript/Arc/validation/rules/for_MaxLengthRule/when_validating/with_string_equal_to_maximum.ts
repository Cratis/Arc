// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { MaxLengthRule } from '../../LengthRules';

interface TestType {
    value: string;
}

describe("when validating with string equal to maximum", () => {
    let rule: MaxLengthRule<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof rule.validate>;

    beforeEach(() => {
        rule = new MaxLengthRule<TestType>(t => t.value, 5);
        instance = { value: 'abcde' };
    
        results = rule.validate(instance, 'value');
    });

    it("should return no validation errors", () => {
        results.should.be.empty;
    });
});
