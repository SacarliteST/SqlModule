$MigrationName = Read-Host "Enter Migration name "
dotnet ef migrations add $MigrationName -c PostgreSqlDbContext -s Host -p Data -o ./Core/Migrations/PostgreSql -v -- --CreateMigrationOnly
Read-Host -Prompt "Press Enter to exit"