// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renderFieldWithErrors } from './given/a_field_rendered_with_errors.js';

describe('when field has distinct server and custom errors', () => {
    let errors: string[];

    beforeEach(() => {
        errors = renderFieldWithErrors('Server says no.', { requiredField: 'First Name is required.' });
    });

    it('should report both errors', () => errors.should.deep.equal(['Server says no.', 'First Name is required.']));
});
