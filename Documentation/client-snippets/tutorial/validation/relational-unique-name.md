```csharp
public class LibraryDbContext : DbContext
{
    public DbSet<Author> Authors => Set<Author>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Author>().HasIndex(author => author.Name).IsUnique();
    }
}
```
