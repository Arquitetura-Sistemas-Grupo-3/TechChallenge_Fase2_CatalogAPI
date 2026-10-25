using Core.Entidade;
using Core.Repository;
using Core.Validation;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infra.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using WebAPI.Interface;
using WebAPI.Middleweres;
using WebAPI.Service;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TechChallenge Jogos API",
        Version = "v1",
        Description = "Microserviço para gerenciamento de jogos."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Informe o token JWT desta forma: Bearer {seu token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});


#region [DB]
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
}, ServiceLifetime.Scoped);
#endregion

#region [Injeção de Dependencia]
builder.Services.AddScoped<IJogoRepository, JogoRepository>();
builder.Services.AddScoped<IJogoService, JogoService>();
builder.Services.AddScoped<ICompraService, CompraService>();
builder.Services.AddScoped<IRepository<Pedido>, EFRepository<Pedido>>();
builder.Services.AddScoped<IBibliotecaRepository, BibliotecaRepository>();
builder.Services.AddScoped<IBibliotecaService, BibliotecaService>();

builder.Services.AddTransient<CorrelationIdMiddleware>();

var factory = new ConnectionFactory {
    HostName = builder.Configuration["RabbitMQ:Host"] ?? "localhost",
    UserName = builder.Configuration["RabbitMQ:User"] ?? "guest",
    Password = builder.Configuration["RabbitMQ:Password"] ?? "guest"
};
builder.Services.AddSingleton(await factory.CreateConnectionAsync());
builder.Services.AddSingleton<EventPublisher>();
builder.Services.AddHostedService<PaymentProcessedConsumer>();

#endregion

#region [Validation]
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<JogoAdicionarRequisicaoValidator>();
#endregion

#region [Config JWT]
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});
#endregion

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope()) {
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<LogMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
