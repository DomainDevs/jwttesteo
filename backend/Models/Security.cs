using BCrypt.Net;

namespace TokenyRefresh.Models;

public static class Security
{
    // Método para hashear la contraseña
    public static string HashPassword(string password)
    {
        // Genera un hash seguro con una sal aleatoria
        return BCrypt.Net.BCrypt.HashPassword(password, 12); // El "12" es el work factor, un valor más alto hace el hash más lento y seguro
    }


    // Método para verificar si una contraseña coincide con un hash
    public static bool VerifyPasswordHash(string password, string passwordHash)
    {
        // Verifica si la contraseña proporcionada coincide con el hash almacenado
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}





