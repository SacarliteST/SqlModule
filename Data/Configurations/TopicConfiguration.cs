using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TopicName).IsRequired().HasMaxLength(300);
        builder.ConfigureAudit();

        builder.HasMany(x => x.SubTopics)
               .WithOne(x => x.ParentTopic)
               .HasForeignKey(x => x.ParentTopicId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Tasks)
               .WithOne(x => x.Topic)
               .HasForeignKey(x => x.TopicId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
