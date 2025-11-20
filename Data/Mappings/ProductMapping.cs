using Dima.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dima.Api.Data.Mappings;

public class ProductMapping : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Product");
        
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasColumnType("NVARCHAR")
            .HasMaxLength(80);
        
        builder.Property(x => x.Slug)
            .IsRequired()
            .HasColumnType("VARCHAR")
            .HasMaxLength(80);
        
        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasColumnType("NVARCHAR")
            .HasMaxLength(255);

        builder.Property(x => x.Price)
            .IsRequired()
            .HasColumnType("MONEY");

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasColumnType("BIT");

    }
}