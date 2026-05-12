using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProyectoIntegradorNet10
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SqlConnection conex = DatabaseConnection.GetConnection();

        int intentos = 3;
        public MainWindow()
        {
            InitializeComponent();
            AplicarModo();
        }

        private void AplicarModo()
        {
            if (DatabaseConnection.Modo) // Oscuro
            {
                Resources["FondoOscuro"] = (Color)FindResource("FondoOscuro1");
                Resources["PanelOscuro"] = (Color)FindResource("PanelOscuro1");
                Resources["LetraOscuro"] = (Color)FindResource("LetraOscuro1");
                imgFondoNoche.Visibility = Visibility.Visible;
                imgFondoDia.Visibility = Visibility.Collapsed;
            }
            else // Claro
            {
                Resources["FondoOscuro"] = (Color)FindResource("FondoClaro");
                Resources["PanelOscuro"] = (Color)FindResource("PanelClaro");
                Resources["LetraOscuro"] = (Color)FindResource("LetraClaro");
                imgFondoDia.Visibility = Visibility.Visible;
                imgFondoNoche.Visibility = Visibility.Collapsed;
            }
        }

        private void btnModo_Click(object sender, RoutedEventArgs e)
        {
            DatabaseConnection.Modo = !DatabaseConnection.Modo;
            AplicarModo();
        }

        private void btnInicio_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text;
            string pass = passContrasenia.Password;

            if (usuario == "" || pass == "")
            {
                MessageBox.Show("Completa todos los campos");
                return;
            }

            try
            {
                conex.Close();
                conex.Open();

                using (SqlCommand command = new SqlCommand(
                    "SELECT rol FROM USUARIO WHERE usuario = @usuario AND contrasena = @pass AND estado = 'activo'", conex))
                {
                    command.Parameters.AddWithValue("@usuario", usuario);
                    command.Parameters.AddWithValue("@pass", pass);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string rol = reader["rol"].ToString();
                            conex.Close();

                            if (rol == "administrador")
                            {
                                MessageBox.Show("Bienvenido Administrador");

                                Dashboard admin = new Dashboard();
                                admin.UsuarioNombre = usuario;
                                admin.UsuarioRol = rol;
                                admin.Show();
                                this.Close();
                            }
                            else if (rol == "operario")
                            {
                                MessageBox.Show("Bienvenido Operario");

                                Dashboard op = new Dashboard();
                                op.UsuarioNombre = usuario;
                                op.UsuarioRol = rol;
                                op.Show();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Rol no reconocido");
                            }
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void btnprueba_Click(object sender, RoutedEventArgs e)
        {
            Dashboard admin = new Dashboard();
            admin.UsuarioNombre = "Admin";
            admin.UsuarioRol = "administrador";
            admin.Show();
            this.Close();
        }
    }
}