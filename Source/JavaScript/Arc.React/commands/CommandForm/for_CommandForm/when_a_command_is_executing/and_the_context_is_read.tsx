// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../../given';
import { an_executing_command_form } from '../given/an_executing_command_form';

describe('when a command is executing and the context is read', given(an_executing_command_form, context => {
    beforeEach(async () => {
        context.renderForm();
        await context.beginExecute();
    });
    afterEach(() => context.cleanup());

    it('should report through the form context that it is executing', () => context.lastContextReading.should.be.true);
}));
