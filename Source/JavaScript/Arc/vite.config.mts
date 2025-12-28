/// <reference types="vitest/config" />

import { defineConfig } from 'vitest/config';

/* @ts-ignore TypeScript complains that the imported vite.config is not under rootDir, but it works at runtime */
import { createConfig } from '../../../vite.base';

const config = createConfig();
export default defineConfig(config);
