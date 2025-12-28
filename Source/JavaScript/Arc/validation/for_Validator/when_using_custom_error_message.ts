// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Validator } from '../Validator';
import '../RuleBuilderExtensions';

interface TestType {
    email: string;
}

describe("when using custom error message", () => {
    let validator: Validator<TestType>;
    let instance: TestType;
    let customMessage: string;
    let results: ReturnType<typeof validator.validate>;

    beforeEach(() => {
        customMessage = 'Email is required';
        validator = new Validator<TestType>();
        validator.ruleFor(t => t.email).notEmpty().withMessage(customMessage);
        
        instance = { email: '' };
    
        results = validator.validate(instance);
    });

    it("should_use_custom_error_message", () => {
        results[0].message.should.equal(customMessage);
    });
});
