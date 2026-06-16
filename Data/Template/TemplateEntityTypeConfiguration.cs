using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TemplateApi.Data.Template;

internal sealed class TemplateEntityTypeConfiguration : IEntityTypeConfiguration<TemplateObject>
{
    public void Configure(EntityTypeBuilder<TemplateObject> builder)
    {
        builder.HasKey(templateObject => templateObject.Id);
    }
}
