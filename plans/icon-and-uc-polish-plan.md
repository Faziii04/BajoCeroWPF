# Icon Fix + UC Polish Plan

## Problem 1: 🔍 Emoji Icons Don't Change With Theme
17 instances across all UCs. Emojis render at fixed colors by the OS.

**Fix:** Replace with Path geometry icon (magnifying glass) using `Stroke="{DynamicResource NavTextColor}"`

## Problem 2: UserControls Need Visual Polish
Stripping duplicate styles from remaining UCs so they use SharedStyles (with animations, consistent sizing).

**Files to process:**
- ClientesUC (already stripped via git restore, re-strip)
- VentasUC (FormTextBox, FormComboBox, ActionButton, etc.)
- Rest of UCs (same pattern)

## Approach
1. Bulk replace 🔍 with Path icon via targeted PowerShell
2. Strip duplicate styles from remaining UCs using apply_diff
