// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Validator } from '../Validator';
import '../RuleBuilderExtensions';

interface TestType {
    name: string;
}

describe("when defining multiple rules for same property", () => {
    let validator: Validator<TestType>;
    let instance: TestType;
    let results: ReturnType<typeof validator.validate>;

    beforeEach(() => {
        validator = new Validator<TestType>();
        validator.ruleFor(t => t.name).notEmpty().minLength(3);
        
        instance = { name: 'ab' };
    
        results = validator.validate(instance);
    });

    it("should return one validation error", () => {
        results.should.have.lengthOf(1);
    });

    it("should have error for min length", () => {
        results[0].message.should.include('3');
    });
});
