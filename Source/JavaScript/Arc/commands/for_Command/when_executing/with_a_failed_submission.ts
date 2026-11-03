// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_command } from '../given/a_command.js';

for (const failure of ['server rejection', 'not found', 'network error', 'invalid JSON']) {
    describe(`when executing with ${failure}`, () => {
        let context: a_command;

        beforeEach(async () => {
            context = new a_command();
            context.command.setInitialValues({ someProperty: 'accepted' });
            context.command.someProperty = 'rejected';
            context.command.propertyChanged('someProperty');

            if (failure === 'network error') {
                context.fetchStub.rejects(new Error('offline'));
            } else {
                context.fetchStub.resolves({
                    status: failure === 'not found' ? 404 : 200,
                    json: async () => {
                        if (failure === 'invalid JSON') {
                            throw new Error('invalid JSON');
                        }
                        return { correlationId: '00000000-0000-0000-0000-000000000000', isSuccess: false, validationResults: [], exceptionMessages: ['rejected'] };
                    }
                });
            }
            await context.command.execute();
        });

        afterEach(() => context.fetchHelper.restore());

        it('should keep the command dirty', () => context.command.hasChanges.should.be.true);
        it('should revert to the last accepted values', () => {
            context.command.revertChanges();
            context.command.someProperty.should.equal('accepted');
        });
    });
}
