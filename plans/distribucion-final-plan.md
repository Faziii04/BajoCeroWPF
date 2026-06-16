# Distribucion UC + PWDistribucion Implementation Plan

## Overview
Repurpose the existing stub files into the distribution workflow:
- [`DistribucionUC`](ProyectoIntegradorNet10/UserControls/DistribucionUC.xaml) → main list with filter chips (Pedido, En ruta, Incidencia). Double-click opens [`PWDistribucion`](ProyectoIntegradorNet10/PopWindows/PWDistribucion.xaml)
- [`PWDistribucion`](ProyectoIntegradorNet10/PopWindows/PWDistribucion.xaml) → popup with two tabs: [`PedidosUC`](ProyectoIntegradorNet10/UserControls/PedidosUC.xaml) and [`IncidenciasUC`](ProyectoIntegradorNet10/UserControls/IncidenciasUC.xaml)
- Need new [`IncidenciaModel`](ProyectoIntegradorNet10/Models) and [`IncidenciaService`](ProyectoIntegradorNet10/Services) based on the `incidencia` table

## Data Model

### IncidenciaModel (new)
```csharp
public class IncidenciaModel {
    public int Id;
    public DateTime Fecha;
    public TimeSpan Hora;
    public string? Motivo;
    public bool Resuelto;
    public string? Notas;
    public int? VentaId;
    // Display helpers
    public string FechaDisplay => ...;
    public string HoraDisplay => ...;
}
```

### IncidenciaService (new)
- `GetAll()` 
- `GetByVentaId(int ventaId)`
- `Insert(IncidenciaModel)`
- `Update(IncidenciaModel)` — for marking resolved

## File Changes

### 1. [`DistribucionUC.xaml`](ProyectoIntegradorNet10/UserControls/DistribucionUC.xaml)
- Remove "Completado" filter chip from `pnlEstadoChips`
- Keep: Pedido, En ruta, Incidencia
- Right action panel stays (assign repartidor, change estado)

### 2. [`DistribucionUC.xaml.cs`](ProyectoIntegradorNet10/UserControls/DistribucionUC.xaml.cs)
- Remove `chipCompletado` references
- Update `SetActiveChip()` switch to only 3 estados
- On double-click or "Ver detalle" → open `PWDistribucion` with the selected venta

### 3. [`PWDistribucion.xaml`](ProyectoIntegradorNet10/PopWindows/PWDistribucion.xaml)
Full window with:
- Header: 🚚 icon + "Distribución — Venta #ID" + close button
- Tab selector: "Pedido" tab and "Incidencia" tab
- Tab 1 content: `<uc:PedidosUC x:Name="pedidosUC"/>`
- Tab 2 content: `<uc:IncidenciasUC x:Name="incidenciasUC"/>`
- Window style: NoResize, transparent, draggable

### 4. [`PWDistribucion.xaml.cs`](ProyectoIntegradorNet10/PopWindows/PWDistribucion.xaml.cs)
- Expose `EditVenta` property (like PWRoles pattern)
- Pass the venta to both child UCs on load
- Tab switching logic

### 5. [`PedidosUC.xaml`](ProyectoIntegradorNet10/UserControls/PedidosUC.xaml)
- **Fix class name** from `Pedido` → `PedidosUC`
- Shows selected venta info card (ID, cliente, total, estado)
- Shows product list (read-only) from `venta.Detalles`
- Action button: "✅ Marcar como Entregado" — sets `entregado = true`, updates `estado = "Completado"`, sets `fecha_entregado` and `hora_entregado`
- Note: Logically we're not editing venta info, only marking as delivered

### 6. [`PedidosUC.xaml.cs`](ProyectoIntegradorNet10/UserControls/PedidosUC.xaml.cs)
- Accept `VentaModel` via property
- Load and display venta details
- `BtnMarcarEntregado_Click` — update `entregado`, `estado`, `fecha_entregado`, `hora_entregado` via `VentasService.UpdateVenta`
- Emit `OnDataChanged` event so parent can refresh

### 7. [`IncidenciasUC.xaml`](ProyectoIntegradorNet10/UserControls/IncidenciasUC.xaml)
- Shows list of incidencias for the current venta
- Each row: fecha, hora, motivo, resolved status
- Add new incidencia: motivo textbox + notes textbox + button
- Mark incidencia as resolved: toggle button

### 8. [`IncidenciasUC.xaml.cs`](ProyectoIntegradorNet10/UserControls/IncidenciasUC.xaml.cs)
- Accept `VentaModel` via property
- Load incidencias by venta ID via `IncidenciaService.GetByVentaId()`
- `BtnAgregarIncidencia_Click` — insert new
- `BtnResolver_Click` — update `resuelto = true`

### 9. New files
- `Models/IncidenciaModel.cs`
- `Services/IncidenciaService.cs`

## Flow Diagram

```
DistribucionUC
├── Filter chips: [Pedido] [En ruta] [Incidencia]
├── Ventas DataGrid (delivery=true, filtered by estado)
└── Double-click → opens PWDistribucion(venta)
                    ├── Tab 1: PedidosUC
                    │   ├── Venta info (read-only)
                    │   ├── Product list (read-only)
                    │   └── [✅ Marcar como Entregado]
                    └── Tab 2: IncidenciasUC
                        ├── Incidencias list
                        ├── [➕ Nueva Incidencia]
                        └── [✅ Resolver]
```
