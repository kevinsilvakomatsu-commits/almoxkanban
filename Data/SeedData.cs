using AlmoxKanban.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace AlmoxKanban.Data
{
    public static class SeedData
    {
        public static void Initialize(AppDbContext context)
        {
            // Cria o banco e as tabelas caso ainda nao existam no PostgreSQL
            context.Database.EnsureCreated();

            if (context.Usuarios.Any())
                return;

            var admin = new Usuario
            {
                NomeCompleto = "Administrador",
                NomeUsuario = "admin",
                SenhaHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Funcao = "Administrador",
                Ativo = true,
                IsAdmin = true,
                DeveTrocarSenha = true,
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now
            };

            context.Usuarios.Add(admin);
            context.SaveChanges();
        }
    }
}