// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_command } from '../given/a_command';
import { given } from '../../../given';
import { CommandResult } from '../../CommandResult';

describe("when validating and fetch throws exception", given(a_command, context => {
    let result: CommandResult<object>;

    beforeEach(async () => {
        context.fetchStub.rejects(new Error('Network error'));

        result = await context.command.validate();
    });

    afterEach(() => {
        context.fetchStub.restore();
    });

    it("should return failed result", () => result.isSuccess.should.be.false);
    it("should include error message", () => result.exceptionMessages[0].should.contain('Error during validation call'));
    it("should have exception messages", () => (result.exceptionMessages.length > 0).should.be.true);
}));
