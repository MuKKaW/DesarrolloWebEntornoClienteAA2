using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Subastas.Api.Data;
using Subastas.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
const string JwtIssuer = "Subastas.Api";
const string JwtAudience = "Subastas.Frontend";
const string JwtSecret = "your-secret-key-change-this-in-production-very-important";

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var cs = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrEmpty(cs))
{
    cs = "Server=db;Port=3306;Database=subastas;User=app;Password=app;";
}

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs), mysql =>
        mysql.EnableRetryOnFailure())
);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = JwtIssuer,
            ValidAudience = JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole("ADMIN") ||
            string.Equals(ctx.User.FindFirst("is_admin")?.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase)
        ));
});

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod()
    );
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await using var connection = db.Database.GetDbConnection();
    if (connection.State != ConnectionState.Open)
        await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT COUNT(*)
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'users'
          AND COLUMN_NAME = 'is_admin'
        """;

    var result = await command.ExecuteScalarAsync();
    var hasIsAdminColumn = Convert.ToInt32(result) > 0;

    if (!hasIsAdminColumn)
    {
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE users
            ADD COLUMN is_admin BOOLEAN NOT NULL DEFAULT FALSE;
            """);
    }

    await db.Database.ExecuteSqlRawAsync("""
        UPDATE users
        SET is_admin = CASE WHEN role = 'ADMIN' THEN TRUE ELSE FALSE END
        WHERE is_admin <> CASE WHEN role = 'ADMIN' THEN TRUE ELSE FALSE END;
        """);

    await SeedDefaultDataAsync(db);
}

app.MapHealthChecks("/health");

// === AUTENTICACIÓN ===

app.MapPost("/api/auth/login", async (LoginRequest req, AppDbContext db) =>
{
    var normalizedEmail = req.Email.Trim().ToLowerInvariant();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
    if (user is null) return Results.Json(new { message = "Credenciales invalidas" }, statusCode: StatusCodes.Status401Unauthorized);

    var passwordValid = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
    if (!passwordValid) return Results.Json(new { message = "Credenciales invalidas" }, statusCode: StatusCodes.Status401Unauthorized);

    var token = GenerateToken(user);
    return Results.Ok(new { token, user = ToUserResponse(user) });
});

app.MapPost("/api/auth/register", async (RegisterRequest req, AppDbContext db) =>
{
    var normalizedEmail = req.Email.Trim().ToLowerInvariant();
    if (req.Password.Length < 6)
        return Results.BadRequest(new { message = "La contrasena debe tener al menos 6 caracteres" });

    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
    if (existingUser is not null) return Results.BadRequest(new { message = "Email ya registrado" });

    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(req.Password);
    var newUser = new User
    {
        Email = normalizedEmail,
        PasswordHash = hashedPassword,
        Role = "USER",
        IsAdmin = false
    };

    db.Users.Add(newUser);
    await db.SaveChangesAsync();

    var token = GenerateToken(newUser);
    return Results.Created($"/api/users/{newUser.Id}", 
        new { token, user = ToUserResponse(newUser) });
});

// === PRODUCTOS ===

