﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Configurations
{
    [ExcludeFromCodeCoverage]
    internal class SubMenuConfiguration : IEntityTypeConfiguration<SubMenuEntity>
    {
        public void Configure(EntityTypeBuilder<SubMenuEntity> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Title).HasMaxLength(64);
            builder.Property(e => e.Url).HasMaxLength(256);
            builder.HasOne(d => d.Menu)
                   .WithMany(p => p.SubMenus)
                   .HasForeignKey(d => d.MenuId);
        }
    }
}
