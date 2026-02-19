# Story Components

Common components and utilities for creating beautiful Storybook stories that automatically adapt to dark and light modes.

## Documentation

For complete documentation on using Story Components, see the [Story Components Documentation](../../../Documentation/frontend/react/stories/index.md).

The documentation covers:

- **Overview**: Motivation and benefits of using Story Components
- **Components**: Detailed reference for all available components
- **Styling**: CSS variables and utility classes
- **Theme Support**: Automatic dark/light mode adaptation

## Quick Start

```tsx
import { StoryContainer, StorySection, StoryBadge } from '@cratis/arc.react/stories';

export const MyStory: Story = {
  render: () => (
    <StoryContainer size="md" asCard>
      <h1>My Component</h1>
      <MyComponent />
    </StoryContainer>
  ),
};
```

## Examples

See `StoryContainer.stories.tsx` for comprehensive examples of all components and patterns.
