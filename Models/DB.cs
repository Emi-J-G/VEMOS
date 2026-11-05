namespace VEMOS.Models;
using Microsoft.Data.SqlClient;
using Dapper;
using VEMOS.Models;
public class DB
{
    string _connectionString = @"Server=localhost\SQLEXPRESS;Database=VEMOSDB;Integrated Security=True;TrustServerCertificate=True;";

    private static readonly Dictionary<string, (double lat, double lng)> CoordenadasRestaurantes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["La Pampa"] = (-34.6037, -58.3816),
            ["El Ateneo"] = (-34.5952, -58.3938),
            ["Don Julio"] = (-34.5875, -58.4294),
            ["Café Tortoni"] = (-34.6035, -58.3739),
            ["Nomo"] = (-34.5947, -58.4022)
        };

    private void CargarRestriccionesUsuario(SqlConnection connection, Usuario usuario)
    {
        if (usuario == null)
            return;

        const string query = @"SELECT r.id, r.nombre, r.descripcion
                               FROM RestriccionesUsuarios ru
                               INNER JOIN Restricciones r ON ru.idRestriccion = r.id
                               WHERE ru.idUsuario = @idUsuario";

        var restricciones = connection.Query<Restriccion>(query, new { idUsuario = usuario.id }).ToList();
        usuario.restricciones = restricciones;
    }

    public List<Restriccion> GetRestriccionesByUsuario(string usuario)
    {
        string query = @"SELECT r.id, r.nombre, r.descripcion
                         FROM Restricciones r
                         INNER JOIN RestriccionesUsuarios ru ON r.id = ru.idRestriccion
                         INNER JOIN Usuarios u ON ru.idUsuario = u.id
                         WHERE u.username = @username";
        List<Restriccion> restricciones = new List<Restriccion>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            restricciones = connection.Query<Restriccion>(query, new { username = usuario }).ToList();
        }
        return restricciones;
    }

    public List<Restaurante> GetRestaurantes()
    {
        string query = @"
            SELECT r.id, r.nombre, r.direccion, r.descripcion, r.telefono, r.tipo, r.idUsuario
            FROM Restaurantes r
            ORDER BY r.nombre";

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            var restaurantes = connection.Query<Restaurante>(query).ToList();

            foreach (var restaurante in restaurantes)
            {
                var restricciones = connection.Query<string>(
                    @"SELECT re.nombre
                      FROM CertificadosRestaurantes cr
                      INNER JOIN Restricciones re ON re.id = cr.idRestriccion
                      WHERE cr.idRestaurante = @idRestaurante
                      ORDER BY re.nombre",
                    new { idRestaurante = restaurante.id }).ToList();

                restaurante.restricciones = restricciones;

                if (CoordenadasRestaurantes.TryGetValue(restaurante.nombre, out var ubicacion))
                {
                    restaurante.lat = ubicacion.lat;
                    restaurante.lng = ubicacion.lng;
                }
                else
                {
                    restaurante.lat = -34.6037;
                    restaurante.lng = -58.3816;
                }
            }

            return restaurantes;
        }
    }

    private void InsertarRestricciones(SqlConnection connection, int idUsuario, IEnumerable<Restriccion> restricciones)
    {
        if (restricciones == null)
            return;

        foreach (var restriccion in restricciones
            .Where(r => !string.IsNullOrWhiteSpace(r?.nombre))
            .Select(r => new Restriccion { nombre = r.nombre.Trim() })
            .GroupBy(r => r.nombre, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First()))
        {
            connection.Execute(
                @"IF NOT EXISTS (SELECT 1 FROM Restricciones WHERE nombre = @nombre)
                  INSERT INTO Restricciones (nombre, descripcion) VALUES (@nombre, @nombre);",
                new { nombre = restriccion.nombre });

            connection.Execute(
                @"INSERT INTO RestriccionesUsuarios (idRestriccion, idUsuario)
                  SELECT r.id, @idUsuario
                  FROM Restricciones r
                  WHERE r.nombre = @nombre
                    AND NOT EXISTS (
                        SELECT 1 FROM RestriccionesUsuarios ru
                        WHERE ru.idRestriccion = r.id AND ru.idUsuario = @idUsuario
                    );",
                new { idUsuario, nombre = restriccion.nombre });
        }
    }

    //agarra un objeto usuario y lo inserta en la base de datos, incluyendo sus restricciones.
    public void registrarUsuario(Usuario usuario) {
        string query = "exec insertUser @username, @nombre, @apellido, @email, @contrasena, @tipo";
        string rquery = @"insertRestricciones @idUsuario, @nombre";
        using (SqlConnection connection = new SqlConnection(_connectionString)) { 
            connection.Open();
            int nuevoId = connection.ExecuteScalar<int>(query, new { username = usuario.username, nombre = usuario.nombre, apellido = usuario.apellido, email = usuario.email, contrasena = usuario.contrasena, tipo = usuario.tipo });
            usuario.id = nuevoId;

            if (usuario.restricciones != null && usuario.restricciones.Any())
                InsertarRestricciones(connection, usuario.id, usuario.restricciones);
        }
    }

    //devuelve un usuario si existe en la base de datos, si no devuelve null.
    public Usuario validarUsuario(string email, string contraseña) {
        string query = "exec validateUserByEmNPass @pemail, @pass";
        string rquery = @"exec getRestriccionesByidUser @idUser";
        Usuario usuario = new Usuario();
        using (SqlConnection connection = new SqlConnection(_connectionString)) {
            connection.Open();//mantiene la bd abierta mientras se ejecuta el using, es mas eficiente bajo varios inserts.
            //busca el usuario
            usuario = connection.QueryFirstOrDefault<Usuario>(query, new { pemail = email, pass = contraseña });
            if (usuario != null) {
                //busca las restricciones del usuario
                List<Restriccion> restricciones = connection.Query<Restriccion>(rquery, new { idUser = usuario.id }).ToList();
                usuario.restricciones = restricciones;
            }
            return usuario;
        }
    }

    //trae un usuario si su user existe
    public Usuario nomUsuarioExiste(string username) {
        string query = "exec getUsuarioByUsername @pusername";
        Usuario usuario = new Usuario();
        using (SqlConnection connection = new SqlConnection(_connectionString)) {
            connection.Open();
            usuario = connection.QueryFirstOrDefault<Usuario>(query, new { pusername = username });
            if (usuario != null)
            {
                CargarRestriccionesUsuario(connection, usuario);
            }
            return usuario;
        }
    }
    public Usuario emUsuarioExiste(string email) {
        string query = "exec getUsuarioByEmail @pemail";
        Usuario usuario = new Usuario();
        using (SqlConnection connection = new SqlConnection(_connectionString)) {
            connection.Open();
            usuario = connection.QueryFirstOrDefault<Usuario>(query, new { pemail = email});
            if (usuario != null)
            {
                CargarRestriccionesUsuario(connection, usuario);
            }
            return usuario;
        }
    }

    public Usuario ObtenerUsuarioPorEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        string query = "exec getUsuarioByEmail @pemail";
        string rquery = @"exec getRestriccionesByidUser @idUser";

        Usuario usuario = null;

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            usuario = connection.QueryFirstOrDefault<Usuario>(query, new { pemail = email });

            if (usuario == null)
                return null;

            CargarRestriccionesUsuario(connection, usuario);
            return usuario;
        }
    }

    public void ActualizarUsuario(Usuario usuario)
        {
            const string query = @"UPDATE Usuarios
                                   SET username = @username,
                                       nombre = @nombre,
                                       apellido = @apellido,
                                       email = @email
                                   WHERE id = @id";

            using var connection = new SqlConnection(_connectionString);
            connection.Execute(query, new
            {
                usuario.id,
                usuario.username,
                usuario.nombre,
                usuario.apellido,
                usuario.email
            });
        }

    public List<Usuario> ObtenerAmigos(int idUsuario)
        {
            const string query = @"SELECT u.id, u.username, u.nombre, u.apellido, u.email, u.tipo
                                   FROM Amigos a
                                   INNER JOIN Usuarios u ON u.id =
                                       CASE WHEN a.idUsuarioA = @idUsuario THEN a.idUsuarioB ELSE a.idUsuarioA END
                                   WHERE a.idUsuarioA = @idUsuario OR a.idUsuarioB = @idUsuario
                                   ORDER BY u.nombre, u.apellido";

            using var connection = new SqlConnection(_connectionString);
            return connection.Query<Usuario>(query, new { idUsuario }).ToList();
        }

    public bool AgregarAmigo(int idUsuario, string identificador)
        {
            const string query = @"DECLARE @idAmigo int;
                                   SELECT TOP 1 @idAmigo = id
                                   FROM Usuarios
                                   WHERE username = @identificador OR email = @identificador;

                                   IF @idAmigo IS NULL OR @idAmigo = @idUsuario
                                       SELECT CAST(0 AS bit);
                                   ELSE IF EXISTS (
                                       SELECT 1 FROM Amigos
                                       WHERE (idUsuarioA = @idUsuario AND idUsuarioB = @idAmigo)
                                          OR (idUsuarioA = @idAmigo AND idUsuarioB = @idUsuario)
                                   )
                                       SELECT CAST(0 AS bit);
                                   ELSE
                                   BEGIN
                                       INSERT INTO Amigos (idUsuarioA, idUsuarioB)
                                       VALUES (@idUsuario, @idAmigo);
                                       SELECT CAST(1 AS bit);
                                   END";

            using var connection = new SqlConnection(_connectionString);
            return connection.ExecuteScalar<bool>(query, new { idUsuario, identificador = identificador.Trim() });
        }
    }
