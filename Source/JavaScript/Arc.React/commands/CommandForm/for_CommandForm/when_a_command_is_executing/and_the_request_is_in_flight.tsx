// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../../given';
import { an_executing_command_form } from '../given/an_executing_command_form';

/**
 * Observed while the request is held open. Asserting only what the form says once the command has
 * settled cannot tell a form that reported execution from one that never did.
 */
describe('when a command is executing and the request is in flight', given(an_executing_command_form, context => {
    beforeEach(async () => {
        context.renderForm();
        await context.beginExecute();
    });
    afterEach(() => context.cleanup());

    it('should give the render prop that it is executing', () => context.lastRenderPropReading.should.be.true);
}));
