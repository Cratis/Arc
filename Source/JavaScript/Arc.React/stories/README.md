# Story Components

Common components and utilities for creating beautiful Storybook stories that automatically adapt to dark and light modes.

## Overview

This folder contains reusable components for wrapping and styling your Storybook stories. All components automatically adapt to the current theme (dark/light mode) using CSS variables.

## Components

### StoryContainer

A container component for wrapping stories with consistent spacing and styling.

**Props:**
- `children`: React.ReactNode - The content to render
- `size`: 'sm' | 'md' | 'lg' | 'full' - Container size (default: 'md')
  - `sm`: 600px max width
  - `md`: 1200px max width
  - `lg`: 1400px max width
  - `full`: no max width
- `asCard`: boolean - Render as a card with background and border (default: false)
- `className`: string - Additional CSS classes

**Example:**
```tsx
import { StoryContainer } from '../stories';

export const MyStory: Story = {
  render: () => (
    <StoryContainer size="sm" asCard>
      <h1>My Component</h1>
      <MyComponent />
    </StoryContainer>
  ),
};
```

### StorySection

A section component for grouping related content with consistent vertical spacing.

**Example:**
```tsx
<StoryContainer>
  <StorySection>
    <h2>First Section</h2>
    <p>Content here</p>
  </StorySection>
  
  <StorySection>
    <h2>Second Section</h2>
    <p>More content</p>
  </StorySection>
</StoryContainer>
```

### StoryGrid

A responsive grid container for displaying multiple items.

**Example:**
```tsx
<StoryGrid>
  <div className="story-card">Card 1</div>
  <div className="story-card">Card 2</div>
  <div className="story-card">Card 3</div>
</StoryGrid>
```

### StoryDivider

A visual divider between sections.

**Example:**
```tsx
<StoryContainer>
  <p>First section</p>
  <StoryDivider />
  <p>Second section</p>
</StoryContainer>
```

### StoryBadge

A status badge component for displaying colored labels.

**Props:**
- `children`: React.ReactNode - Badge content
- `variant`: 'success' | 'warning' | 'error' | 'info' - Badge color variant
- `className`: string - Additional CSS classes

**Example:**
```tsx
<p>
  Build Status: <StoryBadge variant="success">Passing</StoryBadge>
</p>
```

## CSS Variables

The story styles use CSS variables that automatically update based on the current theme:

### Colors
- `--color-text`: Primary text color
- `--color-text-secondary`: Secondary text color
- `--color-text-muted`: Muted text color
- `--color-background`: Main background color
- `--color-background-secondary`: Secondary background (cards, inputs)
- `--color-background-tertiary`: Tertiary background (hover states)
- `--color-border`: Border color
- `--color-border-light`: Lighter border color
- `--color-primary`: Primary brand color
- `--color-primary-hover`: Primary hover state
- `--color-success`: Success state color
- `--color-warning`: Warning state color
- `--color-error`: Error state color
- `--color-info`: Info state color

### Spacing
- `--space-xs`: 0.25rem
- `--space-sm`: 0.5rem
- `--space-md`: 1rem
- `--space-lg`: 1.5rem
- `--space-xl`: 2rem
- `--space-2xl`: 3rem

### Other
- `--radius-sm`, `--radius-md`, `--radius-lg`: Border radius values
- `--shadow-sm`, `--shadow-md`, `--shadow-lg`: Box shadow values
- `--font-sans`, `--font-mono`: Font families

## Utility Classes

You can also use these CSS classes directly:

- `.story-container`, `.story-container-sm`, `.story-container-lg`: Container sizes
- `.story-section`: Section wrapper
- `.story-card`: Card styling
- `.story-grid`: Grid layout
- `.story-divider`: Horizontal divider
- `.story-badge`: Badge base style
- `.story-badge-success`, `.story-badge-warning`, `.story-badge-error`, `.story-badge-info`: Badge variants

## Automatic Theme Support

All styles automatically adapt to dark/light mode. The default theme is dark mode, but you can switch using the backgrounds toolbar in Storybook.

The CSS automatically detects the current theme and applies appropriate colors, ensuring your stories always look great in both modes.

## Examples

See `StoryContainer.stories.tsx` for comprehensive examples of all components and patterns.
