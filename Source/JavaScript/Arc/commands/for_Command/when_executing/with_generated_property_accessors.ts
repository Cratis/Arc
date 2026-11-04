// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { GeneratedCommand } from '../GeneratedCommand.js';
import { createFetchHelper } from '../../../helpers/fetchHelper.js';

for (const value of ['submitted', '', 0, false, null, undefined]) {
    for (const isSuccess of [true, false]) {
        describe(`when executing generated property accessors with ${String(value)} and success is ${isSuccess}`, () => {
            let command: GeneratedCommand;
            let fetchHelper: ReturnType<typeof createFetchHelper>;
            let fetchStub: sinon.SinonStub;

            beforeEach(async () => {
                command = new GeneratedCommand();
                command.setOrigin('http://localhost');
                command.setInitialValues({ value: 'accepted' });
                command.value = value;
                fetchHelper = createFetchHelper();
                fetchStub = fetchHelper.stubFetch();
                fetchStub.resolves({ status: 200, json: async () => ({ correlationId: '00000000-0000-0000-0000-000000000000', isSuccess, validationResults: [] }) });
                await command.execute();
            });

            afterEach(() => fetchHelper.restore());

            it('should only accept successful submissions as clean', () => command.hasChanges.should.equal(!isSuccess));
            it('should revert to the last accepted value', () => {
                command.value = 'later edit';
                command.revertChanges();
                (command.value === (isSuccess ? value : 'accepted')).should.be.true;
            });
        });
    }
}

describe('when capturing current values through generated property accessors', () => {
    let command: GeneratedCommand;

    beforeEach(() => {
        command = new GeneratedCommand();
        command.value = false;
        command.setInitialValuesFromCurrentValues();
    });

    it('should capture the current value as clean', () => command.hasChanges.should.be.false);
    it('should revert through the setter to the captured value', () => {
        command.value = 'later edit';
        command.revertChanges();
        (command.value === false).should.be.true;
        command.hasChanges.should.be.false;
    });
});
