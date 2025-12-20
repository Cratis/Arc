// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_command_with_validator } from '../given/a_command_with_validator';
import { given } from '../../../given';
import { CommandResult } from '../../CommandResult';

describe("when validating with client validation failure", given(a_command_with_validator, context => {
    let result: CommandResult<object>;

    beforeEach(async () => {
        context.command.email = '';
        context.command.age = 25;
        
        result = await context.command.validate();
    });

    afterEach(() => {
        context.fetchStub.restore();
    });

    it("should_not_call_server", () => context.fetchStub.called.should.be.false);
    it("should_return_invalid_result", () => result.isValid.should.be.false);
    it("should_have_validation_error", () => result.validationResults.length.should.equal(1));
    it("should_have_error_for_email_property", () => result.validationResults[0].members[0].should.equal('email'));
}));
