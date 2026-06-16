# Scrollbar Improvement Plan

## Current State
- `SharedStyles.xaml`: Bare styles — ScrollBar Width=6, Thumb with SeparatorColor + Opacity=0.6
- No hover/pressed states on the thumb
- No track background (hard to see scrollable area)
- ScrollBar buttons (arrows) visible by default — look dated

## Proposed Design

**Thumb:**
- Width: 8px (better click target)
- Rounded corners (4px)
- Hover: lighter color (GridRowHoverBrush)
- Pressed/dragging: even lighter (GridRowSelectedBrush)
- Opacity transition: idle 0.5 → hover 0.8

**Track:**
- Transparent by default
- On hover over scrollbar: subtle semi-transparent background (FOscuro1)
- This gives visual feedback that the scrollbar area is interactive

**Buttons (arrows):**
- Collapsed — modern UIs don't use them
- Saves space, cleaner look

**Placement:**
- Sidebar: appears over the dark background image, needs contrast
- All other UCs: appears over FOscuro1 background

## Implementation
1. Replace ScrollBar/Thumb styles in SharedStyles.xaml with a custom template
2. Add ScrollViewer style that hides the scrollbar when not needed (auto-hide)
