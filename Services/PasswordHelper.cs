using BCrypt.Net;

namespace InframartAPI_New.Services
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                // Guard: BCrypt hashes must start with $2a$, $2b$, or $2y$.
                // If the DB contains a plain-text or non-BCrypt hash this throws
                // SaltParseException — we catch it and return false so the caller
                // gets a clean 401 instead of a 500.
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch (SaltParseException)
            {
                // Stored hash is not a valid BCrypt hash (plain-text, MD5, SHA, etc.)
                return false;
            }
        }
    }
}