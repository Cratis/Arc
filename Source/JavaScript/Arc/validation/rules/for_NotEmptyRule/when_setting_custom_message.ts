// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NotEmptyRule } from '../NotEmptyRule';

interface TestType {
    value: string | null;
}

describe("when setting custom message", () => {
    let rule: NotEmptyRule<TestType>;
    let instance: TestType;
    let customMessage: string;
    let results: ReturnType<typeof rule.validate>;

    beforeEach(() => {
        customMessage = 'This field is required';
        rule = new NotEmptyRule<TestType>(t => t.value).withMessage(customMessage);
        instance = { value: null };
    
        results = rule.validate(instance, 'value');
    });

    it("should_use_custom_message", () => {
        results[0].message.should.equal(customMessage);
    });
});
