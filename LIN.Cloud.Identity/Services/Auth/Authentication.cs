﻿using LIN.Cloud.Identity.Services.Auth.Interfaces;
using LIN.Cloud.Identity.Services.Auth.Models;

namespace LIN.Cloud.Identity.Services.Auth;

public partial class Authentication(Data.Accounts accountData, Data.AccountLogs accountLogs, Data.ApplicationRestrictions applicationRestrictions, Data.Applications applications, IIdentityService identityService, IAllowService allowService) : Interfaces.IAuthentication
{

    /// <summary>
    /// Usuario.
    /// </summary>
    private string User { get; set; } = string.Empty;


    /// <summary>
    /// Usuario.
    /// </summary>
    private string Password { get; set; } = string.Empty;


    /// <summary>
    /// Código de la aplicación.
    /// </summary>
    private string AppCode { get; set; } = string.Empty;


    /// <summary>
    /// Modelo obtenido.
    /// </summary>
    public AccountModel? Account { get; set; } = null;


    /// <summary>
    /// Ajustes.
    /// </summary>
    private AuthenticationSettings Settings { get; set; } = new();


    /// <summary>
    /// Establecer credenciales.
    /// </summary>
    /// <param name="username">Usuario.</param>
    /// <param name="password">Contraseña.</param>
    /// <param name="appCode">Código de restrictions.</param>
    public void SetCredentials(string username, string password, string appCode)
    {
        this.User = username;
        this.Password = password;
        this.AppCode = appCode;
    }


    /// <summary>
    /// Iniciar el proceso.
    /// </summary>
    public async Task<Responses> Start(AuthenticationSettings? settings = null)
    {

        // Validar.
        Settings = settings ?? new();

        // Obtener la cuenta.
        var account = await GetAccount();

        // Error.
        if (!account)
            return Responses.NotExistAccount;

        // Validar contraseña.
        bool password = ValidatePassword();

        if (!password)
            return Responses.InvalidPassword;

        // Validar aplicación.
        var valApp = await ValidateApp();

        // Bloqueado por la aplicación.
        if (!valApp)
            return Responses.UnauthorizedByApp;

        if (Settings.Log)
            await SaveLog();

        return Responses.Success;
    }


    /// <summary>
    /// Iniciar el proceso.
    /// </summary>
    private async Task<bool> GetAccount()
    {
        // Obtener la cuenta.
        var account = await accountData.Read(User, new()
        {
            FindOn = FindOn.StableAccounts,
            IsAdmin = true
        });

        // Establecer.
        Account = account.Model;

        // Respuesta.
        return account.Response == Responses.Success;
    }


    /// <summary>
    /// Validar la contraseña.
    /// </summary>
    private bool ValidatePassword()
    {

        // Validar la cuenta.
        if (Account == null)
            return false;

        // Validar la contraseña.
        if (Global.Utilities.Cryptography.Encrypt(Password) != Account.Password)
            return false;

        // Correcto.
        return true;

    }


    /// <summary>
    /// Guardar log.
    /// </summary>
    private async Task SaveLog()
    {
        await accountLogs.Create(new()
        {
            AccountId = Account!.Id,
            AuthenticationMethod = AuthenticationMethods.Password,
            Time = DateTime.Now,
            Application = Application
        });
    }


    /// <summary>
    /// Obtener el token.
    /// </summary>
    public string GenerateToken() => JwtService.Generate(Account!, Application!.Id);


    /// <summary>
    /// Obtener el token.
    /// </summary>
    public AccountModel GetData() => Account!;

}