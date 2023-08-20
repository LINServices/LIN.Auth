namespace LIN.Auth.Controllers;


[Route("authentication")]
public class AuthenticationController : ControllerBase
{


    /// <summary>
    /// Inicia una sesi�n de usuario
    /// </summary>
    /// <param name="user">Usuario �nico</param>
    /// <param name="password">Contrase�a del usuario</param>
    [HttpGet("login")]
    public async Task<HttpReadOneResponse<AccountModel>> Login([FromQuery] string user, [FromQuery] string password, [FromHeader] string application)
    {

        // Comprobaci�n
        if (!user.Any() || !password.Any())
            return new(Responses.InvalidParam);

        // Obtiene el usuario
        var response = await Data.Accounts.Read(user, true);

        if (response.Response != Responses.Success)
            return new(response.Response);

        if (response.Model.Estado != AccountStatus.Normal)
            return new(Responses.NotExistAccount);

        if (response.Model.Contrase�a != EncryptClass.Encrypt(Conexi�n.SecreteWord + password))
            return new(Responses.InvalidPassword);



        var org = response.Model.Organization;

        var app = await Data.Applications.Read(application);

        if (app.Response != Responses.Success)
        {
            return new ReadOneResponse<AccountModel>
            {
                Message = "La aplicaci�n no esta autorizada para iniciar sesi�n en LIN Identity",
                Response = Responses.Unauthorized
            };
        }

        if (org != null)
        {



            var have = org.AppList.Where(T => T.App.Key == application).FirstOrDefault();



            if (have?.Estado == false)
            {
                return new ReadOneResponse<AccountModel>
                {
                    Message = "Tu organizaci�n no permite iniciar sesi�n en esta aplicaci�n.",
                    Response = Responses.UnauthorizedByOrg
                };
            }

        }





        // Genera el token
        var token = Jwt.Generate(response.Model);

        // Crea registro del login
        _ = Data.Logins.Create(new()
        {
            Date = DateTime.Now,
            AccountID = response.Model.ID,
            ApplicationID = app.Model.ID
        });

        response.Token = token;
        return response;

    }



    /// <summary>
    /// Inicia una sesi�n de usuario por medio del token
    /// </summary>
    /// <param name="token">Token de acceso</param>
    [HttpGet("LoginWithToken")]
    public async Task<HttpReadOneResponse<AccountModel>> LoginWithToken([FromHeader] string token, [FromHeader] string application)
    {

        // Valida el token
        (var isValid, var user, var _) = Jwt.Validate(token);

        if (!isValid)
            return new(Responses.InvalidParam);


        // Obtiene el usuario
        var response = await Data.Accounts.Read(user, true);

        if (response.Response != Responses.Success)
            return new(response.Response);

        if (response.Model.Estado != AccountStatus.Normal)
            return new(Responses.NotExistAccount);

        // Crea registro del login
        _ = Data.Logins.Create(new()
        {
            Date = DateTime.Now,
            AccountID = response.Model.ID
        });

        response.Token = token;
        return response;

    }



}