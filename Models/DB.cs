namespace VEMOS.Models;
using Microsoft.Data.SqlClient;
using Dapper;
using VEMOS.Models;
public class DB
{
    string _connectionString = @"Server=localhost;DataBase=NOMBRE_DE_LA_BASE_DE_DATOS;Integrated Security=True;TrustServerCertificate=True;";
    
    //agarra un objeto usuario y lo inserta en la base de datos, incluyendo sus restricciones.
    public void registrarUsuario(Usuario usuario) {
        using (SqlConnection connection = new SqlConnection(_connectionString)) { 
            connection.Open(); //mantiene la bd abierta mientras se ejecuta el using, es mas eficiente bajo varios inserts.

            //inserta user
            string query = "INSERT INTO Usuarios (username, nombre, apellido, email, contrasena) VALUES (@username, @nombre, @apellido, @email, @contrasena); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            int nuevoId = connection.ExecuteScalar<int>(query, usuario);
            usuario.id = nuevoId;

            //inserta restricciones
            if (usuario.restricciones != null) {
                foreach (var restriccion in usuario.restricciones) { 
                    string rquery = "INSERT INTO RestriccionesUsuario (idUsuario, nombre) VALUES (@idUsuario, @nombre)";
                    connection.Execute(rquery, new { idUsuario = usuario.id, nombre = restriccion.nombre });
                }
            }
        }
    }

    //devuelve un usuario si existe en la base de datos, sino devuelve null.
    public Usuario validarUsuario(string email, string contraseña) {
        using (SqlConnection connection = new SqlConnection(_connectionString)) {
            connection.Open();//mantiene la bd abierta mientras se ejecuta el using, es mas eficiente bajo varios inserts.

            //busca el usuario
            string query = "SELECT * FROM Usuarios WHERE email = @pemail AND contrasena = @pcontrasena";
            Usuario usuario = connection.QueryFirstOrDefault<Usuario>(query, new { pemail = email, pcontrasena = contraseña });
            if (usuario == null)
                return null;

            //busca las restricciones del usuario
            string rquery = @"SELECT r.id, r.nombre, r.descripcion FROM RestriccionesUsuario ru
                                JOIN Restricciones r ON ru.idRestriccion = r.id WHERE ru.idUsuario = @pidUsuario";
            List<Restriccion> restricciones = connection.Query<Restriccion>(rquery, new { pidUsuario = usuario.id }).ToList();
            usuario.restricciones = restricciones;

            return usuario;
        }
    }

    //trae un usuario si su user existe
    public Usuario usuarioExiste(string username){
        using (SqlConnection connection = new SqlConnection(_connectionString)) {
            string query = "SELECT * FROM Usuarios WHERE username = @pusername";
            Usuario usuario = connection.QueryFirstOrDefault<Usuario>(query, new { pusername = username });
            return usuario;
        }
    }
}
