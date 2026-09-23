using AlmoxKanban.Data;
using AlmoxKanban.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AlmoxKanban.Services
{
    public interface IAuthService
    {
        Task<(bool Sucesso, Usuario? Usuario, string? Mensagem)> ValidarLoginAsync(string nomeUsuario, string senha);
        Task SignInAsync(HttpContext httpContext, Usuario usuario, bool lembrar);
        Task SignOutAsync(HttpContext httpContext);
        Task<(bool Sucesso, string? Mensagem)> AlterarSenhaAsync(int usuarioId, string senhaAtual, string novaSenha);
        string GerarHash(string senha);
        bool VerificarHash(string senha, string hash);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Sucesso, Usuario? Usuario, string? Mensagem)> ValidarLoginAsync(string nomeUsuario, string senha)
        {
            if (string.IsNullOrWhiteSpace(nomeUsuario) || string.IsNullOrWhiteSpace(senha))
                return (false, null, "Informe o usuário e a senha.");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NomeUsuario.ToLower() == nomeUsuario.Trim().ToLower());
            if (usuario == null)
                return (false, null, "Usuário ou senha inválidos.");

            if (!usuario.Ativo)
                return (false, null, "Este usuário está inativo. Contate o administrador.");

            if (!VerificarHash(senha, usuario.SenhaHash))
                return (false, null, "Usuário ou senha inválidos.");

            return (true, usuario, null);
        }

        public async Task SignInAsync(HttpContext httpContext, Usuario usuario, bool lembrar)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NomeUsuario),
                new Claim("NomeCompleto", usuario.NomeCompleto),
                new Claim(ClaimTypes.Role, usuario.IsAdmin ? "Admin" : "Usuario"),
                new Claim("Funcao", usuario.Funcao ?? ""),
                new Claim("FotoPerfil", usuario.FotoPerfil ?? ""),
                new Claim("DeveTrocarSenha", usuario.DeveTrocarSenha.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = lembrar,
                ExpiresUtc = lembrar ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        }

        public async Task SignOutAsync(HttpContext httpContext)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public async Task<(bool Sucesso, string? Mensagem)> AlterarSenhaAsync(int usuarioId, string senhaAtual, string novaSenha)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return (false, "Usuário não encontrado.");

            if (!usuario.DeveTrocarSenha && !VerificarHash(senhaAtual, usuario.SenhaHash))
                return (false, "Senha atual incorreta.");

            if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < 6)
                return (false, "A nova senha deve possuir pelo menos 6 caracteres.");

            usuario.SenhaHash = GerarHash(novaSenha);
            usuario.DeveTrocarSenha = false;
            usuario.DataAtualizacao = DateTime.Now;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public string GerarHash(string senha)
        {
            return BCrypt.Net.BCrypt.HashPassword(senha);
        }

        public bool VerificarHash(string senha, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(senha, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}
