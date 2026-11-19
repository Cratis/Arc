// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { RequireAvailable } from '../RequireAvailable.js';

describe('when there is no availability provider', () => {
    it('should treat everything as available, because the host is not using availability', () => {
        const rendered = render(React.createElement(RequireAvailable, { part: 'Fleet.Quality.Scores', unavailable: 'gone', children: 'scores' }));
        rendered.container.textContent!.should.equal('scores');
        rendered.unmount();
    });
});
