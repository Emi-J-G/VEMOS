namespace VEMOS.Models;
using Microsoft.Data.SqlClient;
using Dapper;
using VEMOS.Models;
public class DB
{
    string _connectionString = @"Server=localhost;Database=VEMOSDB;Integrated Security=True;TrustServerCertificate=True;";

    private static readonly Dictionary<string, (double lat, double lng)> CoordenadasRestaurantes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["La Pampa"] = (-34.6037, -58.3816),
            ["El Ateneo"] = (-34.5952, -58.3938),
            ["Don Julio"] = (-34.5875, -58.4294),
            ["Café Tortoni"] = (-34.6035, -58.3739),
            ["Nomo"] = (-34.5947, -58.4022),
            ["La Cabrera"] = (-34.5888, -58.4258),
            ["Chila"] = (-34.6084, -58.3637),
            ["Pizzería Güerrín"] = (-34.6044, -58.3926),
            ["El Cuartito"] = (-34.6012, -58.3837),
            ["Sarkis"] = (-34.5948, -58.4382),
            ["La Brigada"] = (-34.6214, -58.3861)
        };

    private static readonly Dictionary<string, List<string>> RestriccionesRestaurantes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Don Julio"] = new() { "Celiaquía", "Diabetes" },
            ["La Cabrera"] = new() { "Celiaquía", "Lactosa" },
            ["Café Tortoni"] = new() { "Lactosa", "Vegetariano" },
            ["Chila"] = new() { "Vegano", "Vegetariano", "Celiaquía" },
            ["Pizzería Güerrín"] = new() { "Vegetariano" },
            ["El Cuartito"] = new() { "Vegetariano" },
            ["Sarkis"] = new() { "Vegano", "Vegetariano", "Halal" },
            ["La Brigada"] = new() { "Celiaquía", "Kosher" }
        };

    private static readonly (double lat, double lng)[] CoordenadasFallback =
    {
        (-34.5750, -58.4500),
        (-34.5920, -58.3500),
        (-34.6200, -58.4550),
        (-34.6450, -58.3850),
        (-34.5600, -58.4050),
        (-34.6150, -58.3300),
        (-34.6650, -58.4250),
        (-34.6000, -58.4750)
    };

    private static string NormalizarNombre(string? nombre)
    {
        return (nombre ?? string.Empty)
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(caracter => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(caracter) !=
                               System.Globalization.UnicodeCategory.NonSpacingMark)
            .Aggregate(new System.Text.StringBuilder(), (builder, caracter) => builder.Append(caracter))
            .ToString()
            .Trim()
            .ToLowerInvariant();
    }

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
            SELECT r.id, r.nombre, r.direccion, r.descripcion, r.telefono, r.tipo, r.idUsuario,
                   foto.foto
            FROM Restaurantes r
            OUTER APPLY (
                SELECT TOP 1 fr.foto
                FROM FotosRestaurantes fr
                WHERE fr.idRestaurante = r.id
                ORDER BY fr.id
            ) foto
            ORDER BY r.nombre";

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            var restaurantes = connection.Query<Restaurante>(query)
                .Where(restaurante => restaurante != null)
                .ToList();

            var fallbackIndex = 0;
            foreach (var restaurante in restaurantes)
            {
                if (restaurante == null)
                    continue;

                var restricciones = connection.Query<string>(
                    @"SELECT re.nombre
                      FROM CertificadosRestaurantes cr
                      INNER JOIN Restricciones re ON re.id = cr.idRestriccion
                      WHERE cr.idRestaurante = @idRestaurante
                      ORDER BY re.nombre",
                    new { idRestaurante = restaurante.id }).ToList();

                restaurante.restricciones = restricciones ?? new List<string>();
                if (restaurante.restricciones.Count == 0 &&
                    !string.IsNullOrWhiteSpace(restaurante.nombre) &&
                    RestriccionesRestaurantes.TryGetValue(restaurante.nombre, out var restriccionesIniciales))
                {
                    restaurante.restricciones = restriccionesIniciales;
                }

                var nombreNormalizado = NormalizarNombre(restaurante.nombre);
                var ubicacionEncontrada = false;
                var latitud = 0d;
                var longitud = 0d;

                foreach (var coordenada in CoordenadasRestaurantes)
                {
                    if (NormalizarNombre(coordenada.Key) != nombreNormalizado)
                        continue;

                    ubicacionEncontrada = true;
                    latitud = coordenada.Value.lat;
                    longitud = coordenada.Value.lng;
                    break;
                }

                if (ubicacionEncontrada)
                {
                    restaurante.lat = latitud;
                    restaurante.lng = longitud;
                }
                else
                {
                    var posicion = CoordenadasFallback[fallbackIndex % CoordenadasFallback.Length];
                    restaurante.lat = posicion.lat;
                    restaurante.lng = posicion.lng;
                    fallbackIndex++;
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
        string userQuery = "exec getUsuarioByEmail @pemail";
        Usuario usuarioRegistrado = new Usuario();
        using (SqlConnection connection = new SqlConnection(_connectionString)) { 
            connection.Open();
            connection.Execute(query, new { username = usuario.username, nombre = usuario.nombre, apellido = usuario.apellido, email = usuario.email, contrasena = usuario.contrasena, tipo = usuario.tipo });
            usuarioRegistrado = connection.QueryFirstOrDefault<Usuario>(userQuery, new { pemail = usuario.email });
            usuario.id = usuarioRegistrado.id;

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

    public List<Usuario> BuscarUsuarios(string texto, int idUsuario)
        {
            const string query = @"SELECT TOP 30 id, username, nombre, apellido, tipo, foto
                                   FROM Usuarios
                                   WHERE id <> @idUsuario
                                     AND (username LIKE '%' + @texto + '%'
                                          OR nombre LIKE '%' + @texto + '%'
                                          OR apellido LIKE '%' + @texto + '%'
                                          OR email LIKE '%' + @texto + '%')
                                   ORDER BY nombre, apellido, username";

            using var connection = new SqlConnection(_connectionString);
            return connection.Query<Usuario>(query, new { idUsuario, texto = texto.Trim() }).ToList();
        }

    public Usuario? ObtenerPerfilPublico(string identificador)
        {
            const string query = @"SELECT TOP 1 id, username, nombre, apellido, tipo, foto
                                   FROM Usuarios
                                   WHERE username = @identificador OR email = @identificador";

            using var connection = new SqlConnection(_connectionString);
            return connection.QueryFirstOrDefault<Usuario>(query, new { identificador });
        }

    public bool SonAmigos(int idUsuarioA, int idUsuarioB)
        {
            const string query = @"SELECT CASE WHEN EXISTS (
                                       SELECT 1 FROM Amigos
                                       WHERE (idUsuarioA = @idUsuarioA AND idUsuarioB = @idUsuarioB)
                                          OR (idUsuarioA = @idUsuarioB AND idUsuarioB = @idUsuarioA)
                                   ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END";

            using var connection = new SqlConnection(_connectionString);
            return connection.ExecuteScalar<bool>(query, new { idUsuarioA, idUsuarioB });
        }

    public List<Restaurante> ObtenerRestaurantesFavoritos(int idUsuario)
        {
            const string query = @"SELECT r.id, r.nombre, r.direccion, r.descripcion, r.telefono, r.tipo, r.idUsuario,
                                          foto.foto
                                   FROM RestaurantesFavoritos rf
                                   INNER JOIN Restaurantes r ON r.id = rf.idRestaurante
                                   OUTER APPLY (
                                       SELECT TOP 1 fr.foto
                                       FROM FotosRestaurantes fr
                                       WHERE fr.idRestaurante = r.id
                                       ORDER BY fr.id
                                   ) foto
                                   WHERE rf.idUsuario = @idUsuario
                                   ORDER BY r.nombre";

            using var connection = new SqlConnection(_connectionString);
            return connection.Query<Restaurante>(query, new { idUsuario }).ToList();
        }

    public List<Plato> ObtenerPlatosFavoritos(int idUsuario)
    {
        const string query = @"SELECT p.id, p.idRestaurante, p.nombre, p.descripcion, p.precio, p.tipo, p.foto
                               FROM PlatosFavoritos pf
                               INNER JOIN Platos p ON p.id = pf.idPlato
                               WHERE pf.idUsuario = @idUsuario
                               ORDER BY p.nombre";

        using var connection = new SqlConnection(_connectionString);
        return connection.Query<Plato>(query, new { idUsuario }).ToList();
    }

    public void ActualizarRestriccionesUsuario(int idUsuario, IEnumerable<string> nombres)
        {
            var restricciones = nombres
                .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
                .Select(nombre => nombre.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            connection.Execute(
                "DELETE FROM RestriccionesUsuarios WHERE idUsuario = @idUsuario",
                new { idUsuario });

            foreach (var nombre in restricciones)
            {
                connection.Execute(
                    @"IF NOT EXISTS (SELECT 1 FROM Restricciones WHERE nombre = @nombre)
                      INSERT INTO Restricciones (nombre, descripcion) VALUES (@nombre, @nombre);",
                    new { nombre });
                connection.Execute(
                    @"INSERT INTO RestriccionesUsuarios (idRestriccion, idUsuario)
                      SELECT id, @idUsuario FROM Restricciones WHERE nombre = @nombre;",
                    new { idUsuario, nombre });
            }
        }

    public HashSet<int> ObtenerIdsRestaurantesFavoritos(int idUsuario)
        {
            const string query = @"SELECT idRestaurante
                                   FROM RestaurantesFavoritos
                                   WHERE idUsuario = @idUsuario";

            using var connection = new SqlConnection(_connectionString);
            return connection.Query<int>(query, new { idUsuario }).ToHashSet();
        }

    public bool AlternarRestauranteFavorito(int idUsuario, int idRestaurante)
        {
            const string query = @"IF EXISTS (
                                       SELECT 1 FROM RestaurantesFavoritos
                                       WHERE idUsuario = @idUsuario AND idRestaurante = @idRestaurante
                                   )
                                   BEGIN
                                       DELETE FROM RestaurantesFavoritos
                                       WHERE idUsuario = @idUsuario AND idRestaurante = @idRestaurante;
                                       SELECT CAST(0 AS bit);
                                   END
                                   ELSE
                                   BEGIN
                                       INSERT INTO RestaurantesFavoritos (idUsuario, idRestaurante)
                                       VALUES (@idUsuario, @idRestaurante);
                                       SELECT CAST(1 AS bit);
                                   END";

            using var connection = new SqlConnection(_connectionString);
            return connection.ExecuteScalar<bool>(query, new { idUsuario, idRestaurante });
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
