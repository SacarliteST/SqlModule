$MigrationName = Read-Host "Enter Migration name "
dotnet ef migrations add $MigrationName -c SqliteDbContext -s Host -p Data -o ./Core/Migrations/SqlLite -v -- --CreateMigrationOnly
Read-Host -Prompt "Press Enter to exit"