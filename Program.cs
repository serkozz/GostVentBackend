using System.Text;
using Services;
using EF.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Types.Classes;
using Microsoft.EntityFrameworkCore;

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

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddAuthorization();

builder.Services.AddSqlite<SQLiteContext>(builder.Configuration.GetConnectionString("SQLite"));
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<YooKassaPaymentService>(provider => new YooKassaPaymentService(builder.Configuration, provider.GetRequiredService<OrderService>(), provider.GetRequiredService<SQLiteContext>()));
builder.Services.AddSingleton<StorageServiceCollection>(new StorageServiceCollection());
builder.Services.AddSingleton<MailService>(new MailService(builder.Configuration, builder.Services.First(descriptor => descriptor.ImplementationType == typeof(SQLiteContext)).ImplementationInstance as SQLiteContext));

// builder.Services.AddSingleton<YooKassaPaymentService>(x => 
//     new YooKassaPaymentService(builder.Configuration,
//     x.GetRequiredService<OrderService>(),
//     x.GetRequiredService<SQLiteContext>()));

// builder.Services.AddSingleton<YooKassaPaymentService>(new YooKassaPaymentService(builder.Configuration,
//         builder.Services.Where(desc => desc.ImplementationType == typeof(OrderService)).ToList()[0].ImplementationInstance as OrderService));

var storageServiceCollection = builder.Services.FirstOrDefault(desc => desc.ServiceType == typeof(StorageServiceCollection))?.ImplementationInstance as StorageServiceCollection;

if (storageServiceCollection is null)
    throw new InvalidCastException("Cant get StorageServiceCollection from services");

storageServiceCollection.TryAddService(new DropboxStorageService(builder.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
