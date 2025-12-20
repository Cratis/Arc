// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EmailRule } from '../../EmailRule';

interface TestType {
    value: string;
}

describe("when validating with empty string", () => {
    let rule: EmailRule<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof rule.validate>;

    beforeEach(() => {
        rule = new EmailRule<TestType>(t => t.value);
        instance = { value: '' };
    
        results = rule.validate(instance, 'value');
    });

    it("should_return_no_validation_errors", () => {
        results.should.be.empty;
    });
});
