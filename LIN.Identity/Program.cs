global using Http.ResponsesList;
global using LIN.Identity;
global using LIN.Identity.Hubs;
global using LIN.Identity.Services;
global using LIN.Modules;
global using LIN.Types.Auth.Enumerations;
global using LIN.Types.Auth.Models;
global using LIN.Types.Enumerations;
global using LIN.Types.Responses;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.SignalR;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.IdentityModel.Tokens;
global using System.Text;
global using LIN.Access.Logger;
using LIN.Identity.Data;
{

    LIN.Access.Logger.Logger.AppName = "LIN.IDENTITY";

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddSignalR();


    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAnyOrigin",
            builder =>
            {
                builder.AllowAnyOrigin()
                   .AllowAnyHeader()
                   .AllowAnyMethod();
            });
    });




    var sqlConnection = builder.Configuration["ConnectionStrings:somee"] ?? string.Empty;

    // Servicio de BD
    builder.Services.AddDbContext<Context>(options =>
    {
        options.UseSqlServer(sqlConnection);
    });



    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();


    try
    {
        // Si la base de datos no existe
        using var scope = app.Services.CreateScope();
        var dataContext = scope.ServiceProvider.GetRequiredService<Context>();
        var res = dataContext.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        _ = LIN.Access.Logger.Logger.Log(ex, 3);
    }


    app.UseCors("AllowAnyOrigin");

    app.MapHub<AccountHub>("/realTime/service");
    app.MapHub<PassKeyHub>("/realTime/auth/passkey");

    app.UseSwagger();
    app.UseSwaggerUI();

    Conexi�n.SetStringConnection(sqlConnection);

    app.UseStaticFiles();
    app.UseHttpsRedirection();
    Jwt.Open();
    EmailWorker.StarService();


    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}