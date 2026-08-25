using Microsoft.EntityFrameworkCore;
using QuasarApi.Database.Models;

namespace QuasarApi.DataBase
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Area> Area { get; set; }
        public DbSet<AppConfig> AppConfig { get; set; }
        public DbSet<Cliente> Cliente { get; set; }
        public DbSet<Empresa> Empresa { get; set; }
        public DbSet<Equipamento> Equipamento { get; set; }
        public DbSet<Estoque> Estoque { get; set; }
        public DbSet<Fornecedor> Fornecedor { get; set; }
        public DbSet<Transportadora> Transportadora { get; set; }
        public DbSet<Locacao> Locacao { get; set; }
        public DbSet<Material> Material { get; set; }
        public DbSet<NotaFiscal> NotaFiscal { get; set; }
        public DbSet<NotaFiscalItem> NotaFiscalItem { get; set; }
        public DbSet<RetornoInterno> RetornoInterno { get; set; }
        public DbSet<RetornoInternoItem> RetornoInternoItem { get; set; }
        public DbSet<Romaneio> Romaneio { get; set; }
        public DbSet<RomaneioItem> RomaneioItem { get; set; }
        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<Volume> Volume { get; set; }
        public DbSet<StatusVolume> StatusVolume { get; set; }
        public DbSet<Zona> Zona { get; set; }
        public DbSet<HistoricoArmazenagem> HistoricoArmazenagem { get; set; }
        public DbSet<Movimentacao> Movimentacao { get; set; }
        public DbSet<MovimentacaoDestino> MovimentacaoDestino { get; set; }
        public DbSet<DocExpedicao> DocExpedicao { get; set; }
        public DbSet<HistoricoDespacho> HistoricoDespacho { get; set; }
        public DbSet<DMS> DMS { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<DMS>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Empresa>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Estoque>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Fornecedor>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Transportadora>(entity =>
            {
                entity.ToTable("Transportadora");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Locacao>(entity =>
            {
                entity.ToTable("Locacao");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<AppConfig>(entity =>
            {
                entity.ToTable("AppConfig");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Material>(entity =>
            {
                entity.HasKey(e => e.Codigo);
            });

            modelBuilder.Entity<NotaFiscal>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<NotaFiscalItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Quantidade).HasPrecision(15, 3);
                entity.Property(e => e.QtdConferida).HasPrecision(15, 3);
                entity.Property(e => e.QtdArmazenada).HasPrecision(15, 3);
                entity.Property(e => e.UsuarioConferencia).HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.UsuarioArmazenagem).HasMaxLength(100).IsUnicode(false);
            });

            modelBuilder.Entity<RetornoInterno>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Romaneio>(entity =>
            {
                entity.ToTable("Romaneio");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<RomaneioItem>(entity =>
            {
                entity.ToTable("RomaneioItem");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Volume>(entity =>
            {
                entity.HasKey(e => new { e.NotaFiscalNr, e.VolumeNr });
            });

            modelBuilder.Entity<HistoricoArmazenagem>(entity =>
            {
                entity.HasKey(e => new { e.Id });
            });

            modelBuilder.Entity<Movimentacao>(entity =>
            {
                entity.ToTable("Movimentacao");
                entity.HasKey(e => e.Id); // Chave primária
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd(); // IDENTITY(1,1)

                entity.Property(e => e.ItemNr)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LocacaoOrigem)
                    .HasMaxLength(100);

                entity.Property(e => e.QtdOrigem);

                entity.Property(e => e.LocacaoEspera)
                    .HasMaxLength(100);

                entity.Property(e => e.LocacaoDestino)
                    .HasMaxLength(100);

                entity.Property(e => e.QtdDestino);

                entity.Property(e => e.FilialId);

                entity.Property(e => e.CriadoPor)
                    .HasMaxLength(100);

                entity.Property(e => e.CriadoEm);

                entity.Property(e => e.FinalizadoPor)
                    .HasMaxLength(100);

                entity.Property(e => e.FinalizadoEm);
            });

            modelBuilder.Entity<MovimentacaoDestino>(entity =>
            {
                entity.ToTable("MovimentacaoDestino");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ItemNr)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.Locacao)
                    .HasMaxLength(100);
                entity.Property(e => e.FilialId);
            });

            modelBuilder.Entity<DocExpedicao>(entity =>
            {
                entity.ToTable("DocExpedicao");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Numero).HasMaxLength(100).IsRequired();
                entity.Property(e => e.QtdVolumes).IsRequired();
            });

            modelBuilder.Entity<HistoricoDespacho>(entity =>
            {
                entity.ToTable("HistoricoDespacho");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NotaFiscalNr).HasMaxLength(100).IsRequired();
                entity.Property(e => e.VolumeNr).HasMaxLength(100).IsRequired();
                entity.Property(e => e.TransportadoraId).IsRequired();
                entity.Property(e => e.TransportadoraNome).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Veiculo).HasMaxLength(50);
                entity.Property(e => e.Responsavel).HasMaxLength(100);
                entity.Property(e => e.CriadoEm).IsRequired();
                entity.Property(e => e.CriadoPor).HasMaxLength(100);
            });

            modelBuilder.Entity<Zona>(entity =>
            {
                entity.ToTable("Zona");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).HasMaxLength(100);
            });


            base.OnModelCreating(modelBuilder);
        }
    }
}
