// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import { given } from '../../../../given';
import { a_command_form_with_exception_feedback } from '../given/a_command_form_with_exception_feedback';

describe('when displaying exception feedback with a localized message', given(a_command_form_with_exception_feedback, context => {
    const localizedMessage = 'Une erreur inattendue est survenue. Veuillez réessayer.';

    beforeEach(async () => {
        await context.renderForm({ exceptionMessage: localizedMessage });
        await act(async () => context.setResult(context.exceptionResult()));
    });

    it('should use the localized message as the only fallback text', () => {
        context.rendered.getByRole('alert').textContent!.should.equal(localizedMessage);
        context.rendered.container.textContent!.should.equal(localizedMessage);
    });
}));
