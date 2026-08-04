using LojaApi.Data;
using LojaApi.Services;
using LojaApi.src.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Resend;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ── Banco de dados ────────────────────────────────────────────────
        builder.Services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

        // ── JWT ───────────────────────────────────────────────────────────
        var jwtSecret = builder.Configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret não configurado.");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ClockSkew = TimeSpan.Zero,
                };
            });

        builder.Services.AddAuthorization();
        builder.Services.AddScoped<TokenService>();
        builder.Services.AddScoped<TenantService>();
        builder.Services.AddScoped<MercadoPagoService>();
        builder.Services.AddScoped<PlanosService>();
        builder.Services.AddScoped<FinanceiroService>();
        builder.Services.AddScoped<TurmasService>();
        builder.Services.AddScoped<AlertaEmailService>();
        builder.Services.AddScoped<ComunicadoEmailService>();
        builder.Services.AddHostedService<BloqueioAutomaticoService>();
        builder.Services.AddScoped<ReservaChacaraNotificacaoService>();

        // ── Controllers + Swagger ─────────────────────────────────────────
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Loja API", Version = "v1" });
            c.OperationFilter<LojaApi.src.Utils.FileUploadOperationFilter>();
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                Description = "Informe o token JWT: Bearer {token}",
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
            });
        });

        // ── CORS — libera o frontend ──────────────────────────────────────
        builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
        {
            var origins = (builder.Configuration["AllowedOrigins"] ?? "http://localhost:5173")
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
            p.WithOrigins(origins)
             .AllowAnyHeader()
             .AllowAnyMethod();
        }));

        builder.Services.AddResend(o =>
        {
            o.ApiToken = Environment.GetEnvironmentVariable("RESEND_APITOKEN") ?? "";
        });

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var app = builder.Build();

        // ── Migrations automáticas na inicialização ───────────────────────
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }

        // ── Middleware ────────────────────────────────────────────────────
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}