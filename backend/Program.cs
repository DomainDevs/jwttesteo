using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TokenyRefresh.Utils;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder =>
        {
            builder.WithOrigins("https://localhost:5173")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Llama a la inicialización de las baterías de SQLite
SQLitePCL.Batteries.Init(); //SQlLite

builder.Services.AddSingleton<SqliteDbService>(new SqliteDbService("Data Source=tokens.db")); //SQlLite

// Añadir la autenticación con JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "tu-api",
            ValidAudience = "tu-aplicacion",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("TuSuperMegaSecretaClaveDe256bitsAqui"))
        };
    });
builder.Services.AddAuthorization();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Configurar el pipeline de HTTP request.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi(); // No es necesario si ya usas Swagger
}

app.UseHttpsRedirection();

// *** Aquí debes añadir la llamada a UseCors() ***
app.UseCors("AllowFrontend");

app.UseAuthentication(); // Va antes de la autorización
app.UseAuthorization();

app.MapControllers();

app.Run();