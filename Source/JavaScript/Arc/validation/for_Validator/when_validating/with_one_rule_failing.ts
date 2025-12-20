// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Validator } from '../../Validator';
import '../../RuleBuilderExtensions';

interface TestType {
    name: string;
    age: number;
}

describe("when validating with one rule failing", () => {
    let validator: Validator<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof validator.validate>;

    beforeEach(() => {
        validator = new Validator<TestType>();
        validator.ruleFor(t => t.name).notEmpty();
        validator.ruleFor(t => t.age).greaterThanOrEqual(18);
        
        instance = { name: '', age: 25 };
    
        results = validator.validate(instance);
    });

    it("should_return_one_validation_error", () => {
        results.should.have.lengthOf(1);
    });

    it("should_indicate_instance_is_not_valid", () => {
        validator.isValidFor(instance).should.be.false;
    });

    it("should_have_error_for_name_property", () => {
        results[0].members.should.include('name');
    });
});
