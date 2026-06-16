using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class IncidenciasUC : UserControl
    {
        private int? _ventaId;
        private List<IncidenciaModel> _incidencias = new();

        public event Action? OnDataChanged;

        public IncidenciasUC()
        {
            InitializeComponent();
        }

        public void SetVentaId(int ventaId)
        {
            _ventaId = ventaId;
            _ = LoadIncidencias();
        }

        private async System.Threading.Tasks.Task LoadIncidencias()
        {
            if (_ventaId == null) return;
            try
            {
                _incidencias = await IncidenciaService.GetByVentaId(_ventaId.Value);
                RenderIncidencias();
                txtCount.Text = $"{_incidencias.Count} incidencia{(_incidencias.Count != 1 ? "s" : "")}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar incidencias: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenderIncidencias()
        {
            pnlIncidencias.Children.Clear();

            if (_incidencias.Count == 0)
            {
                pnlIncidencias.Children.Add(new TextBlock
                {
                    Text = "No hay incidencias registradas.",
                    FontSize = 12,
                    Foreground = TryFindResource("SectionLabelColor") as System.Windows.Media.Brush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                });
                return;
            }

            foreach (var inc in _incidencias)
            {
                var card = new Border
                {
                    Background = TryFindResource("POscuro2") as System.Windows.Media.Brush,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 6),
                };

                var innerGrid = new Grid();
                innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Status indicator
                var statusBorder = new Border
                {
                    Width = 8, Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = inc.Resuelto
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113))
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)),
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(statusBorder, 0);
                innerGrid.Children.Add(statusBorder);

                var infoStack = new StackPanel { VerticalAlignment = System.Windows.VerticalAlignment.Center };
                Grid.SetColumn(infoStack, 1);

                infoStack.Children.Add(new TextBlock
                {
                    Text = $"{inc.FechaDisplay} {inc.HoraDisplay} — {inc.Motivo}",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = TryFindResource("NavTextColor") as System.Windows.Media.Brush,
                    TextTrimming = System.Windows.TextTrimming.CharacterEllipsis
                });

                if (!string.IsNullOrEmpty(inc.Notas))
                {
                    infoStack.Children.Add(new TextBlock
                    {
                        Text = inc.Notas,
                        FontSize = 10,
                        Foreground = TryFindResource("SectionLabelColor") as System.Windows.Media.Brush,
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                }

                innerGrid.Children.Add(infoStack);

                if (!inc.Resuelto)
                {
                    var resolveBtn = new Button
                    {
                        Content = "✅ Resolver",
                        FontSize = 11,
                        Height = 28,
                        Padding = new Thickness(10, 0, 10, 0),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Tag = inc.Id,
                        Style = TryFindResource("ActionButton") as Style
                    };
                    resolveBtn.Click += BtnResolver_Click;
                    Grid.SetColumn(resolveBtn, 2);
                    innerGrid.Children.Add(resolveBtn);
                }
                else
                {
                    var resolvedText = new TextBlock
                    {
                        Text = "✅ Resuelto",
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)),
                        VerticalAlignment = System.Windows.VerticalAlignment.Center
                    };
                    Grid.SetColumn(resolvedText, 2);
                    innerGrid.Children.Add(resolvedText);
                }

                card.Child = innerGrid;
                pnlIncidencias.Children.Add(card);
            }
        }

        private async void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (_ventaId == null) return;

            string motivo = txtMotivo.Text.Trim();
            if (string.IsNullOrEmpty(motivo))
            {
                MessageBox.Show("Ingrese un motivo para la incidencia.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                btnAgregar.IsEnabled = false;
                btnAgregar.Content = "Guardando...";

                var incidencia = new IncidenciaModel
                {
                    Fecha = DateTime.Today,
                    Hora = DateTime.Now.TimeOfDay,
                    Motivo = motivo,
                    Resuelto = false,
                    Notas = string.IsNullOrWhiteSpace(txtNotas.Text) ? null : txtNotas.Text.Trim(),
                    VentaId = _ventaId.Value,
                };

                await IncidenciaService.Insert(incidencia);
                txtMotivo.Clear();
                txtNotas.Clear();
                await LoadIncidencias();
                OnDataChanged?.Invoke();

                btnAgregar.IsEnabled = true;
                btnAgregar.Content = "➕ Registrar Incidencia";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                btnAgregar.IsEnabled = true;
                btnAgregar.Content = "➕ Registrar Incidencia";
            }
        }

        private async void BtnResolver_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int incidenciaId)
            {
                try
                {
                    await IncidenciaService.Update(new IncidenciaModel
                    {
                        Id = incidenciaId,
                        Resuelto = true,
                        Notas = null
                    });
                    await LoadIncidencias();
                    OnDataChanged?.Invoke();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al resolver: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
