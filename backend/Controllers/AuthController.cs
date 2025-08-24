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

        command.CommandText = "SELECT Id, PasswordHash FROM Users WHERE Username = @Username;";
        command.Parameters.AddWithValue("@Username", model.Username);

        using var reader = command.ExecuteReader();
        if (!reader.Read()) return Unauthorized("Credenciales inválidas.");

        var userId = reader.GetInt32(0);
        var storedPasswordHash = reader.GetString(1);

        if (!BCrypt.Net.BCrypt.Verify(model.Password, storedPasswordHash))
            return Unauthorized("Credenciales inválidas.");

        var accessToken = Generador.GenerateAccessToken(new User { Id = userId, Username = model.Username });
        var refreshToken = Generador.GenerateRefreshToken();

        var expiryDate = DateTime.UtcNow.AddMinutes(3);   // corta vida
        var maxExpiryDate = DateTime.UtcNow.AddMinutes(6);   // vida máxima de sesión

        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO RefreshTokens (Token, UserId, ExpiryDate, MaxExpiryDate) VALUES (@Token, @UserId, @ExpiryDate, @MaxExpiryDate);";
        insertCommand.Parameters.AddWithValue("@Token", refreshToken);
        insertCommand.Parameters.AddWithValue("@UserId", userId);
        insertCommand.Parameters.AddWithValue("@ExpiryDate", expiryDate);
        insertCommand.Parameters.AddWithValue("@MaxExpiryDate", maxExpiryDate);
        insertCommand.ExecuteNonQuery();

        return Ok(
            new { 
                AccessToken = accessToken, 
                RefreshToken = refreshToken,
                RefreshExpiry = expiryDate,
                RefreshMaxExpiry = maxExpiryDate
            }
            );
    }


    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshTokenRequest model)
    {
        using var connection = _dbService.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT UserId, ExpiryDate, MaxExpiryDate FROM RefreshTokens WHERE Token = @Token;";
        command.Parameters.AddWithValue("@Token", model.RefreshToken);

        using var reader = command.ExecuteReader();
        if (!reader.Read()) return BadRequest("Invalid or expired refresh token.");

        var userId = reader.GetInt32(0);
        var expiryDate = reader.GetDateTime(1);
        var maxExpiryDate = reader.GetDateTime(2);

        // ⏳ Validar expiración corta
        if (expiryDate < DateTime.UtcNow)
        {
            return BadRequest("Refresh token expired.");
        }

        // 🔒 Validar expiración máxima
        if (maxExpiryDate < DateTime.UtcNow)
        {
            return BadRequest("Session expired. Please login again.");
        }

        // 🗑️ Eliminar el token usado
        using var deleteCommand = connection.CreateCommand();
        deleteCommand.CommandText = "DELETE FROM RefreshTokens WHERE Token = @Token;";
        deleteCommand.Parameters.AddWithValue("@Token", model.RefreshToken);
        deleteCommand.ExecuteNonQuery();

        // 👤 Obtener usuario
        using var userCommand = connection.CreateCommand();
        userCommand.CommandText = "SELECT Id, Username FROM Users WHERE Id = @UserId;";
        userCommand.Parameters.AddWithValue("@UserId", userId);
        using var userReader = userCommand.ExecuteReader();
        if (!userReader.Read()) return NotFound("User not found.");

        var user = new User { Id = userReader.GetInt32(0), Username = userReader.GetString(1) };

        // 🔑 Generar nuevos tokens
        var newAccessToken = Generador.GenerateAccessToken(user);
        var newRefreshToken = Generador.GenerateRefreshToken();
        var newExpiryDate = DateTime.UtcNow.AddMinutes(3); // corta vida

        // 💾 Guardar nuevo refresh token, pero conservar MaxExpiry original
        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = @"
        INSERT INTO RefreshTokens (Token, UserId, ExpiryDate, MaxExpiryDate) 
        VALUES (@Token, @UserId, @ExpiryDate, @MaxExpiryDate);";
        insertCommand.Parameters.AddWithValue("@Token", newRefreshToken);
        insertCommand.Parameters.AddWithValue("@UserId", userId);
        insertCommand.Parameters.AddWithValue("@ExpiryDate", newExpiryDate);
        insertCommand.Parameters.AddWithValue("@MaxExpiryDate", maxExpiryDate);
        insertCommand.ExecuteNonQuery();

        // ✅ Retornar también las fechas para que el frontend pueda validar
        return Ok(new
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            RefreshExpiry = newExpiryDate,
            RefreshMaxExpiry = maxExpiryDate
        });
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