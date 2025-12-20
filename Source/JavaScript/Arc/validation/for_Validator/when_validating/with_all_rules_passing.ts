// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Validator } from '../../Validator';
import '../../RuleBuilderExtensions';

interface TestType {
    name: string;
    age: number;
}

describe("when validating with all rules passing", () => {
    let validator: Validator<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof validator.validate>;

    beforeEach(() => {
        validator = new Validator<TestType>();
        validator.ruleFor(t => t.name).notEmpty();
        validator.ruleFor(t => t.age).greaterThanOrEqual(18);
        
        instance = { name: 'John', age: 25 };
    
        results = validator.validate(instance);
    });

    it("should_return_no_validation_errors", () => {
        results.should.be.empty;
    });

    it("should_indicate_instance_is_valid", () => {
        validator.isValidFor(instance).should.be.true;
    });
});
