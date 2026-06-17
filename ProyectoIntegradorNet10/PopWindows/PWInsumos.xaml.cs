using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWInsumos : Window
    {
        public event Action? OnDataChanged;
        public int EditInsumoId { get; set; }
        private bool _isEditing;

        public PWInsumos() { InitializeComponent(); this.Loaded += PWInsumos_Loaded; }

        private async void PWInsumos_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWInsumos_Loaded;
            if (EditInsumoId > 0)
            {
                txtTitulo.Text = "Editar Insumo"; _isEditing = true; btnEliminar.IsEnabled = true;
                var m = await InsumosService.GetById(EditInsumoId);
                if (m != null) { txtNombre.Text = m.Nombre; txtDescripcion.Text = m.Descripcion ?? ""; txtUnidadMedida.Text = m.UnidadMedida ?? ""; txtPrecioUnitario.Text = m.PrecioUnitario?.ToString("N2") ?? ""; txtCantidadStock.Text = m.CantidadStock?.ToString("N0") ?? ""; }
            }
            else txtNombre.Focus();
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text)) { MessageBox.Show("El nombre es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning); txtNombre.Focus(); return; }
            try
            {
                btnGuardar.IsEnabled = false;
                decimal? pu = null; if (decimal.TryParse(txtPrecioUnitario.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var pp)) pu = pp;
                decimal? cs = null; if (decimal.TryParse(txtCantidadStock.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cc)) cs = cc;
                var m = new InsumoModel { Nombre = txtNombre.Text.Trim(), Descripcion = string.IsNullOrWhiteSpace(txtDescripcion.Text) ? null : txtDescripcion.Text.Trim(), UnidadMedida = string.IsNullOrWhiteSpace(txtUnidadMedida.Text) ? null : txtUnidadMedida.Text.Trim(), PrecioUnitario = pu, CantidadStock = cs };
                if (_isEditing) { m.Id = EditInsumoId; await InsumosService.Update(m); MessageBox.Show("Insumo actualizado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information); }
                else { await InsumosService.Insert(m); MessageBox.Show("Insumo creado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information); }
                OnDataChanged?.Invoke(); this.Close();
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); btnGuardar.IsEnabled = true; }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditing) return;
            if (MessageBox.Show($"¿Eliminar insumo \"{txtNombre.Text}\"?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try { await InsumosService.Delete(EditInsumoId); MessageBox.Show("Insumo eliminado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information); OnDataChanged?.Invoke(); this.Close(); }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();
        private void BtnCerrar_Click(object sender, MouseButtonEventArgs e) => this.Close();
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); }
    }
}
