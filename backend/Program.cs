using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TokenyRefresh.Utils;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
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
            ValidIssuer = "https://localhost:7047", // 👈 debe coincidir con ValidIssuer //BackEnd
            ValidAudience = "https://localhost:5173", // 👈 debe coincidir con ValidAudience //FrontEnd
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("TuSuperMegaSecretaClaveDe256bitsAqui*tr53ghpoyhgrhjhts5d3sd"))
        };
    });
builder.Services.AddAuthorization();


var app = builder.Build();


// *** Aquí debes añadir la llamada a UseCors() ***
app.UseCors("AllowFrontend");


app.UseSwagger();
app.UseSwaggerUI();

// Configurar el pipeline de HTTP request.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi(); // No es necesario si ya usas Swagger
}

app.UseHttpsRedirection();


app.UseAuthentication(); // Va antes de la autorización
app.UseAuthorization();

app.MapControllers();

app.Run();