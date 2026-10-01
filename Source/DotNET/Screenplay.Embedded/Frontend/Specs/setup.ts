// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

afterEach(() => {
    cleanup();
    // The viewer remembers view options in storage; one spec's choices are not the next spec's starting point.
    window.localStorage.clear();
});
