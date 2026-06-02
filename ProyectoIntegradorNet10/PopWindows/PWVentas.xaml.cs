using System;
using System.Windows;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWVentas : Window
    {
        /// <summary>
        /// Raised when the popup saves data so the parent can refresh.
        /// </summary>
        public event Action? OnDataChanged;

        /// <summary>
        /// If set, opens in edit mode for this venta.
        /// </summary>
        public VentaModel? EditVenta
        {
            get => _editVenta;
            set
            {
                _editVenta = value;
                ventasUC.EditVenta = value;
                pagosUC.EditVenta = value;
            }
        }
        private VentaModel? _editVenta;

        public PWVentas()
        {
            InitializeComponent();

            // Wire up child UC events
            ventasUC.OnDataChanged += () => OnDataChanged?.Invoke();
            pagosUC.OnDataChanged += () => OnDataChanged?.Invoke();

            this.Loaded += PWVentas_Loaded;
        }

        private void PWVentas_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWVentas_Loaded;

            if (EditVenta != null)
            {
                txtTitulo.Text = $"Venta #{EditVenta.Id}";
            }
        }

        // ──────────── TAB SWITCHING ────────────

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (ventasUC == null || pagosUC == null) return;

            if (rbDetalles.IsChecked == true)
            {
                ventasUC.Visibility = Visibility.Visible;
                pagosUC.Visibility = Visibility.Collapsed;
            }
            else
            {
                ventasUC.Visibility = Visibility.Collapsed;
                pagosUC.Visibility = Visibility.Visible;
                pagosUC.LoadPagos();
            }
        }

        // ──────────── CLOSE ────────────

        private void BtnCerrar_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
