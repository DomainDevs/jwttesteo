using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TokenyRefresh.Models;

namespace TokenyRefresh.Utils;

public static class Generador
{
    public static string GenerateAccessToken(User user)
    {
        // Carga la clave secreta desde la configuración
        var tokenSecret = "TuSuperMegaSecretaClaveDe256bitsAqui*tr53ghpoyhgrhjhts5d3sd"; // Debe ser largo y seguro
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Define los "claims" (información que quieres guardar en el token)
        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username)
    };

        // Crea el token con la información, la expiración y la firma
        var token = new JwtSecurityToken(
            issuer: "https://localhost:7047", //BackEnd
            audience: "https://localhost:5173", //FrontEnd
            claims: claims,
            //expires: DateTime.UtcNow.AddMinutes(15), // Expira access token, expira en 15 minutos
            expires: DateTime.UtcNow.AddSeconds(30), // Expira access token, expira en 30 segundo para pruebas
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public static string GenerateRefreshToken()
    {
        // Genera 32 bytes de datos aleatorios
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

}
