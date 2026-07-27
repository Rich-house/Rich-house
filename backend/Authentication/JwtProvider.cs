using Marketify.Contracts.Authenthication;
using Marketify.Entites;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Marketify.Authentication
{
    public class JwtProvider : IJwtProvider
    {
        private readonly JwtOptions _jwtOptions;
        public JwtProvider(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
        }
        public (string token, int expiresIn) GenerateToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub ,user.Id),
            new (JwtRegisteredClaimNames.Email ,user.Email!),
            new (JwtRegisteredClaimNames.GivenName ,user.FirstName),
            new (JwtRegisteredClaimNames.FamilyName ,user.LastName),
            new (JwtRegisteredClaimNames.Jti ,Guid.NewGuid().ToString())
        };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            var symmetricSecurityKey = new SymmetricSecurityKey
                (Encoding.UTF8.GetBytes(_jwtOptions.key));
            var signinCredentials = new SigningCredentials(symmetricSecurityKey,SecurityAlgorithms.HmacSha256);
            var expiresIn = 30;
            var expiresInDate  = DateTime.UtcNow.AddMinutes(expiresIn);
            var token = new JwtSecurityToken //shpe
                (
                issuer: _jwtOptions.Issuer
               , audience: _jwtOptions.Audience
               , claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresIn),
               signingCredentials: signinCredentials


                );
            return (token: new JwtSecurityTokenHandler().WriteToken(token), expiresIn: _jwtOptions.ExpireyMinutes);

        }

        public string? ValidateToken(string token)
        {
            var  tokenhandler = new JwtSecurityTokenHandler();
            var symmetricSecurityKey = new SymmetricSecurityKey
               (Encoding.UTF8.GetBytes(_jwtOptions.key));
            try
            {
                tokenhandler.ValidateToken(token, new TokenValidationParameters
                {
                    IssuerSigningKey = symmetricSecurityKey,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);
                var jwttoken  = (JwtSecurityToken)validatedToken ;
                return jwttoken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
            }
            catch {
                return null;
            }
        }
    }
}
