// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NotEmptyRule } from '../../NotEmptyRule';
import { ValidationResultSeverity } from '../../../ValidationResultSeverity';

interface TestType {
    value: string | null;
}

describe("when validating with null value", () => {
    let rule: NotEmptyRule<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof rule.validate>;

    beforeEach(() => {
        rule = new NotEmptyRule<TestType>(t => t.value);
        instance = { value: null };
    
        results = rule.validate(instance, 'value');
    });

    it("should return one validation error", () => {
        results.should.have.lengthOf(1);
    });

    it("should have error severity", () => {
        results[0].severity.should.equal(ValidationResultSeverity.Error);
    });

    it("should have property name in message", () => {
        results[0].message.should.include('value');
    });

    it("should include property in members", () => {
        results[0].members.should.include('value');
    });
});
