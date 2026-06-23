# Reportes Tab — Practical Renovation

## What Changes

| Action | Report | Why |
|--------|--------|-----|
| **NEW ★** | Ingresos (bar chart: daily + monthly) | Core business metric — money earned over time |
| **FIX** | Productos Más Vendidos | `p.categoria` doesn't exist in DB → use `producto_familia` |
| **FIX** | Inventario Actual | Same bug |
| **NEW** | Producción | Production status, costs, completion rate |
| **NEW** | Órdenes de Compra | Purchase orders with supplier, status |
| **REMOVE** | Roles | Admin config, not a business report |
| **KEEP** | Ventas, Facturación, Clientes, Empleados, Vehículos, Depósitos | Already meaningful |

## Implementation Steps

### 1. Add OxyPlot.Wpf NuGet
```
dotnet add package OxyPlot.Wpf
```

### 2. ReportesService.cs — Fix + Add

Fix `GetProductosMasVendidos()` and `GetInventarioActual()` — replace `p.categoria` with JOIN through `miembros` → `producto_familia`.

Add 3 new methods:
- `GetIngresos(desde, hasta)` — daily + monthly revenue for bar chart + table
- `GetProduccion()` — production list with status/cost/items
- `GetOrdenesCompra(desde, hasta)` — purchase orders with supplier

### 3. ReportesUC.xaml — Redesign

- Add `xmlns:oxy` namespace
- Add `PlotView` above the DataGrid for bar charts
- Add 3 KPI cards (count, total, avg) that update per report
- Sidebar: remove Roles button, add Ingresos, Producción, Órdenes buttons

### 4. ReportesUC.xaml.cs — Wire

- `LoadReport()` switch: new cases for Ingresos, Producción, Órdenes
- `BuildRevenueChart()` — OxyPlot bar chart (daily bars, monthly toggle)
- KPI card updates per report type
- Export still works for all

## Sidebar (Final)

```
VENTAS
  📊 Ingresos         ★ bar chart
  💰 Ventas
  🧾 Facturación

INVENTARIO
  📦 Productos        fixed
  📋 Stock Actual     fixed
  🏭 Depósitos

PRODUCCIÓN
  🏗️ Producción       ★ new
  📑 Órdenes Compra   ★ new

CLIENTES
  👥 Clientes

LOGÍSTICA
  🚛 Vehículos

RRHH
  👤 Empleados
```

## SQL: Ingresos Diarios/Mensuales
```sql
-- Daily revenue (for bar chart)
SELECT v.fecha,
       SUM(vd.cantidad * vd.precio_unitario) AS total,
       COUNT(DISTINCT v.id) AS ventas_count
FROM venta v
JOIN venta_detalles vd ON vd.venta_id = v.id
WHERE v.fecha >= @desde AND v.fecha <= @hasta
GROUP BY v.fecha
ORDER BY v.fecha

-- Monthly revenue (aggregated)
SELECT DATE_TRUNC('month', v.fecha)::date AS mes,
       SUM(vd.cantidad * vd.precio_unitario) AS total,
       COUNT(DISTINCT v.id) AS ventas_count
FROM venta v
JOIN venta_detalles vd ON vd.venta_id = v.id
WHERE v.fecha >= @desde AND v.fecha <= @hasta
GROUP BY DATE_TRUNC('month', v.fecha)
ORDER BY mes
```

## SQL Fix: Productos Más Vendidos
```sql
SELECT p.id, p.nombre,
       COALESCE(pf.nombre, 'Sin familia') AS familia,
       COALESCE(SUM(vd.cantidad), 0) AS total_vendido,
       COALESCE(SUM(vd.cantidad * vd.precio_unitario), 0) AS total_ingresos
FROM producto p
LEFT JOIN miembros m ON m.producto_id = p.id
LEFT JOIN producto_familia pf ON pf.id = m.familia_id
LEFT JOIN venta_detalles vd ON vd.producto_id = p.id
LEFT JOIN venta v ON v.id = vd.venta_id
    AND (v.fecha >= @desde OR @desde IS NULL)
    AND (v.fecha <= @hasta OR @hasta IS NULL)
GROUP BY p.id, p.nombre, pf.nombre
ORDER BY total_vendido DESC LIMIT 50
```

## SQL Fix: Inventario Actual
```sql
SELECT p.id, p.nombre, p.estado,
       COALESCE(pf.nombre, 'Sin familia') AS familia,
       COALESCE(SUM(pd.cantidad), 0) AS stock_total,
       COUNT(DISTINCT pd.deposito_id) AS depositos_count
FROM producto p
LEFT JOIN miembros m ON m.producto_id = p.id
LEFT JOIN producto_familia pf ON pf.id = m.familia_id
LEFT JOIN producto_deposito pd ON pd.producto_id = p.id
GROUP BY p.id, p.nombre, p.estado, pf.nombre
ORDER BY p.nombre
```

## SQL: Producción
```sql
SELECT p.id, p.fecha_inicio, p.fecha_fin, p.estado, p.costo_total,
       COUNT(DISTINCT ip.insumo_id) AS insumos_count,
       COUNT(DISTINCT pp.producto_id) AS productos_count
FROM produccion p
LEFT JOIN insumo_produccion ip ON ip.produccion_id = p.id
LEFT JOIN produccion_producto pp ON pp.produccion_id = p.id
WHERE (p.fecha_inicio::date >= @desde OR @desde IS NULL)
  AND (p.fecha_inicio::date <= @hasta OR @hasta IS NULL)
GROUP BY p.id ORDER BY p.fecha_inicio DESC
```

## SQL: Órdenes de Compra
```sql
SELECT oc.id, oc.fecha_pedido, oc.estado, oc.monto,
       pr.nombre AS proveedor,
       COUNT(DISTINCT do.insumo_id) AS items_count
FROM orden_compra oc
LEFT JOIN proveedor pr ON pr.id = oc.proveedor
LEFT JOIN detalles_orden do ON do.orden_id = oc.id
WHERE (oc.fecha_pedido >= @desde OR @desde IS NULL)
  AND (oc.fecha_pedido <= @hasta OR @hasta IS NULL)
GROUP BY oc.id, pr.nombre ORDER BY oc.fecha_pedido DESC
```
