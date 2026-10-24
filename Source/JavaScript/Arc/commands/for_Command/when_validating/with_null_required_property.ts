// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { ValidationResultReason, ValidationResultReasonDetail } from '../../../validation/index.js';
import { CommandResult } from '../../CommandResult.js';
import { a_command } from '../given/a_command.js';

describe("when validating with null required property", given(class extends a_command {
    constructor() {
        super();
        this.command.someProperty = null as unknown as string;
    }
}, context => {
    let result: CommandResult<object>;

    beforeEach(async () => {
        result = await context.command.validate();
    });

    afterEach(() => {
        context.fetchStub.restore();
    });

    it("should not call server", () => context.fetchStub.called.should.be.false);
    it("should return invalid result", () => result.isValid.should.be.false);
    it("should have validation error", () => result.validationResults.length.should.equal(1));
    it("should have validation message for property", () => result.validationResults[0].message.should.equal('someProperty is required'));
    it("should have validation member for property", () => result.validationResults[0].members.should.deep.equal(['someProperty']));
    it("should identify a rule rejection", () => result.validationResults[0].reason.should.equal(ValidationResultReason.Rule));
    it("should identify a required property rejection", () => result.validationResults[0].reasonDetail!.should.equal(ValidationResultReasonDetail.Required));
}));
