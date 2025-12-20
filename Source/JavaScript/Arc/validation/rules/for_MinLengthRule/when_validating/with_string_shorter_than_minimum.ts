// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { MinLengthRule } from '../../LengthRules';

interface TestType {
    value: string;
}

describe("when validating with string shorter than minimum", () => {
    let rule: MinLengthRule<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof rule.validate>;

    beforeEach(() => {
        rule = new MinLengthRule<TestType>(t => t.value, 5);
        instance = { value: 'abc' };
    
        results = rule.validate(instance, 'value');
    });

    it("should_return_one_validation_error", () => {
        results.should.have.lengthOf(1);
    });

    it("should_include_minimum_length_in_message", () => {
        results[0].message.should.include('5');
    });
});
