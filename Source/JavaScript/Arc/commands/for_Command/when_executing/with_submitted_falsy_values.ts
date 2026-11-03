// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_command } from '../given/a_command.js';
import { PropertyDescriptor } from '../../../reflection/PropertyDescriptor.js';

for (const value of ['', 0, false, null, undefined]) {
    describe(`when executing with submitted value ${String(value)}`, () => {
        let context: a_command;

        beforeEach(async () => {
            context = new a_command();
            context.command.propertyDescriptors = [new PropertyDescriptor('someProperty', String, true)];
            context.command.setInitialValues({ someProperty: 'accepted' });
            context.command.someProperty = value as unknown as string;
            context.command.propertyChanged('someProperty');
            context.fetchStub.resolves({ status: 200, json: async () => ({ correlationId: '00000000-0000-0000-0000-000000000000', isSuccess: true, validationResults: [] }) });
            await context.command.execute();
        });

        afterEach(() => context.fetchHelper.restore());

        it('should accept the submitted value as clean', () => context.command.hasChanges.should.be.false);
        it('should revert to the submitted value', () => {
            context.command.someProperty = 'later edit';
            context.command.revertChanges();
            (context.command.someProperty === value).should.be.true;
        });
    });
}
