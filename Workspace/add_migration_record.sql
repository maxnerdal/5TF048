-- Insert the migration record to tell EF Core that InitialCreate has been applied
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES ('20250916000001_InitialCreate', '9.0.9');
