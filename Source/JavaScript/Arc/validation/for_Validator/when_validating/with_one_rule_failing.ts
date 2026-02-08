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

    it("should return one validation error", () => {
        results.should.have.lengthOf(1);
    });

    it("should indicate instance is not valid", () => {
        validator.isValidFor(instance).should.be.false;
    });

    it("should have error for name property", () => {
        results[0].members.should.include('name');
    });
});
