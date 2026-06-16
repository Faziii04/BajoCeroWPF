using System;
using System.Windows;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWDistribucion : Window
    {
        public VentaModel? EditVenta { get; set; }

        public event Action? OnDataChanged;

        public PWDistribucion()
        {
            InitializeComponent();
            this.Loaded += PWDistribucion_Loaded;
        }

        private void PWDistribucion_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWDistribucion_Loaded;

            if (EditVenta != null)
            {
                txtTitulo.Text = $"Distribución — Venta #{EditVenta.Id}";
                pedidosUC.SetVenta(EditVenta);
                incidenciasUC.SetVentaId(EditVenta.Id);

                pedidosUC.OnDataChanged += () => OnDataChanged?.Invoke();
                incidenciasUC.OnDataChanged += () => OnDataChanged?.Invoke();
            }
        }

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (pedidosUC == null || incidenciasUC == null) return;

            if (rbPedido.IsChecked == true)
            {
                pedidosUC.Visibility = Visibility.Visible;
                incidenciasUC.Visibility = Visibility.Collapsed;
            }
            else
            {
                pedidosUC.Visibility = Visibility.Collapsed;
                incidenciasUC.Visibility = Visibility.Visible;
            }
        }

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
