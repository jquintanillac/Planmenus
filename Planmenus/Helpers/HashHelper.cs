using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace Planmenus.Helpers
{
    public class HashHelper
    {
        private const int Iteraciones = 100000;

        public static HashedPassword Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
            return new HashedPassword() { Password = Derivar(password, salt), Salt = Convert.ToBase64String(salt) };
        }

        public static bool CheckHash(string attemptedPassword, string hash, string salt)
        {
            if (string.IsNullOrEmpty(attemptedPassword) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
            {
                return false;
            }

            byte[] saltBytes;
            byte[] hashBytes;
            try
            {
                saltBytes = Convert.FromBase64String(salt);
                hashBytes = Convert.FromBase64String(hash);
            }
            catch (FormatException)
            {
                // El valor guardado no es un hash (p. ej. contraseña antigua en texto plano)
                return false;
            }

            byte[] intento = Convert.FromBase64String(Derivar(attemptedPassword, saltBytes));
            return CryptographicOperations.FixedTimeEquals(intento, hashBytes);
        }

        /// <summary>
        /// Indica si el valor guardado tiene el formato de un hash generado por esta clase
        /// (256 bits en Base64), para distinguirlo de una contraseña antigua en texto plano.
        /// </summary>
        public static bool EsHash(string valor)
        {
            if (string.IsNullOrEmpty(valor) || valor.Length != 44)
            {
                return false;
            }
            Span<byte> buffer = stackalloc byte[33];
            return Convert.TryFromBase64String(valor, buffer, out int bytes) && bytes == 32;
        }

        private static string Derivar(string password, byte[] salt)
        {
            // derive a 256-bit subkey (HMACSHA256)
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: Iteraciones,
                numBytesRequested: 256 / 8));
        }
    }
    public class HashedPassword
    {
        public string Password { get; set; }
        public string Salt { get; set; }
    }
}
