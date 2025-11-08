using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using FullPost.Models.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace FullPost.Authentication;
public class JWTAuthentication : IJWTAuthentication
{
    public string _key;
    IConfiguration config;
    public JWTAuthentication(string key, IConfiguration configuration)
    {
        _key = key;
        config = configuration;
    }
    public string GenerateToken(LoginResponse loginResponse)
    {
        var jwtSettings = config.GetSection("Jwt");
        var tokenKey = Encoding.ASCII.GetBytes(jwtSettings["Key"]);
        var claims = new List<Claim>();
        var tokenHandler = new JwtSecurityTokenHandler();
        claims.Add(new Claim(ClaimTypes.Name, loginResponse.UserId.ToString()));
        claims.Add(new Claim(ClaimTypes.Email, loginResponse.Email));
        // foreach (var role in loginResponse.Roles)
        // {
        //     claims.Add(new Claim(ClaimTypes.Role, role.Name));
        // }
        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims),
            IssuedAt = DateTime.Now,
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpireMinutes"])),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(tokenKey),
                SecurityAlgorithms.HmacSha256Signature
            )
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}