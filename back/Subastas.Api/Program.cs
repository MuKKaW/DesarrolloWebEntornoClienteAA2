using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using Subastas.Api.Data;
using Subastas.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

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
    static async Task<bool> HasColumnAsync(DbCommand command, string columnName)
    {
        command.CommandText = $"""
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'users'
              AND COLUMN_NAME = '{columnName}'
            """;

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    var hasIsAdminColumn = await HasColumnAsync(command, "is_admin");
    var hasNicknameColumn = await HasColumnAsync(command, "nickname");

    if (!hasIsAdminColumn)
    {
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE users
            ADD COLUMN is_admin BOOLEAN NOT NULL DEFAULT FALSE;
            """);
    }

    if (!hasNicknameColumn)
    {
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE users
            ADD COLUMN nickname VARCHAR(80) NOT NULL DEFAULT '';
            """);
    }

    await db.Database.ExecuteSqlRawAsync("""
        UPDATE users
        SET is_admin = CASE WHEN role = 'ADMIN' THEN TRUE ELSE FALSE END
        WHERE is_admin <> CASE WHEN role = 'ADMIN' THEN TRUE ELSE FALSE END;
        """);

    await db.Database.ExecuteSqlRawAsync("""
        UPDATE users
        SET nickname = SUBSTRING_INDEX(email, '@', 1)
        WHERE nickname IS NULL OR nickname = '';
        """);

    await SeedDefaultDataAsync(db);
}

app.MapHealthChecks("/health");

// === AUTENTICACIÓN ===

app.MapPost("/api/auth/login", async (LoginRequest req, AppDbContext db) =>
{
    var normalizedEmail = req.Email.Trim().ToLowerInvariant();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
    if (user is null) return Results.Json(new { message = "No existe ningun usuario con ese correo" }, statusCode: StatusCodes.Status401Unauthorized);

    var passwordValid = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
    if (!passwordValid) return Results.Json(new { message = "La contrasena es incorrecta" }, statusCode: StatusCodes.Status401Unauthorized);

    var token = GenerateToken(user);
    return Results.Ok(new { token, user = ToUserResponse(user) });
});

app.MapPost("/api/auth/register", async (RegisterRequest req, AppDbContext db) =>
{
    var normalizedEmail = req.Email.Trim().ToLowerInvariant();
    var normalizedNickname = NormalizeNickname(req.Nickname);
    if (req.Password.Length < 6)
        return Results.BadRequest(new { message = "La contrasena debe tener al menos 6 caracteres" });
    var nicknameValidationError = ValidateNickname(normalizedNickname);
    if (nicknameValidationError is not null)
        return Results.BadRequest(new { message = nicknameValidationError });

    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
    if (existingUser is not null) return Results.BadRequest(new { message = "Email ya registrado" });
    var existingNickname = await db.Users.FirstOrDefaultAsync(u => u.Nickname.ToLower() == normalizedNickname.ToLower());
    if (existingNickname is not null) return Results.BadRequest(new { message = "Ese apodo ya esta en uso" });

    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(req.Password);
    var newUser = new User
    {
        Email = normalizedEmail,
        Nickname = normalizedNickname,
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
        .Select(p => new
        {
            p.Id,
            p.CreatedBy,
            p.Title,
            p.Description,
            p.StartPrice,
            p.CurrentPrice,
            p.StartsAt,
            p.EndsAt,
            p.Status,
            p.CreatedAt,
            createdByNickname = p.CreatedByUser != null ? p.CreatedByUser.Nickname : null,
            lastBidNickname = db.Bids
                .Where(b => b.ProductId == p.Id)
                .OrderByDescending(b => b.Amount)
                .ThenByDescending(b => b.CreatedAt)
                .Select(b => b.User != null ? b.User.Nickname : null)
                .FirstOrDefault()
        })
        .ToListAsync();

    return Results.Ok(new { items = products, total, page, pageSize });
});

app.MapGet("/api/products/{id:long}", async (long id, AppDbContext db) =>
{
    var p = await db.Products
        .AsNoTracking()
        .Where(x => x.Id == id)
        .Select(product => new
        {
            product.Id,
            product.CreatedBy,
            product.Title,
            product.Description,
            product.StartPrice,
            product.CurrentPrice,
            product.StartsAt,
            product.EndsAt,
            product.Status,
            product.CreatedAt,
            createdByNickname = product.CreatedByUser != null ? product.CreatedByUser.Nickname : null,
            lastBidNickname = db.Bids
                .Where(b => b.ProductId == product.Id)
                .OrderByDescending(b => b.Amount)
                .ThenByDescending(b => b.CreatedAt)
                .Select(b => b.User != null ? b.User.Nickname : null)
                .FirstOrDefault()
        })
        .FirstOrDefaultAsync();
    return p is null ? Results.NotFound() : Results.Ok(p);
});

app.MapPost("/api/products", async (Product input, ClaimsPrincipal principal, AppDbContext db) =>
{
    var authenticatedUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!long.TryParse(authenticatedUserId, out var createdByUserId))
        return Results.Json(new { message = "Tu sesion ya no es valida. Vuelve a iniciar sesion." }, statusCode: StatusCodes.Status401Unauthorized);

    var creatorExists = await db.Users.AnyAsync(u => u.Id == createdByUserId);
    if (!creatorExists)
        return Results.Json(new { message = "Tu sesion ya no es valida. Vuelve a iniciar sesion." }, statusCode: StatusCodes.Status401Unauthorized);

    if (input.CurrentPrice <= 0) input.CurrentPrice = input.StartPrice;
    input.CreatedBy = createdByUserId;

    db.Products.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/api/products/{input.Id}", input);
}).RequireAuthorization();

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
    var normalizedNickname = NormalizeNickname(req.Nickname);
    var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
    if (existing is not null) return Results.BadRequest(new { message = "Email ya existe" });
    var nicknameValidationError = ValidateNickname(normalizedNickname);
    if (nicknameValidationError is not null)
        return Results.BadRequest(new { message = nicknameValidationError });
    var existingNickname = await db.Users.FirstOrDefaultAsync(u => u.Nickname.ToLower() == normalizedNickname.ToLower());
    if (existingNickname is not null) return Results.BadRequest(new { message = "Ese apodo ya esta en uso" });

    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(req.Password);
    var isAdmin = req.IsAdmin ?? string.Equals(req.Role, "ADMIN", StringComparison.OrdinalIgnoreCase);
    var user = new User
    {
        Email = normalizedEmail,
        Nickname = normalizedNickname,
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
    if (!string.IsNullOrWhiteSpace(input.Nickname))
    {
        var normalizedNickname = NormalizeNickname(input.Nickname);
        var nicknameValidationError = ValidateNickname(normalizedNickname);
        if (nicknameValidationError is not null) return Results.BadRequest(new { message = nicknameValidationError });
        var nicknameExists = await db.Users.AnyAsync(u => u.Id != id && u.Nickname.ToLower() == normalizedNickname.ToLower());
        if (nicknameExists) return Results.BadRequest(new { message = "Ese apodo ya esta en uso" });
        existing.Nickname = normalizedNickname;
    }
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
        .Select(b => new
        {
            b.Id,
            b.ProductId,
            b.UserId,
            b.Amount,
            b.CreatedAt,
            userNickname = b.User != null ? b.User.Nickname : null
        })
        .ToListAsync();

    return Results.Ok(new { items = bids, total, page, pageSize });
});

