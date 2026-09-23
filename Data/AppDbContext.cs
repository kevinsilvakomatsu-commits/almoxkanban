using Microsoft.EntityFrameworkCore;
using AlmoxKanban.Models;

namespace AlmoxKanban.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Tarefa> Tarefas { get; set; }
        public DbSet<TarefaResponsavel> TarefaResponsaveis { get; set; }
        public DbSet<FotoTarefa> FotosTarefa { get; set; }
        public DbSet<ResolucaoTarefa> Resolucoes { get; set; }
        public DbSet<FotoResolucao> FotosResolucao { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<HistoricoTarefa> HistoricoTarefas { get; set; }
        public DbSet<RegistroAuditoria> RegistrosAuditoria { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TarefaResponsavel>()
                .HasKey(tr => new { tr.TarefaId, tr.UsuarioId });

            modelBuilder.Entity<TarefaResponsavel>()
                .HasOne(tr => tr.Tarefa)
                .WithMany(t => t.Responsaveis)
                .HasForeignKey(tr => tr.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TarefaResponsavel>()
                .HasOne(tr => tr.Usuario)
                .WithMany(u => u.TarefasResponsavel)
                .HasForeignKey(tr => tr.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tarefa>()
                .HasOne(t => t.Criador)
                .WithMany(u => u.TarefasCriadas)
                .HasForeignKey(t => t.CriadorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tarefa>()
                .HasOne(t => t.UsuarioConclusao)
                .WithMany()
                .HasForeignKey(t => t.UsuarioConclusaoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ResolucaoTarefa>()
                .HasOne(r => r.Tarefa)
                .WithOne(t => t.Resolucao)
                .HasForeignKey<ResolucaoTarefa>(r => r.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ResolucaoTarefa>()
                .HasOne(r => r.UsuarioConclusao)
                .WithMany()
                .HasForeignKey(r => r.UsuarioConclusaoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FotoTarefa>()
                .HasOne(f => f.Tarefa)
                .WithMany(t => t.Fotos)
                .HasForeignKey(f => f.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FotoResolucao>()
                .HasOne(f => f.Resolucao)
                .WithMany(r => r.Fotos)
                .HasForeignKey(f => f.ResolucaoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comentario>()
                .HasOne(c => c.Tarefa)
                .WithMany(t => t.Comentarios)
                .HasForeignKey(c => c.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comentario>()
                .HasOne(c => c.Usuario)
                .WithMany(u => u.Comentarios)
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HistoricoTarefa>()
                .HasOne(h => h.Tarefa)
                .WithMany(t => t.Historicos)
                .HasForeignKey(h => h.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HistoricoTarefa>()
                .HasOne(h => h.Usuario)
                .WithMany(u => u.Historicos)
                .HasForeignKey(h => h.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RegistroAuditoria>()
                .HasOne(r => r.Usuario)
                .WithMany()
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.NomeUsuario)
                .IsUnique();
        }
    }
}
