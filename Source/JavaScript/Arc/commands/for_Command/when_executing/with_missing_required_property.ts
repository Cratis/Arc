// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given';
import { CommandResult } from '../../CommandResult';
import { a_command } from '../given/a_command';

describe("when executing with missing required property", given(class extends a_command {
    constructor() {
        super();
        // Intentionally NOT setting someProperty to test validation
        this.command.someProperty = undefined as unknown as string;
    }
}, context => {
    let result: CommandResult<object>;

    beforeEach(async () => {
        result = await context.command.execute();
    });

    afterEach(() => {
        context.fetchStub.restore();
    });

    it("should not call fetch", () => context.fetchStub.called.should.be.false);
    it("should return failed result", () => result.isSuccess.should.be.false);
    it("should return invalid result", () => result.isValid.should.be.false);
    it("should return authorized result", () => result.isAuthorized.should.be.true);
    it("should have validation results", () => result.validationResults.length.should.equal(1));
    it("should have validation message for property", () => result.validationResults[0].message.should.equal('someProperty is required'));
    it("should have validation member for property", () => result.validationResults[0].members[0].should.equal('someProperty'));
}));
