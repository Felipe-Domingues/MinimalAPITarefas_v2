using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("TarefasDB")
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", () => "Hello! World!");

app.MapGet("frases", async () => await new HttpClient().GetStringAsync("https://ron-swanson-quotes.herokuapp.com/v2/quotes"));

app.MapGet("/tarefas", async (AppDbContext db) => await db.Tarefas.ToListAsync());

app.MapGet("/tarefas/{id:int}", async (int id, AppDbContext db) =>
    await db.Tarefas.FindAsync(id) is Tarefa tarefa ? Results.Ok(tarefa) : Results.NotFound("Tarefa não encontrada"));

app.MapGet("/tarefas/concluida", async (AppDbContext db) => await db.Tarefas.Where(t => t.IsConcluida).ToListAsync());

app.MapPost("/tarefas", async (Tarefa tarefa, AppDbContext db) =>
    {
        db.Tarefas.Add(tarefa);
        db.SaveChangesAsync();
        return Results.Created($"/tarefas/{tarefa.Id}", tarefa);
    });

app.MapPut("/tarefas/{id:int}", async (int id, Tarefa inputTarefa, AppDbContext db) =>
    {
        if (id != inputTarefa.Id)
            return Results.BadRequest("Dados inválidos");

        var tarefa = await db.Tarefas.FindAsync(id);
        if (tarefa is null)
            return Results.NotFound("Tarefa não encontrada");

        tarefa.Nome = inputTarefa.Nome;
        tarefa.IsConcluida = inputTarefa.IsConcluida;

        await db.SaveChangesAsync();
        return Results.NoContent();
    });

app.MapDelete("/tarefas/{id:int}", async (int id, AppDbContext db) =>
    {
        if (await db.Tarefas.FindAsync(id) is Tarefa tarefa)
        {
            db.Tarefas.Remove(tarefa);
            await db.SaveChangesAsync();
            return Results.Ok(tarefa);
        }
        return Results.NotFound("Tarefa não encontrada");
    });

app.Run();

class Tarefa
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public bool IsConcluida { get; set; }
}

class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tarefa> Tarefas => Set<Tarefa>();
}