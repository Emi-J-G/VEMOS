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
        const string query = "exec GetRestaurantes";
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
                    "exec GetRestriccionesRestaurante @idRestaurante",
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
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Execute(
                "exec InsertarRestriccionesUsuario @idUsuario, @nombre",
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
        string rquery = "exec getRestriccionesByidUser @idUser";
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
        }
        return usuario;
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
        string rquery = "exec getRestriccionesByidUser @idUser";

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
            const string query = "exec updateUser @username, @nombre, @apellido, @email, @id";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Execute(query, new
                {
                    usuario.id,
                    usuario.username,
                    usuario.nombre,
                    usuario.apellido,
                    usuario.email
                });
            }
        }

    public List<Usuario> ObtenerAmigos(int idUsuario)
        {
            const string query = "exec getAmigos @idUsuario";
            List <Usuario> amigos = new List<Usuario>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                amigos = connection.Query<Usuario>(query, new { idUsuario }).ToList();
            }
            return amigos;
        }

        public List<Usuario> BuscarUsuariosPorTextoORestriccion(string texto, int idUsuario)
        {
            const string query = "exec BuscarUsuariosPorTextoORestriccion @idUsuario, @texto";
            List<Usuario> usuarios = new List<Usuario>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                usuarios = connection.Query<Usuario>(query, new { idUsuario, texto = texto.Trim() }).ToList();
            }
            return usuarios;
        }

    public Usuario ObtenerPerfilPublico(string identificador)
        {
            const string query = "exec ObtenerPerfilPublico @identificador";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                Usuario perfil = connection.QueryFirstOrDefault<Usuario>(query, new { identificador });
            }
            if (perfil == null)
            {
                return null;
            }

            return perfil;
        }

    public bool SonAmigos(int idUsuarioA, int idUsuarioB)
        {
            const string query = "exec VerificarAmigos @idUsuarioA, @idUsuarioB";
            bool sonAmigos = false;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                sonAmigos = connection.ExecuteScalar<bool>(query, new { idUsuarioA, idUsuarioB });
            }
            return sonAmigos;
        }

    public List<Restaurante> ObtenerRestaurantesFavoritos(int idUsuario)
        {
            const string query = "exec GetRestaurantesFavoritos @idUsuario";
            List<Restaurante> restaurantes = new List<Restaurante>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                restaurantes = connection.Query<Restaurante>(query, new { idUsuario }).ToList();
            }
            return restaurantes;
        }

    public List<Plato> ObtenerPlatosFavoritos(int idUsuario)
        {
            const string query = "exec GetPlatosFavoritos @idUsuario";
            List<Plato> platos = new List<Plato>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                platos = connection.Query<Plato>(query, new { idUsuario }).ToList();
            }
            return platos;
        }

    public void ActualizarRestriccionesUsuario(int idUsuario, IEnumerable<string> nombres)
        {
            List<string> restricciones = nombres
                .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
                .Select(nombre => nombre.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Execute(
                    "exec DeleteRestriccionUsuario @idUsuario",
                    new { idUsuario });
            }
            foreach (string nombre in restricciones)
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                connection.Execute(
                    "exec InsertRestriccionUsuario @idUsuario, @nombre",
                    new { idUsuario, nombre });
                }
            }
        }

    public List<int> ObtenerIdsRestaurantesFavoritos(int idUsuario)
        {
            const string query = "exec GetIdsRestaurantesFavoritos @idUsuario";
            List<int> ids = new List<int>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                ids = connection.Query<int>(query, new { idUsuario }).ToList();
            }
            return ids;
        }

    public bool AlternarRestauranteFavorito(int idUsuario, int idRestaurante)
        {
            const string query = "exec AlternarRestauranteFavorito @idUsuario, @idRestaurante";
            bool resultado = false;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                resultado = connection.ExecuteScalar<bool>(query, new { idUsuario, idRestaurante });
            }
            return resultado;
        }

    public bool AgregarAmigo(int idUsuario, string identificador)
        {
            const string query = "exec AgregarAmigo @idUsuario, @identificador";
            bool resultado = false;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                resultado = connection.ExecuteScalar<bool>(query, new { idUsuario, identificador = identificador.Trim() });
            }
            return resultado;
        }

        public int ObtenerOCrearConversacion(int idUsuarioA, int idUsuarioB)
        {
            if (idUsuarioA == idUsuarioB)
                throw new ArgumentException("No se puede crear un chat consigo mismo.");
            const string query = "exec GetConversacion @idUsuarioA, @idUsuarioB";
            int idConversacion = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                idConversacion = connection.ExecuteScalar<int>(query, new { idUsuarioA, idUsuarioB });
            }
            return idConversacion;
        }

        public bool UsuarioPerteneceAConversacion(int idUsuario, int idConversacion)
        {
            const string query = "exec VerUsuarioPerteneceAConversacion @idUsuario, @idConversacion";
            bool pertenece = false;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                pertenece = connection.ExecuteScalar<bool>(query, new { idUsuario, idConversacion });
            }
            return pertenece;
        }

        public void AgregarMensajeChat(int idConversacion, int idRemitente, string texto)
        {
            const string query = @"exec AgregarMensajeChat @idConversacion, @idRemitente, @texto";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Execute(query, new { idConversacion, idRemitente, texto = texto.Trim() });
            }
        }

        public List<MensajeChat> ObtenerMensajesChat(int idConversacion)
        {
            const string query = @"exec ObtenerMensajesChat @idConversacion";

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
