// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EmailRule } from '../../EmailRule';

interface TestType {
    value: string;
}

describe("when validating with invalid email", () => {
    let rule: EmailRule<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof rule.validate>;

    beforeEach(() => {
        rule = new EmailRule<TestType>(t => t.value);
        instance = { value: 'notanemail' };
    
        results = rule.validate(instance, 'value');
    });

    it("should_return_one_validation_error", () => {
        results.should.have.lengthOf(1);
    });
});
