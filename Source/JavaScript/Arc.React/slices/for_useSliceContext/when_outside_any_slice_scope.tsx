// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { SliceLocation } from '@cratis/arc/reflection';
import { ReportButton } from './ReportButton.js';

describe('when outside any slice scope', () => {
    let location: SliceLocation | undefined;

    beforeEach(() => {
        render(<ReportButton onRender={rendered => location = rendered} />).unmount();
    });

    it('should return the empty location', () => location!.should.equal(SliceLocation.empty));
});
