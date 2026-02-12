// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { MaxLengthRule } from '../../LengthRules';

interface TestType {
    value: string;
}

describe("when validating with string longer than maximum", () => {
    let rule: MaxLengthRule<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof rule.validate>;

    beforeEach(() => {
        rule = new MaxLengthRule<TestType>(t => t.value, 5);
        instance = { value: 'abcdefg' };
    
        results = rule.validate(instance, 'value');
    });

    it("should return one validation error", () => {
        results.should.have.lengthOf(1);
    });

    it("should include maximum length in message", () => {
        results[0].message.should.include('5');
    });
});