app.MapGet("/api/products", async (AppDbContext db, int page = 1, int pageSize = 10, 
    string? search = null, string sort = "createdAt", decimal? minPrice = null, decimal? maxPrice = null, string? status = null) =>
{
    var query = db.Products.AsNoTracking();

    if (!string.IsNullOrEmpty(search))
        query = query.Where(p => p.Title.Contains(search) || (p.Description != null && p.Description.Contains(search)));

    if (minPrice.HasValue)
        query = query.Where(p => p.StartPrice >= minPrice.Value);

    if (maxPrice.HasValue)
        query = query.Where(p => p.StartPrice <= maxPrice.Value);

    if (!string.IsNullOrWhiteSpace(status))
        query = query.Where(p => p.Status == status);

    var total = await query.CountAsync();
    var products = await query
        .OrderByDescending(p => p.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Results.Ok(new { items = products, total, page, pageSize });
});

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

app.MapPut("/api/admin/products/{id:long}/cancel", async (long id, AppDbContext db) =>
{
    var existing = await db.Products.FirstOrDefaultAsync(x => x.Id == id);
    if (existing is null) return Results.NotFound(new { message = "Producto no encontrado" });

    if (existing.Status != "ACTIVE")
        return Results.BadRequest(new { message = "Solo se pueden cancelar subastas activas" });

    existing.Status = "ENDED";
    existing.EndsAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(existing);
}).RequireAuthorization("AdminOnly");

// === USUARIOS ===

app.MapGet("/api/users", async (AppDbContext db, int page = 1, int pageSize = 10) =>
{
    var total = await db.Users.CountAsync();
    var users = await db.Users
        .AsNoTracking()
        .OrderByDescending(u => u.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Results.Ok(new { items = users, total, page, pageSize });
});

app.MapGet("/api/users/{id:long}", async (long id, AppDbContext db) =>
{
    var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    return u is null ? Results.NotFound() : Results.Ok(u);
});

app.MapPost("/api/users", async (CreateUserRequest req, AppDbContext db) =>
{
    var normalizedEmail = req.Email.Trim().ToLowerInvariant();
    var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
    if (existing is not null) return Results.BadRequest(new { message = "Email ya existe" });

    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(req.Password);
    var isAdmin = req.IsAdmin ?? string.Equals(req.Role, "ADMIN", StringComparison.OrdinalIgnoreCase);
    var user = new User
    {
        Email = normalizedEmail,
        PasswordHash = hashedPassword,
        Role = isAdmin ? "ADMIN" : req.Role,
        IsAdmin = isAdmin
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/api/users/{user.Id}", user);
});

app.MapPut("/api/users/{id:long}", async (long id, UpdateUserRequest input, AppDbContext db) =>
{
    var existing = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
    if (existing is null) return Results.NotFound();

    if (!string.IsNullOrEmpty(input.Email))
        existing.Email = input.Email.Trim().ToLowerInvariant();
    if (!string.IsNullOrEmpty(input.PasswordHash))
        existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.PasswordHash);
    if (!string.IsNullOrEmpty(input.Role))
        existing.Role = input.Role;
    if (input.IsAdmin.HasValue)
        existing.IsAdmin = input.IsAdmin.Value;
    else
        existing.IsAdmin = string.Equals(existing.Role, "ADMIN", StringComparison.OrdinalIgnoreCase);
    if (existing.IsAdmin && existing.Role != "ADMIN")
        existing.Role = "ADMIN";
    if (!existing.IsAdmin && existing.Role == "ADMIN")
        existing.Role = "USER";

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

// === PUJAS (BIDS) ===

app.MapGet("/api/bids", async (AppDbContext db, int page = 1, int pageSize = 10) =>
{
    var total = await db.Bids.CountAsync();
    var bids = await db.Bids
        .AsNoTracking()
        .OrderByDescending(b => b.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Results.Ok(new { items = bids, total, page, pageSize });
});

app.MapGet("/api/bids/{id:long}", async (long id, AppDbContext db) =>
{
    var b = await db.Bids.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    return b is null ? Results.NotFound() : Results.Ok(b);
});

app.MapGet("/api/products/{productId:long}/bids", async (long productId, AppDbContext db) =>
{
    var bids = await db.Bids
        .AsNoTracking()
        .Where(b => b.ProductId == productId)
        .OrderByDescending(b => b.Amount)
        .ToListAsync();

    return Results.Ok(bids);
});

app.MapPost("/api/bids", async (Bid input, ClaimsPrincipal principal, AppDbContext db) =>
{
    var product = await db.Products.FirstOrDefaultAsync(p => p.Id == input.ProductId);
    if (product is null) return Results.NotFound(new { message = "Producto no encontrado" });

    if (product.Status != "ACTIVE")
        return Results.BadRequest(new { message = "La subasta no está activa" });

    var authenticatedUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    if (authenticatedUserId is null || authenticatedUserId != input.UserId.ToString())
        return Results.Json(new { message = "Usuario no autorizado para pujar" }, statusCode: StatusCodes.Status403Forbidden);

    var leadingBid = await db.Bids
        .AsNoTracking()
        .Where(b => b.ProductId == input.ProductId)
        .OrderByDescending(b => b.Amount)
        .ThenByDescending(b => b.CreatedAt)
        .FirstOrDefaultAsync();

    var currentThreshold = Math.Max(product.CurrentPrice, leadingBid?.Amount ?? 0m);
    if (input.Amount <= currentThreshold)
        return Results.BadRequest(new { message = "La puja debe ser mayor al precio actual" });

    if (leadingBid?.UserId == input.UserId)
        return Results.BadRequest(new { message = "No puedes volver a pujar mientras sigues en cabeza" });

    product.CurrentPrice = Math.Max(input.Amount, currentThreshold);
    db.Bids.Add(input);
    await db.SaveChangesAsync();

    return Results.Created($"/api/bids/{input.Id}", ToBidResponse(input));
}).RequireAuthorization();

app.MapDelete("/api/bids/{id:long}", async (long id, AppDbContext db) =>
{
    var existing = await db.Bids.FirstOrDefaultAsync(x => x.Id == id);
    if (existing is null) return Results.NotFound();

    db.Bids.Remove(existing);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// === ESTADÍSTICAS ===

app.MapGet("/api/statistics", async (AppDbContext db) =>
{
    var totalProducts = await db.Products.CountAsync();
    var totalBids = await db.Bids.CountAsync();
    var avgPrice = await db.Products.AverageAsync(p => p.StartPrice);
    var activeProducts = await db.Products.CountAsync(p => p.Status == "ACTIVE");

    return Results.Ok(new
    {
        totalProducts,
        totalBids,
        avgPrice,
        activeProducts
    });
});

app.MapGet("/api/statistics/bids-by-date", async (AppDbContext db) =>
{
    var bidsByDate = await db.Bids
        .AsNoTracking()
        .GroupBy(b => b.CreatedAt.Date)
        .Select(g => new { date = g.Key, count = g.Count() })
        .OrderBy(x => x.date)
        .ToListAsync();

    return Results.Ok(bidsByDate);
});

app.MapGet("/api/statistics/top-products", async (AppDbContext db) =>
{
    var topProducts = await db.Products
        .AsNoTracking()
        .Select(p => new
        {
            p.Id,
            p.Title,
            bidCount = db.Bids.Count(b => b.ProductId == p.Id),
            p.CurrentPrice
        })
        .OrderByDescending(x => x.bidCount)
        .Take(5)
        .ToListAsync();

    return Results.Ok(topProducts);
});

app.Run();

// === HELPER FUNCTIONS ===

string GenerateToken(User user)
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim("is_admin", user.IsAdmin.ToString())
    };

    var token = new JwtSecurityToken(
        issuer: JwtIssuer,
        audience: JwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddDays(7),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

static object ToUserResponse(User user) => new
{
    user.Id,
    user.Email,
    user.Role,
    user.IsAdmin,
    user.CreatedAt
};

static object ToBidResponse(Bid bid) => new
{
    bid.Id,
    bid.ProductId,
    bid.UserId,
    bid.Amount,
    bid.CreatedAt
};

static async Task SeedDefaultDataAsync(AppDbContext db)
{
    var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@admin.com");
    var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("admin");
    if (adminUser is null)
    {
        adminUser = new User
        {
            Email = "admin@admin.com",
            PasswordHash = adminPasswordHash,
            Role = "ADMIN",
            IsAdmin = true
        };
        db.Users.Add(adminUser);
        await db.SaveChangesAsync();
    }
    else
    {
        adminUser.PasswordHash = adminPasswordHash;
        adminUser.IsAdmin = true;
        adminUser.Role = "ADMIN";
        await db.SaveChangesAsync();
    }

    var regularUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "user@user.com");
    var regularPasswordHash = BCrypt.Net.BCrypt.HashPassword("user");
    if (regularUser is null)
    {
        regularUser = new User
        {
            Email = "user@user.com",
            PasswordHash = regularPasswordHash,
            Role = "USER",
            IsAdmin = false
        };
        db.Users.Add(regularUser);
        await db.SaveChangesAsync();
    }
    else
    {
        regularUser.PasswordHash = regularPasswordHash;
        regularUser.IsAdmin = false;
        regularUser.Role = "USER";
        await db.SaveChangesAsync();
    }

    var now = DateTime.UtcNow;
    var seedProducts = new[]
    {
        new Product
        {
            Title = "MacBook Pro 14 M3",
            Description = "Portatil seminuevo con 16 GB de RAM y 512 GB SSD.",
            StartPrice = 1200m,
            CurrentPrice = 1350m,
            StartsAt = now.AddDays(-2),
            EndsAt = now.AddDays(5),
            Status = "ACTIVE",
            CreatedBy = adminUser.Id
        },
        new Product
        {
            Title = "Camara Sony Alpha A7 III",
            Description = "Camara full frame con objetivo 28-70 mm incluida en la subasta.",
            StartPrice = 900m,
            CurrentPrice = 1025m,
            StartsAt = now.AddDays(-1),
            EndsAt = now.AddDays(6),
            Status = "ACTIVE",
            CreatedBy = adminUser.Id
        },
        new Product
        {
            Title = "Nintendo Switch OLED",
            Description = "Con dock, funda y dos mandos adicionales.",
            StartPrice = 250m,
            CurrentPrice = 295m,
            StartsAt = now.AddHours(-12),
            EndsAt = now.AddDays(4),
            Status = "ACTIVE",
            CreatedBy = regularUser.Id
        },
        new Product
        {
            Title = "Bicicleta Trek Marlin 7",
            Description = "Mountain bike talla M revisada y lista para usar.",
            StartPrice = 650m,
            CurrentPrice = 760m,
            StartsAt = now.AddDays(-3),
            EndsAt = now.AddDays(3),
            Status = "ACTIVE",
            CreatedBy = regularUser.Id
        }
    };

    foreach (var seedProduct in seedProducts)
    {
        var existingProduct = await db.Products.FirstOrDefaultAsync(p => p.Title == seedProduct.Title);
        if (existingProduct is null)
        {
            db.Products.Add(seedProduct);
        }
        else
        {
            existingProduct.Description = seedProduct.Description;
            existingProduct.StartPrice = seedProduct.StartPrice;
            existingProduct.StartsAt = seedProduct.StartsAt;
            existingProduct.EndsAt = seedProduct.EndsAt;
            existingProduct.Status = seedProduct.Status;
            existingProduct.CreatedBy = seedProduct.CreatedBy;
        }
    }

    await db.SaveChangesAsync();

    var seededBids = new[]
    {
        new { ProductTitle = "MacBook Pro 14 M3", UserEmail = "user@user.com", Amount = 1350m },
        new { ProductTitle = "Camara Sony Alpha A7 III", UserEmail = "user@user.com", Amount = 1025m },
        new { ProductTitle = "Nintendo Switch OLED", UserEmail = "admin@admin.com", Amount = 295m },
        new { ProductTitle = "Bicicleta Trek Marlin 7", UserEmail = "admin@admin.com", Amount = 760m }
    };

    foreach (var seededBid in seededBids)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Title == seededBid.ProductTitle);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == seededBid.UserEmail);
        if (product is null || user is null)
            continue;

        var existingBid = await db.Bids.FirstOrDefaultAsync(b =>
            b.ProductId == product.Id &&
            b.UserId == user.Id &&
            b.Amount == seededBid.Amount);

        if (existingBid is null)
        {
            db.Bids.Add(new Bid
            {
                ProductId = product.Id,
                UserId = user.Id,
                Amount = seededBid.Amount,
                CreatedAt = now.AddHours(-2)
            });
        }

        if (product.CurrentPrice < seededBid.Amount)
            product.CurrentPrice = seededBid.Amount;
    }

    foreach (var product in await db.Products.ToListAsync())
    {
        var highestBidAmount = await db.Bids
            .Where(b => b.ProductId == product.Id)
            .MaxAsync(b => (decimal?)b.Amount) ?? 0m;

        product.CurrentPrice = Math.Max(product.CurrentPrice, Math.Max(product.StartPrice, highestBidAmount));
    }

    await db.SaveChangesAsync();
}

// === DTO CLASSES ===

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password);
public record CreateUserRequest(string Email, string Password, string Role, bool? IsAdmin);
public record UpdateUserRequest(string? Email, string? PasswordHash, string? Role, bool? IsAdmin);