app.MapGet("/api/bids/{id:long}", async (long id, AppDbContext db) =>
{
    var b = await db.Bids
        .AsNoTracking()
        .Where(x => x.Id == id)
        .Select(bid => new
        {
            bid.Id,
            bid.ProductId,
            bid.UserId,
            bid.Amount,
            bid.CreatedAt,
            userNickname = bid.User != null ? bid.User.Nickname : null
        })
        .FirstOrDefaultAsync();
    return b is null ? Results.NotFound() : Results.Ok(b);
});

app.MapGet("/api/products/{productId:long}/bids", async (long productId, AppDbContext db) =>
{
    var bids = await db.Bids
        .AsNoTracking()
        .Where(b => b.ProductId == productId)
        .OrderByDescending(b => b.Amount)
        .ThenByDescending(b => b.CreatedAt)
        .Select(b => new
        {
            b.Id,
            b.ProductId,
            b.UserId,
            b.Amount,
            b.CreatedAt,
            userNickname = b.User != null ? b.User.Nickname : null
        })
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

    var biddingUser = await db.Users.FirstOrDefaultAsync(u => u.Id == input.UserId);
    if (biddingUser is null)
        return Results.Json(new { message = "Tu sesion ya no es valida. Vuelve a iniciar sesion." }, statusCode: StatusCodes.Status401Unauthorized);

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

    return Results.Created($"/api/bids/{input.Id}", ToBidResponse(input, biddingUser.Nickname));
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
        new Claim("nickname", user.Nickname),
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
    user.Nickname,
    user.Role,
    user.IsAdmin,
    user.CreatedAt
};

static object ToBidResponse(Bid bid, string? userNickname) => new
{
    bid.Id,
    bid.ProductId,
    bid.UserId,
    userNickname,
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
            Nickname = "admin",
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
        adminUser.Nickname = "admin";
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
            Nickname = "user",
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
        regularUser.Nickname = "user";
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

static string NormalizeNickname(string nickname) => nickname.Trim();
static string? ValidateNickname(string nickname)
{
    if (string.IsNullOrWhiteSpace(nickname))
        return "El apodo es obligatorio";
    if (nickname.Length < 3)
        return "El apodo debe tener al menos 3 caracteres";
    if (nickname.Length > 30)
        return "El apodo no puede superar los 30 caracteres";
    if (!Regex.IsMatch(nickname, "^[A-Za-z0-9_-]+$"))
        return "El apodo solo puede contener letras, numeros, guiones y guion bajo";
    return null;
}

// === DTO CLASSES ===

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password, string Nickname);
public record CreateUserRequest(string Email, string Password, string Role, bool? IsAdmin, string Nickname);
public record UpdateUserRequest(string? Email, string? PasswordHash, string? Role, bool? IsAdmin, string? Nickname);
