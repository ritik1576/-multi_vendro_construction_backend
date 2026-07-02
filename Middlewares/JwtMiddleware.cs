using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace InframartAPI_New.Middlewares
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private const string SecretKey = "THIS_IS_SECRET_KEY_123";

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(token) && token.StartsWith("Bearer "))
            {
                token = token.Substring("Bearer ".Length);

                attachUserToContext(context, token);
            }

            await _next(context);
        }

        private void attachUserToContext(HttpContext context, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(SecretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                var email = jwtToken.Claims.First(x => x.Type == "email").Value;
                var role = jwtToken.Claims.First(x => x.Type == "role").Value;

                // Store in HttpContext
                context.Items["UserEmail"] = email;
                context.Items["UserRole"] = role;
            }
            catch
            {
                // Invalid token → ignore user
            }
        }
    }
}