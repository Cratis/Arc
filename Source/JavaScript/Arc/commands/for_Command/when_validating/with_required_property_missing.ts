// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given';
import { CommandResult } from '../../CommandResult';
import { a_command } from '../given/a_command';

describe("when validating with required property missing", given(class extends a_command {
    constructor() {
        super();
        this.command.someProperty = undefined as unknown as string;
    }
}, context => {
    let result: CommandResult<object>;

    beforeEach(async () => {
        result = await context.command.validate();
    });

    afterEach(() => {
        context.fetchStub.restore();
    });

    it("should_not_call_server", () => context.fetchStub.called.should.be.false);
    it("should_return_invalid_result", () => result.isValid.should.be.false);
    it("should_have_validation_error", () => result.validationResults.length.should.equal(1));
    it("should_have_error_for_missing_property", () => result.validationResults[0].members[0].should.equal('someProperty'));
}));
