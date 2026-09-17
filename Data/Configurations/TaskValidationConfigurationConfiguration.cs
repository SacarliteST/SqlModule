using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class TaskValidationConfigurationConfiguration
    : IEntityTypeConfiguration<TaskValidationConfiguration>
{
    public void Configure(EntityTypeBuilder<TaskValidationConfiguration> builder)
    {
        builder.ToTable("TaskValidationConfigurations", table =>
        {
            table.HasCheckConstraint(
                "CK_TaskValidationConfigurations_PassingScore",
                "\"PassingScore\" BETWEEN 1 AND 100");
            table.HasCheckConstraint(
                "CK_TaskValidationConfigurations_MaxAttempts",
                "\"MaxAttempts\" IS NULL OR \"MaxAttempts\" > 0");
            table.HasCheckConstraint(
                "CK_TaskValidationConfigurations_HintMask",
                "\"VisibleHintGroupsMask\" >= 0");
        });
        builder.HasKey(configuration => configuration.Id);
        builder.Property(configuration => configuration.TaskId).IsRequired();
        builder.Property(configuration => configuration.PassingScore).IsRequired();
        builder.Property(configuration => configuration.VisibleHintGroupsMask).IsRequired();
        builder.Property(configuration => configuration.Version).IsConcurrencyToken().IsRequired();
        builder.ConfigureAudit();

        builder.HasOne(configuration => configuration.Task)
            .WithOne(task => task.ValidationConfiguration)
            .HasForeignKey<TaskValidationConfiguration>(configuration => configuration.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(configuration => configuration.Checks)
            .WithOne(check => check.Configuration)
            .HasForeignKey(check => check.ConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(configuration => configuration.Checks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
