using System.Text;
using Services;
using EF.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Types.Classes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            // policy.AllowAnyOrigin();
            /// Можно менять источники запросов, или просто прописать AllowAnyOrigin() (но лучше с корсом)
            policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod();
            policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
        });
});

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
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SecretKeySixteen")),
        ValidateAudience = false,
        ValidateIssuer = false,
        /// Allows to make token expire in less than 5 minutes 
        ClockSkew = TimeSpan.Zero,
    };
});

builder.Services.AddAuthorization();

builder.Services.AddSqlite<SQLiteContext>(builder.Configuration.GetConnectionString("SQLite"));
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddSingleton<StorageServiceCollection>(new StorageServiceCollection());

var storageServiceCollection = builder.Services.FirstOrDefault(desc => desc.ServiceType == typeof(StorageServiceCollection))?.ImplementationInstance as StorageServiceCollection;

if (storageServiceCollection is null)
    throw new InvalidCastException("Cant get StorageServiceCollection from services");

storageServiceCollection.TryAddService(new DropboxStorageService(builder.Configuration));
storageServiceCollection.TryAddService(new FileSystemStorageService());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();   // добавление middleware аутентификации 
app.UseAuthorization();   // добавление middleware авторизации
app.MapControllers();
app.Run();
