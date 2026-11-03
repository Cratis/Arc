// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_command } from '../given/a_command.js';

describe('when executing with an edit during submission', () => {
    let context: a_command;

    beforeEach(async () => {
        context = new a_command();
        context.command.setInitialValues({ someProperty: 'accepted' });
        context.command.someProperty = 'submitted';
        context.command.propertyChanged('someProperty');
        let complete!: (response: object) => void;
        context.fetchStub.returns(new Promise(resolve => { complete = resolve; }));

        const execution = context.command.execute();
        context.command.someProperty = 'unsent edit';
        context.command.propertyChanged('someProperty');
        complete({ status: 200, json: async () => ({ correlationId: '00000000-0000-0000-0000-000000000000', isSuccess: true, validationResults: [] }) });
        await execution;
    });

    afterEach(() => context.fetchHelper.restore());

    it('should send the submitted value', () => JSON.parse(context.fetchStub.firstCall.args[1].body).someProperty.should.equal('submitted'));
    it('should preserve the unsent edit', () => context.command.someProperty.should.equal('unsent edit'));
    it('should keep the command dirty', () => context.command.hasChanges.should.be.true);
    it('should revert to the submitted value', () => {
        context.command.revertChanges();
        context.command.someProperty.should.equal('submitted');
    });
});
