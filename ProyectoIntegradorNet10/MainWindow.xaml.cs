using System;
using System.Windows;
using System.Windows.Input;
using ProyectoIntegradorNet10.Services;
using ProyectoIntegradorNet10.Windows;

namespace ProyectoIntegradorNet10
{
    public partial class MainWindow : Window
    {
        int intentos = 3;

        public MainWindow()
        {
            InitializeComponent();
            AplicarModo();
        }

        private void AplicarModo()
        {
            if (GlobalVars.ModoOscuro)
            {
                imgFondoNoche.Visibility = Visibility.Visible;
                imgFondoDia.Visibility = Visibility.Collapsed;
            }
            else
            {
                imgFondoDia.Visibility = Visibility.Visible;
                imgFondoNoche.Visibility = Visibility.Collapsed;
            }
        }

        private void btnModo_Click(object sender, RoutedEventArgs e)
        {
            GlobalVars.SwitchTheme();
            AplicarModo();
        }

        private async void btnInicio_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text.Trim();
            string pass = passContrasenia.Password;

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Completa todos los campos");
                return;
            }

            try
            {
                var empleado = await EmpleadoService.LoginAsync(usuario, pass);

                if (empleado != null)
                {
                    MessageBox.Show($"Bienvenido {empleado.Nombre} {empleado.Apellido}");
                    AbrirDashboard(empleado.Nombre, "usuario");
                }
                else
                {
                    intentos--;
                    MessageBox.Show("Usuario o contraseña incorrectos.\nIntentos restantes: " + intentos);

                    if (intentos == 0)
                    {
                        MessageBox.Show("Intentos máximos alcanzados");
                        Application.Current.Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AbrirDashboard(string usuario, string rol)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.UsuarioNombre = usuario;
            dashboard.UsuarioRol = rol;
            dashboard.Show();
            this.Close();
        }

        private void btnprueba_Click(object sender, RoutedEventArgs e)
        {
            AbrirDashboard("Admin", "administrador");
        }

        // --- Window control buttons ---

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // --- Window dragging (since WindowStyle="None") ---

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}
