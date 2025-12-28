// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { LengthRule } from '../../LengthRules';

interface TestType {
    value: string;
}

describe("when validating with string within range", () => {
    let rule: LengthRule<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof rule.validate>;

    beforeEach(() => {
        rule = new LengthRule<TestType>(t => t.value, 3, 7);
        instance = { value: 'abcde' };
    
        results = rule.validate(instance, 'value');
    });

    it("should_return_no_validation_errors", () => {
        results.should.be.empty;
    });
});
