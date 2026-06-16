# ProductosUC Redesign — Matching the Dashboard Aesthetic

## Current Problems
- TabControl looks old/standard WPF, not matching Dashboard's modern look
- Product cards lack the shadow/depth the Dashboard has
- Family filter feels tacked-on (separate search bar + chips row)
- No visual hierarchy — everything is flat
- The emoji 📦 placeholder feels cheap on cards

## Proposed New Layout

```
┌────────────────────────────────────────────────────────────┐
│ ┌──┐ Productos (42)                    [🔍] [+Nuevo][↻]  │
│ └──┘ Todos los productos                                  │  ← Dashboard-style header
├────────────────────────────────────────────────────────────┤
│ [📦  Todos]  [💧  Agua]  [🧃  Jugos]  [🥤  Bebidas] ...  │  ← Family pills (inline)
├────────────────────────────────────────────────────────────┤
│ ┌────────────┐  ┌────────────┐  ┌────────────┐           │
│ │ ┌────────┐ │  │ ┌────────┐ │  │ ┌────────┐ │           │
│ │ │  IMG   │ │  │ │  IMG   │ │  │ │  IMG   │ │           │
│ │ └────────┘ │  │ └────────┘ │  │ └────────┘ │           │
│ │ Nombre     │  │ Nombre     │  │ Nombre     │           │
│ │ Bs 10.00   │  │ Bs 10.00   │  │ Bs 10.00   │           │
│ │ Stock: 5   │  │ Stock: 5   │  │ Stock: 5   │           │
│ │ ● Activo   │  │ ● Activo   │  │ ● Inactivo │           │  ← Status dot
│ └────────────┘  └────────────┘  └────────────┘           │
└────────────────────────────────────────────────────────────┘
```

## Key Changes

### 1. Replace TabControl with Pill Toggle
Instead of the WPF TabControl, use a toggle row:
- "📦 Productos" | "📁 Familias" — two pill buttons at top-left
- Active pill gets accent background, inactive gets hover-brush
- This matches the Dashboard's filter chip pattern

### 2. Dashboard-Style Header
Like [`ReportesUC`](ProyectoIntegradorNet10/UserControls/ReportesUC.xaml:258):
- Icon box (34x34 accent rounded) + "Productos" title + count badge
- Search bar integrated in the header (right side)
- "Nuevo" and "Refrescar" buttons using ActionButton

### 3. Family Filters as Scrollable Pills
- Family chips scroll horizontally (already done, just polish)
- Active chip gets accent background, inactive gets transparent/hover
- "Todas" chip always first

### 4. Better Product Cards
- Card: 210x?, POscuro2 background, 12px radius, CardShadow
- Image section: 210x140 (landscape ratio, not square) — looks more modern
- Info section: Name (14px bold), Price (13px), Stock (12px), Status dot/badge
- Hover: CardHoverShadow + border highlight

### 5. Status as Colored Dot + Text
Instead of a badge, use:
- 🟢 Activo (SuccessBrush)
- 🔴 Inactivo (DangerBrush)

### 6. Empty State
Dashboard-style empty state with icon box + title + subtitle + CTA button

## Files to Modify
- `ProductosUC.xaml` — complete rewrite
- `ProductosUC.xaml.cs` — minor updates (keep logic, update UI references)

## What Stays the Same
- All data loading/search logic
- Popup integration
- Family chip click handling
