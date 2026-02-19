# Story Components

## Overview

The Arc.React Story Components library provides a comprehensive set of reusable components and utilities designed specifically for creating beautiful, consistent Storybook stories. These components ensure that all your stories have a uniform look and feel while automatically adapting to both dark and light themes.

## Motivation

When building component libraries, documentation through Storybook is essential. However, creating stories that look professional and consistent across different themes can be time-consuming and repetitive. Common challenges include:

- **Inconsistent Spacing**: Different stories use different padding and margins, making the documentation feel disjointed
- **Theme Support**: Manual theme handling requires duplicate code and careful maintenance
- **Layout Patterns**: Repeating the same container, grid, and section patterns across multiple stories
- **Visual Hierarchy**: Ensuring consistent typography, colors, and visual structure
- **Developer Experience**: Spending more time on story presentation than on showcasing the actual components

## Solution

The Story Components library solves these problems by providing:

### Consistency

Pre-built components with standardized spacing, sizing, and layout patterns ensure every story looks cohesive. Developers can focus on showcasing their components rather than styling containers.

### Automatic Theme Adaptation

All components use CSS variables that automatically update based on the current Storybook theme. Switch between dark and light modes seamlessly without any additional code.

### Reusable Patterns

Common UI patterns like containers, grids, sections, badges, and dividers are available as ready-to-use components, reducing boilerplate and improving maintainability.

### Beautiful by Default

Thoughtfully designed defaults for colors, shadows, borders, and spacing mean your stories look professional out of the box.

## Getting Started

Import the components you need in your story files:

```tsx
import { StoryContainer, StorySection, StoryGrid, StoryBadge, StoryDivider } from '../stories';

export const MyStory: Story = {
  render: () => (
    <StoryContainer size="md" asCard>
      <h1>My Component</h1>
      <MyComponent />
    </StoryContainer>
  ),
};
```

## What's Included

- **[Components](components.md)**: Pre-built React components for common story layouts
- **[Styling](styling.md)**: CSS variables and utility classes for consistent theming
- **[Theme Support](theme-support.md)**: Automatic dark/light mode adaptation

## Examples

For comprehensive examples of all components and patterns, see the `StoryContainer.stories.tsx` file in the source code.
