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

    it("should_return_one_validation_error", () => {
        results.should.have.lengthOf(1);
    });

    it("should_have_error_severity", () => {
        results[0].severity.should.equal(ValidationResultSeverity.Error);
    });

    it("should_have_property_name_in_message", () => {
        results[0].message.should.include('value');
    });

    it("should_include_property_in_members", () => {
        results[0].members.should.include('value');
    });
});
