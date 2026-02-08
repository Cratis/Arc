// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NotEmptyRule } from '../../NotEmptyRule';

interface TestType {
    value: string;
}

describe("when validating with valid string", () => {
    let rule: NotEmptyRule<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof rule.validate>;

    beforeEach(() => {
        rule = new NotEmptyRule<TestType>(t => t.value);
        instance = { value: 'test' };
    
        results = rule.validate(instance, 'value');
    });

    it("should return no validation errors", () => {
        results.should.be.empty;
    });
});
