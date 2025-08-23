using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TokenyRefresh.Models;
using TokenyRefresh.Utils;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SqliteDbService _dbService;

    public AuthController(SqliteDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest model)
    {
        using var connection = _dbService.GetConnection();
        using var command = connection.CreateCommand();

        // 1. Buscar al usuario por el nombre de usuario para obtener su hash de contraseña.
        command.CommandText = "SELECT Id, PasswordHash FROM Users WHERE Username = @Username;";
        command.Parameters.AddWithValue("@Username", model.Username);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            // Si no se encuentra el usuario, se devuelven credenciales inválidas.
            return Unauthorized("Credenciales inválidas.");
        }

        var userId = reader.GetInt32(0);
        var storedPasswordHash = reader.GetString(1); // Obtiene el hash almacenado

        // 2. Verificar la contraseña con el método de BCrypt.
        if (!BCrypt.Net.BCrypt.Verify(model.Password, storedPasswordHash))
        {
            // Si la verificación falla, se devuelven credenciales inválidas.
            return Unauthorized("Credenciales inválidas.");
        }

        // Si la verificación es exitosa, el resto de la lógica sigue igual:
        // Generar y guardar los tokens
        var accessToken = Generador.GenerateAccessToken(new User { Id = userId, Username = model.Username });
        var refreshToken = Generador.GenerateRefreshToken();

        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO RefreshTokens (Token, UserId, ExpiryDate) VALUES (@Token, @UserId, @ExpiryDate);";
        insertCommand.Parameters.AddWithValue("@Token", refreshToken);
        insertCommand.Parameters.AddWithValue("@UserId", userId);
        //insertCommand.Parameters.AddWithValue("@ExpiryDate", DateTime.UtcNow.AddDays(7)); //Cuantos días durará el token de refresh
        insertCommand.Parameters.AddWithValue("@ExpiryDate", DateTime.UtcNow.AddMinutes(1)); //Cuantos minutos durará el token de refresh
        insertCommand.ExecuteNonQuery();

        return Ok(new { AccessToken = accessToken, RefreshToken = refreshToken });
    }

    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshTokenRequest model)
    {
        using var connection = _dbService.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT UserId, ExpiryDate FROM RefreshTokens WHERE Token = @Token;";
        command.Parameters.AddWithValue("@Token", model.RefreshToken);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return BadRequest("Invalid or expired refresh token.");
        }

        var userId = reader.GetInt32(0);
        var expiryDate = reader.GetDateTime(1);
        if (expiryDate < DateTime.UtcNow)
        {
            // Revocar el token
            using var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = "DELETE FROM RefreshTokens WHERE Token = @Token;";
            deleteCommand.Parameters.AddWithValue("@Token", model.RefreshToken);
            deleteCommand.ExecuteNonQuery();

            return BadRequest("Invalid or expired refresh token.");
        }

        // Eliminar el token de refresco antiguo
        using var deleteCommand2 = connection.CreateCommand();
        deleteCommand2.CommandText = "DELETE FROM RefreshTokens WHERE Token = @Token;";
        deleteCommand2.Parameters.AddWithValue("@Token", model.RefreshToken);
        deleteCommand2.ExecuteNonQuery();

        // Obtener el usuario
        using var userCommand = connection.CreateCommand();
        userCommand.CommandText = "SELECT Id, Username FROM Users WHERE Id = @UserId;";
        userCommand.Parameters.AddWithValue("@UserId", userId);
        using var userReader = userCommand.ExecuteReader();
        if (!userReader.Read()) return NotFound("User not found.");

        var user = new User { Id = userReader.GetInt32(0), Username = userReader.GetString(1) };

        // Generar un nuevo JWT y un nuevo Refresh Token
        var newAccessToken = Generador.GenerateAccessToken(user);
        var newRefreshToken = Generador.GenerateRefreshToken();
        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO RefreshTokens (Token, UserId, ExpiryDate) VALUES (@Token, @UserId, @ExpiryDate);";
        insertCommand.Parameters.AddWithValue("@Token", newRefreshToken);
        insertCommand.Parameters.AddWithValue("@UserId", userId);
        //insertCommand.Parameters.AddWithValue("@ExpiryDate", DateTime.UtcNow.AddDays(7)); //Cuantos minutos durará el token de refresh
        insertCommand.Parameters.AddWithValue("@ExpiryDate", DateTime.UtcNow.AddMinutes(1)); //Cuantos minutos durará el token de refresh
        insertCommand.ExecuteNonQuery();

        return Ok(new { AccessToken = newAccessToken, RefreshToken = newRefreshToken });
    }

    [HttpPost("logout")]
    public IActionResult Logout([FromBody] RefreshTokenRequest model)
    {
        using var connection = _dbService.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM RefreshTokens WHERE Token = @Token;";
        command.Parameters.AddWithValue("@Token", model.RefreshToken);
        command.ExecuteNonQuery();

        return Ok();
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest model)
    {
        // 1. Validar que el nombre de usuario no exista
        using var connection = _dbService.GetConnection();
        using var checkUserCommand = connection.CreateCommand();
        checkUserCommand.CommandText = "SELECT Id FROM Users WHERE Username = @Username;";
        checkUserCommand.Parameters.AddWithValue("@Username", model.Username);

        using var reader = checkUserCommand.ExecuteReader();
        if (reader.Read())
        {
            return BadRequest("El nombre de usuario ya existe.");
        }

        // 2. Hashear la contraseña
        var hashedPassword = Security.HashPassword(model.Password);

        // 3. Insertar el nuevo usuario en la base de datos
        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO Users (Username, PasswordHash) VALUES (@Username, @PasswordHash);";
        insertCommand.Parameters.AddWithValue("@Username", model.Username);
        insertCommand.Parameters.AddWithValue("@PasswordHash", hashedPassword);

        try
        {
            insertCommand.ExecuteNonQuery();
            return Ok("Usuario registrado con éxito.");
        }
        catch (Exception ex)
        {
            // Manejo de errores de inserción
            return StatusCode(500, "Error al registrar el usuario.");
        }

    }

    [Authorize] // Asegura que solo los usuarios autenticados puedan acceder
    [HttpGet("protected")]
    public IActionResult GetProtectedData()
    {
        return Ok(new { message = "Acceso concedido. Esta es información protegida." });
    }

}