// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../../given';
import { an_executing_command_form } from '../given/an_executing_command_form';

/**
 * The only path that reaches the finally. Command.performRequest catches transport failures and
 * answers with a failed result, so a broken network never rejects out of execute(); a command that
 * rejects on its own - a throwing validator, an unbuildable payload, an override - still does. Without
 * the finally the form would report an execution that has already ended, forever.
 */
describe('when a command is executing and the command itself rejected', given(an_executing_command_form, context => {
    beforeEach(async () => {
        context.renderFormWithRejectingCommand();
        await context.executeAndSettle();
    });
    afterEach(() => context.cleanup());

    it('should stop reporting that it is executing', () => context.lastRenderPropReading.should.be.false);
}));
