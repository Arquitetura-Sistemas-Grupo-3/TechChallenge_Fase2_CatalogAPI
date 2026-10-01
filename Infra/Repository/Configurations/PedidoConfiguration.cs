using Core.Entidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infra.Repository.Configurations
{
    public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
    {
        public void Configure(EntityTypeBuilder<Pedido> builder)
        {
            builder.ToTable("Pedido");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnType("INT").UseIdentityColumn();
            builder.Property(x => x.Preco).HasColumnType("decimal(18,2)");
        }
    }

    public class BibliotecaJogoConfiguration : IEntityTypeConfiguration<BibliotecaJogo>
    {
        public void Configure(EntityTypeBuilder<BibliotecaJogo> builder)
        {
            builder.ToTable("BibliotecaJogo");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnType("INT").UseIdentityColumn();
            builder.HasIndex(x => new { x.UserId, x.JogoId });
        }
    }
}
