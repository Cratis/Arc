# Story Components Reference

This page documents all available components for building Storybook stories.

## StoryContainer

A container component for wrapping stories with consistent spacing and styling.

### Props

- `children`: React.ReactNode - The content to render
- `size`: 'sm' | 'md' | 'lg' | 'full' - Container size (default: 'md')
  - `sm`: 600px max width
  - `md`: 1200px max width
  - `lg`: 1400px max width
  - `full`: no max width
- `asCard`: boolean - Render as a card with background and border (default: false)
- `className`: string - Additional CSS classes

### Example

```tsx
import { StoryContainer } from '@cratis/arc.react/stories';

export const MyStory: Story = {
  render: () => (
    <StoryContainer size="sm" asCard>
      <h1>My Component</h1>
      <MyComponent />
    </StoryContainer>
  ),
};
```

### Usage Guidelines

- Use `size="sm"` for focused examples with narrow content
- Use `size="md"` (default) for most standard stories
- Use `size="lg"` for complex layouts or multiple columns
- Use `size="full"` for full-width demonstrations
- Add `asCard` when you want visual separation from the background

## StorySection

A section component for grouping related content with consistent vertical spacing.

### Props

- `children`: React.ReactNode - The content to render

### Example

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

### Usage Guidelines

- Use sections to separate distinct topics or examples within a story
- Sections provide consistent vertical spacing between content blocks
- Ideal for multi-part demonstrations

## StoryGrid

A responsive grid container for displaying multiple items.

### Props

- `children`: React.ReactNode - Grid items to render

### Example

```tsx
<StoryGrid>
  <div className="story-card">Card 1</div>
  <div className="story-card">Card 2</div>
  <div className="story-card">Card 3</div>
</StoryGrid>
```

### Usage Guidelines

- Use grids to showcase variations or multiple states of a component
- Grid automatically adjusts columns based on screen size
- Combine with `.story-card` class for card-style grid items

## StoryDivider

A visual divider between sections.

### Props

No props - it's a simple visual element.

### Example

```tsx
<StoryContainer>
  <p>First section</p>
  <StoryDivider />
  <p>Second section</p>
</StoryContainer>
```

### Usage Guidelines

- Use dividers to create clear visual separation
- Automatically adapts color to current theme
- Lighter alternative to separate sections

## StoryBadge

A status badge component for displaying colored labels.

### Props

- `children`: React.ReactNode - Badge content
- `variant`: 'success' | 'warning' | 'error' | 'info' - Badge color variant
- `className`: string - Additional CSS classes

### Example

```tsx
<p>
  Build Status: <StoryBadge variant="success">Passing</StoryBadge>
</p>
```

### Variants

- `success`: Green badge for positive states
- `warning`: Yellow/orange badge for cautionary states
- `error`: Red badge for error states
- `info`: Blue badge for informational states

### Usage Guidelines

- Use badges to highlight status, state, or metadata
- Keep badge content concise (1-2 words)
- Choose variants that match semantic meaning
