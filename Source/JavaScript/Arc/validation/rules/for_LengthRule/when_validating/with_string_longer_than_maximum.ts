// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { LengthRule } from '../../LengthRules';

interface TestType {
    value: string;
}

describe("when validating with string longer than maximum", () => {
    let rule: LengthRule<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof rule.validate>;

    beforeEach(() => {
        rule = new LengthRule<TestType>(t => t.value, 3, 7);
        instance = { value: 'abcdefgh' };
    
        results = rule.validate(instance, 'value');
    });

    it("should_return_one_validation_error", () => {
        results.should.have.lengthOf(1);
    });
});
