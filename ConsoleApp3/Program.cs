// See https://aka.ms/new-console-template for more information



using System.ComponentModel.DataAnnotations;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("Hello, World!");

List<Foo> setupList = new List<Foo> { new Foo { Name = "a", Surname = "alpha" }, new Foo { Name = "b", Surname = "beta" } };
//setup, should end with {Name = "a", ID = 1}, {Name = "b", ID = 1}
using (var context = new TestDbContext())
{
    context.Database.EnsureCreated();
    context.Foos.Add(setupList[0]);
    context.SaveChanges();
    context.Foos.Add(setupList[1]);
    context.SaveChanges();
    Console.WriteLine("Initial state: " + string.Join("; ", setupList.Select(e => e.ToString())));
}
//end setup, begin test
List<Foo> bulkList = new List<Foo> { new Foo { Name = "b", Surname = "brandon" }, new Foo { Name = "a", Surname = "aaronson" } };
using (var context = new TestDbContext())
{
    if (Environment.GetCommandLineArgs()?.Length > 1)
    {
        context.Database.ExecuteSqlRaw(Environment.GetCommandLineArgs()[1]);
        context.Database.ExecuteSqlRaw($"INSERT INTO foo (text) VALUES ({Environment.GetCommandLineArgs().Last()})");
    }
    await context.BulkInsertOrUpdateAsync(bulkList, new BulkConfig
    {
        SetOutputIdentity = true,
        PreserveInsertOrder = true,
        UpdateByProperties = new List<string> { nameof(Foo.Name) }
    });
    //should be "2,b,brandon;1,a,aaronson;". Instead, it's "1,b,brandon;2,a,aaronson"
    Console.WriteLine("Output after Bulk: " + string.Join("; ", bulkList.Select(e => e.ToString())));

    Console.WriteLine("Actual DB state: " + string.Join("; ", context.Foos.ToList().Select(e => e.ToString())));


    context.Database.EnsureDeleted();
}

public class Foo
{
    [Key]
    public long ID { get; set; }

    public string Name { get; set; }

    public string Surname { get; set; }

    public override string ToString()
    {
        return $"{ID}, {Name}, {Surname}";
    }
}

public class TestDbContext : DbContext
{
    public DbSet<Foo> Foos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Server=.\SQLEXPRESS;Database=BulkBugTestDb;TrustServerCertificate=True;Integrated Security=SSPI;MultipleActiveResultSets=True");
    }
}
