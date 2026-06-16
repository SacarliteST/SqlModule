using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;

namespace SQLModule.Data.Template;

internal sealed class TemplateEntityTypeConfiguration : IEntityTypeConfiguration<TemplateObject>
{
    public void Configure(EntityTypeBuilder<TemplateObject> builder)
    {
        builder.HasKey(templateObject => templateObject.Id);
    }
}
