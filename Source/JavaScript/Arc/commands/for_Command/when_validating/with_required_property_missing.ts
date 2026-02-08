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

    it("should not call server", () => context.fetchStub.called.should.be.false);
    it("should return invalid result", () => result.isValid.should.be.false);
    it("should have validation error", () => result.validationResults.length.should.equal(1));
    it("should have error for missing property", () => result.validationResults[0].members[0].should.equal('someProperty'));
}));
