namespace VEMOS.Models;

using Microsoft.Data.SqlClient;
using Dapper;

public class DB
{
    string _connectionString = "Server=localhost;Database=VEMOSDB;Integrated Security=True;TrustServerCertificate=True;";

    private void CargarRestriccionesUsuario(Usuario usuario)
    {
        if (usuario == null)
            return;
        const string query = "exec CargarRestriccionesUsuario @idUsuario";
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            List<Restriccion> restricciones = connection.Query<Restriccion>(query, new { idUsuario = usuario.id }).ToList();
            usuario.restricciones = restricciones;
        }
    }

    public List<Restriccion> GetRestricciones() {
        const string query = "exec getRestricciones";
        List<Restriccion> restricciones = new List<Restriccion>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            restricciones = connection.Query<Restriccion>(query).ToList();
        }
        return restricciones;
    }

    public void EliminarAmigo(int idUsuario, int idAmigo)
    {
        const string query = "exec eliminarAmigo @idUsuario, @idAmigo";
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Execute(query, new { idUsuario, idAmigo });
        }
    }
    
    public List<Restriccion> GetRestriccionesByUsuario(string usuario)
    {
        string query = "exec GetRestriccionesByUsuario @username";
        List<Restriccion> restricciones = new List<Restriccion>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            restricciones = connection.Query<Restriccion>(query, new { username = usuario }).ToList();
        }
        return restricciones;
    }
    public List<Usuario> GetUsuariosByRestriccion(List<Restriccion> Restricciones) {
        string query = "exec GetUsuariosByRestriccion @idRestriccion";
        List<Usuario> users = new List<Usuario>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            foreach (Restriccion r in Restricciones)
            {
                List<Usuario> encontrados = connection.Query<Usuario>(query, new { idRestriccion = r.id }).ToList();
                users.AddRange(encontrados);
            }
        }
        return users
            .GroupBy(user => user.id)
            .Select(group => group.First())
            .ToList();
    }
    public List<Restaurante> GetRestaurantes()
    {
        const string query = @"exec GetRestaurantes";
        List<Restaurante> restaurantes = new List<Restaurante>();
        List<string> restricciones = new List<string>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            restaurantes = connection.Query<Restaurante>(query)
                .Where(restaurante => restaurante != null)
                .ToList();
        }
        foreach (Restaurante restaurante in restaurantes)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                restricciones = connection.Query<string>(
                    @"exec GetRestriccionesRestaurante @idRestaurante",
                    new { idRestaurante = restaurante.id }).ToList();
            }
            restaurante.restricciones = restricciones ?? new List<string>();
        }
        return restaurantes;
    }

    private void InsertarRestricciones(int idUsuario, IEnumerable<Restriccion> restricciones)
    {
        if (restricciones == null)
            return;
        foreach (Restriccion restriccion in restricciones
            .Where(restriccion => restriccion != null)
            .Where(restriccion => !string.IsNullOrWhiteSpace(restriccion.nombre))
            .Select(restriccion => new Restriccion { nombre = restriccion.nombre.Trim() })
            .GroupBy(r => r.nombre, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First()))
        {
            using (SqlConnection innerConnection = new SqlConnection(_connectionString))
            {
                connection.Execute(
                @"exec InsertarRestriccionesUsuario @idUsuario, @nombre",
                new { idUsuario, nombre = restriccion.nombre });
            }
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
                InsertarRestricciones(usuario.id, usuario.restricciones);
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
                CargarRestriccionesUsuario(usuario);
            }
        }
        return usuario;
    }
    public Usuario emUsuarioExiste(string email) {
        string query = "exec getUsuarioByEmail @pemail";
        Usuario usuario = new Usuario();
        using (SqlConnection connection = new SqlConnection(_connectionString)) {
            connection.Open();
            usuario = connection.QueryFirstOrDefault<Usuario>(query, new { pemail = email});
            if (usuario != null)
            {
                CargarRestriccionesUsuario(usuario);
            }
        }
        return usuario;
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

            CargarRestriccionesUsuario(usuario);
        }
        return usuario;
    }

    public void ActualizarUsuario(Usuario usuario)
        {
            const string query = @"UPDATE Usuarios
                                   SET username = @username,
                                       nombre = @nombre,
                                       apellido = @apellido,
                                       email = @email
                                   WHERE id = @id";

            using SqlConnection connection = new SqlConnection(_connectionString);
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

            using SqlConnection connection = new SqlConnection(_connectionString);
            return connection.Query<Usuario>(query, new { idUsuario }).ToList();
        }

        public List<Usuario> BuscarUsuariosPorTextoORestriccion(string texto, int idUsuario)
        {
            const string query = @"SELECT DISTINCT TOP 30 u.id, u.username, u.nombre, u.apellido, u.email, u.tipo, u.foto
                                   FROM Usuarios u
                                   LEFT JOIN RestriccionesUsuarios ru ON ru.idUsuario = u.id
                                   LEFT JOIN Restricciones r ON r.id = ru.idRestriccion
                                   WHERE u.id <> @idUsuario
                                     AND (u.username LIKE '%' + @texto + '%'
                                          OR u.nombre LIKE '%' + @texto + '%'
                                          OR u.apellido LIKE '%' + @texto + '%'
                                          OR u.email LIKE '%' + @texto + '%'
                                          OR r.nombre LIKE '%' + @texto + '%')
                                   ORDER BY u.nombre, u.apellido, u.username";

            using SqlConnection connection = new SqlConnection(_connectionString);
            return connection.Query<Usuario>(query, new { idUsuario, texto = texto.Trim() }).ToList();
        }

    public Usuario ObtenerPerfilPublico(string identificador)
        {
            const string query = @"SELECT TOP 1 id, username, nombre, apellido, tipo, foto
                                   FROM Usuarios
                                   WHERE username = @identificador OR email = @identificador";

            using SqlConnection connection = new SqlConnection(_connectionString);
            Usuario perfil = connection.QueryFirstOrDefault<Usuario>(query, new { identificador });

            if (perfil == null)
            {
                return null;
            }

            return perfil;
        }

    public bool SonAmigos(int idUsuarioA, int idUsuarioB)
        {
            const string query = @"SELECT CASE WHEN EXISTS (
                                       SELECT 1 FROM Amigos
                                       WHERE (idUsuarioA = @idUsuarioA AND idUsuarioB = @idUsuarioB)
                                          OR (idUsuarioA = @idUsuarioB AND idUsuarioB = @idUsuarioA)
                                   ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END";

            using SqlConnection connection = new SqlConnection(_connectionString);
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

            using SqlConnection connection = new SqlConnection(_connectionString);
            return connection.Query<Restaurante>(query, new { idUsuario }).ToList();
        }

    public List<Plato> ObtenerPlatosFavoritos(int idUsuario)
        {
            const string query = @"SELECT p.id, p.idRestaurante, p.nombre, p.descripcion, p.precio, p.tipo, p.foto
                               FROM PlatosFavoritos pf
                               INNER JOIN Platos p ON p.id = pf.idPlato
                               WHERE pf.idUsuario = @idUsuario
                               ORDER BY p.nombre";

            using SqlConnection connection = new SqlConnection(_connectionString);
            return connection.Query<Plato>(query, new { idUsuario }).ToList();
        }

    public void ActualizarRestriccionesUsuario(int idUsuario, IEnumerable<string> nombres)
        {
            List<string> restricciones = nombres
                .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
                .Select(nombre => nombre.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();
            connection.Execute(
                "DELETE FROM RestriccionesUsuarios WHERE idUsuario = @idUsuario",
                new { idUsuario });

            foreach (string nombre in restricciones)
            {
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

            using SqlConnection connection = new SqlConnection(_connectionString);
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

            using SqlConnection connection = new SqlConnection(_connectionString);
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

            using SqlConnection connection = new SqlConnection(_connectionString);
            return connection.ExecuteScalar<bool>(query, new { idUsuario, identificador = identificador.Trim() });
        }

        public void AsegurarTablasChat()
        {
            const string query = @"
                IF OBJECT_ID('dbo.ConversacionesChat', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ConversacionesChat (
                        id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        idUsuarioA int NOT NULL,
                        idUsuarioB int NOT NULL,
                        fechaActualizacion datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT UQ_ConversacionesChat_Pares UNIQUE (idUsuarioA, idUsuarioB),
                        CONSTRAINT CK_ConversacionesChat_Pares CHECK (idUsuarioA < idUsuarioB)
                    );
                END;

                IF OBJECT_ID('dbo.MensajesChat', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.MensajesChat (
                        id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        idConversacion int NOT NULL,
                        idRemitente int NOT NULL,
                        texto nvarchar(1000) NOT NULL,
                        fecha datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT FK_MensajesChat_Conversacion
                            FOREIGN KEY (idConversacion) REFERENCES dbo.ConversacionesChat(id)
                            ON DELETE CASCADE
                    );
                END;";

            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Execute(query);
        }

        public int ObtenerOCrearConversacion(int idUsuarioA, int idUsuarioB)
        {
            if (idUsuarioA == idUsuarioB)
                throw new ArgumentException("No se puede crear un chat consigo mismo.");

            AsegurarTablasChat();

            int menor = Math.Min(idUsuarioA, idUsuarioB);
            int mayor = Math.Max(idUsuarioA, idUsuarioB);

            const string query = @"
                IF NOT EXISTS (
                    SELECT 1 FROM ConversacionesChat
                    WHERE idUsuarioA = @menor AND idUsuarioB = @mayor
                )
                BEGIN
                    INSERT INTO ConversacionesChat (idUsuarioA, idUsuarioB)
                    VALUES (@menor, @mayor);
                END;

                SELECT id FROM ConversacionesChat
                WHERE idUsuarioA = @menor AND idUsuarioB = @mayor;";

            using SqlConnection connection = new SqlConnection(_connectionString);
            return connection.ExecuteScalar<int>(query, new { menor, mayor });
        }

        public bool UsuarioPerteneceAConversacion(int idUsuario, int idConversacion)
        {
            AsegurarTablasChat();
            const string query = @"SELECT CASE WHEN EXISTS (
                                       SELECT 1 FROM ConversacionesChat
                                       WHERE id = @idConversacion
                                         AND (idUsuarioA = @idUsuario OR idUsuarioB = @idUsuario)
                                   ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END";

            using SqlConnection connection = new SqlConnection(_connectionString);
            return connection.ExecuteScalar<bool>(query, new { idUsuario, idConversacion });
        }

        public void AgregarMensajeChat(int idConversacion, int idRemitente, string texto)
        {
            AsegurarTablasChat();
            const string query = @"
                INSERT INTO MensajesChat (idConversacion, idRemitente, texto)
                VALUES (@idConversacion, @idRemitente, @texto);

                UPDATE ConversacionesChat
                SET fechaActualizacion = SYSUTCDATETIME()
                WHERE id = @idConversacion;";

            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Execute(query, new { idConversacion, idRemitente, texto = texto.Trim() });
        }

        public List<MensajeChat> ObtenerMensajesChat(int idConversacion)
        {
            AsegurarTablasChat();
            const string query = @"
                SELECT m.id, m.idConversacion, m.idRemitente,
                       m.texto, m.fecha, u.username AS remitenteNombre
                FROM MensajesChat m
                INNER JOIN Usuarios u ON u.id = m.idRemitente
                WHERE m.idConversacion = @idConversacion
                ORDER BY m.fecha, m.id;";

            using SqlConnection connection = new SqlConnection(_connectionString);
            List<MensajeChat> mensajes = connection.Query<MensajeChat>(query, new { idConversacion }).ToList();

            // SQL Server guarda estas columnas con SYSUTCDATETIME(): indicar UTC evita
            // que se serialicen como hora local o que el navegador las interprete mal.
            foreach (MensajeChat mensaje in mensajes)
            {
                mensaje.fecha = DateTime.SpecifyKind(mensaje.fecha, DateTimeKind.Utc);
            }

            return mensajes;
        }

        public Usuario ObtenerUsuarioConversacion(int idConversacion, int idUsuarioActual)
        {
            AsegurarTablasChat();
            const string query = @"
                SELECT u.id, u.username, u.nombre, u.apellido, u.email, u.tipo, u.foto
                FROM ConversacionesChat c
                INNER JOIN Usuarios u ON u.id = CASE
                    WHEN c.idUsuarioA = @idUsuarioActual THEN c.idUsuarioB
                    ELSE c.idUsuarioA
                END
                WHERE c.id = @idConversacion
                  AND (c.idUsuarioA = @idUsuarioActual OR c.idUsuarioB = @idUsuarioActual);";

            using SqlConnection connection = new SqlConnection(_connectionString);
            Usuario usuario = connection.QueryFirstOrDefault<Usuario>(query, new { idConversacion, idUsuarioActual });

            if (usuario == null)
            {
                return null;
            }

            return usuario;
        }

        public List<ConversacionChat> ObtenerConversacionesChat(int idUsuario)
        {
            AsegurarTablasChat();
            const string query = @"
                SELECT c.id, c.idUsuarioA, c.idUsuarioB,
                       u.id, u.username, u.nombre, u.apellido, u.email, u.tipo, u.foto
                FROM ConversacionesChat c
                INNER JOIN Usuarios u ON u.id = CASE
                    WHEN c.idUsuarioA = @idUsuario THEN c.idUsuarioB
                    ELSE c.idUsuarioA
                END
                WHERE c.idUsuarioA = @idUsuario OR c.idUsuarioB = @idUsuario
                ORDER BY c.fechaActualizacion DESC;";

            const string ultimoQuery = @"
                SELECT TOP 1 id, idConversacion, idRemitente, texto, fecha
                FROM MensajesChat
                WHERE idConversacion = @idConversacion
                ORDER BY fecha DESC, id DESC;";

            using SqlConnection connection = new SqlConnection(_connectionString);
            var filas = connection.Query<ConversacionChat, Usuario, ConversacionChat>(
                query,
                (conversacion, usuario) =>
                {
                    conversacion.otroUsuario = usuario;
                    return conversacion;
                },
                new { idUsuario },
                splitOn: "id").ToList();

            foreach (ConversacionChat conversacion in filas)
            {
                conversacion.ultimoMensaje = connection.QueryFirstOrDefault<MensajeChat>(
                    ultimoQuery, new { idConversacion = conversacion.id });

                if (conversacion.ultimoMensaje != null)
                {
                    conversacion.ultimoMensaje.fecha = DateTime.SpecifyKind(
                        conversacion.ultimoMensaje.fecha,
                        DateTimeKind.Utc);
                }
            }

            return filas;
        }
    }
