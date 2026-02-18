// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import * as chai from 'chai';
chai.should();
import * as chaiAsPromised from 'chai-as-promised';
import * as sinonChai from 'sinon-chai';
chai.use(sinonChai.default);
chai.use(chaiAsPromised.default);

import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

afterEach(() => {
    cleanup();
});
