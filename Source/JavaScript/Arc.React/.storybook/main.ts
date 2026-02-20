// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { StorybookConfig } from "@storybook/react-vite";
import { dirname, join, resolve } from "path";
import type { InlineConfig } from 'vite';

const config: StorybookConfig = {
  stories: [
    "../commands/**/*.stories.@(js|jsx|mjs|ts|tsx)",
    "../stories/**/*.stories.@(js|jsx|mjs|ts|tsx)",
  ],
  addons: [
    getAbsolutePath("@storybook/addon-links"),
    getAbsolutePath("@storybook/addon-essentials"),
    getAbsolutePath("@storybook/addon-interactions"),
    getAbsolutePath("@storybook/addon-storysource"),
  ],
  framework: {
    name: getAbsolutePath("@storybook/react-vite"),
    options: {},
  },
  async viteFinal(config: InlineConfig) {
    // Resolve workspace dependencies for monorepo setup
    config.resolve = config.resolve || {};
    config.resolve.alias = {
      ...config.resolve.alias,
      '@cratis/arc/reflection': resolve(__dirname, '../../Arc/reflection/index.ts'),
      '@cratis/arc/commands': resolve(__dirname, '../../Arc/commands/index.ts'),
      '@cratis/arc/queries': resolve(__dirname, '../../Arc/queries/index.ts'),
      '@cratis/arc/validation': resolve(__dirname, '../../Arc/validation/index.ts'),
      '@cratis/arc/identity': resolve(__dirname, '../../Arc/identity/index.ts'),
      '@cratis/arc': resolve(__dirname, '../../Arc/index.ts'),
    };
    return config;
  },
};

export default config;

function getAbsolutePath(value: string): string {
  return dirname(require.resolve(join(value, "package.json")));
}
