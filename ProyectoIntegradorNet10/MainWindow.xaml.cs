using System;
using System.Windows;
using ProyectoIntegradorNet10.Services;
using ProyectoIntegradorNet10.Windows;

namespace ProyectoIntegradorNet10
{
    public partial class MainWindow : Window
    {
        // TODO: Replace with PostgreSQL authentication later
        // private NpgsqlConnection? conex = DatabaseConnection.GetConnection();

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

        private void btnInicio_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text;
            string pass = passContrasenia.Password;

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Completa todos los campos");
                return;
            }

            // HARDCODED CREDENTIALS FOR DEVELOPMENT:
            //   admin / admin   -> administrador
            //   oper / oper     -> operario
            if (usuario == "admin" && pass == "admin")
            {
                MessageBox.Show("Bienvenido Administrador");
                AbrirDashboard(usuario, "administrador");
            }
            else if (usuario == "oper" && pass == "oper")
            {
                MessageBox.Show("Bienvenido Operario");
                AbrirDashboard(usuario, "operario");
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

            // ---------------------------------------------------------------
            // ORIGINAL SQL SERVER CODE (commented out for PostgreSQL migration):
            // try
            // {
            //     conex.Close();
            //     conex.Open();
            //     using (SqlCommand command = new SqlCommand(...))
            //     { ... }
            // }
            // catch (Exception ex) { ... }
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
    }
}
