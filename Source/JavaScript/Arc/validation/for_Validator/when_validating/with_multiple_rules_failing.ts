// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Validator } from '../../Validator';
import '../../RuleBuilderExtensions';

interface TestType {
    name: string;
    age: number;
}

describe("when validating with multiple rules failing", () => {
    let validator: Validator<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof validator.validate>;

    beforeEach(() => {
        validator = new Validator<TestType>();
        validator.ruleFor(t => t.name).notEmpty();
        validator.ruleFor(t => t.age).greaterThanOrEqual(18);
        
        instance = { name: '', age: 15 };
    
        results = validator.validate(instance);
    });

    it("should_return_two_validation_errors", () => {
        results.should.have.lengthOf(2);
    });

    it("should_have_error_for_name_property", () => {
        results.some(r => r.members.includes('name')).should.be.true;
    });

    it("should_have_error_for_age_property", () => {
        results.some(r => r.members.includes('age')).should.be.true;
    });
});
