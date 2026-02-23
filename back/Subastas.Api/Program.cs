using Microsoft.EntityFrameworkCore;
using Subastas.Api.Data;
using Subastas.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var cs = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs))
);

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("front", p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod()
    );
});

var app = builder.Build();

app.UseCors("front");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/products", async (AppDbContext db) =>
    await db.Products.AsNoTracking().OrderByDescending(p => p.Id).ToListAsync()
);

app.MapGet("/api/products/{id:long}", async (long id, AppDbContext db) =>
{
    var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    return p is null ? Results.NotFound() : Results.Ok(p);
});

app.MapPost("/api/products", async (Product input, AppDbContext db) =>
{
    if (input.CurrentPrice <= 0) input.CurrentPrice = input.StartPrice;

    db.Products.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/api/products/{input.Id}", input);
});

app.MapPut("/api/products/{id:long}", async (long id, Product input, AppDbContext db) =>
{
    var existing = await db.Products.FirstOrDefaultAsync(x => x.Id == id);
    if (existing is null) return Results.NotFound();

    existing.Title = input.Title;
    existing.Description = input.Description;
    existing.StartPrice = input.StartPrice;
    existing.CurrentPrice = input.CurrentPrice;
    existing.StartsAt = input.StartsAt;
    existing.EndsAt = input.EndsAt;
    existing.Status = input.Status;
    existing.CreatedBy = input.CreatedBy;

    await db.SaveChangesAsync();
    return Results.Ok(existing);
});

app.MapDelete("/api/products/{id:long}", async (long id, AppDbContext db) =>
{
    var existing = await db.Products.FirstOrDefaultAsync(x => x.Id == id);
    if (existing is null) return Results.NotFound();

    db.Products.Remove(existing);
    await db.SaveChangesAsync();
    return Results.NoContent();
});


app.MapGet("/api/users", async (AppDbContext db) =>
    await db.Users.AsNoTracking().OrderByDescending(u => u.Id).ToListAsync()
);

app.MapGet("/api/users/{id:long}", async (long id, AppDbContext db) =>
{
    var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    return u is null ? Results.NotFound() : Results.Ok(u);
});

app.MapPost("/api/users", async (User input, AppDbContext db) =>
{
    db.Users.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/api/users/{input.Id}", input);
});

app.MapPut("/api/users/{id:long}", async (long id, User input, AppDbContext db) =>
{
    var existing = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
    if (existing is null) return Results.NotFound();

    existing.Email = input.Email;
    existing.PasswordHash = input.PasswordHash;
    existing.Role = input.Role;

    await db.SaveChangesAsync();
    return Results.Ok(existing);
});

app.MapDelete("/api/users/{id:long}", async (long id, AppDbContext db) =>
{
    var existing = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
    if (existing is null) return Results.NotFound();

    db.Users.Remove(existing);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();