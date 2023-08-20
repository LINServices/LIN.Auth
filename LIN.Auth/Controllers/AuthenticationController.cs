namespace LIN.Auth.Controllers;


[Route("authentication")]
public class AuthenticationController : ControllerBase
{


    /// <summary>
    /// Inicia una sesi�n de usuario
    /// </summary>
    /// <param name="user">Usuario �nico</param>
    /// <param name="password">Contrase�a del usuario</param>
    /// <param name="application">Key de aplicaci�n</param>
    [HttpGet("login")]
    public async Task<HttpReadOneResponse<AccountModel>> Login([FromQuery] string user, [FromQuery] string password, [FromHeader] string application)
    {

        // Validaci�n de par�metros.
        if (!user.Any() || !password.Any() || !application.Any())
            return new(Responses.InvalidParam);

        // Obtiene la App.
        var app = await Data.Applications.Read(application);

        // Verifica si la app existe.
        if (app.Response != Responses.Success)
        {
            return new ReadOneResponse<AccountModel>
            {
                Message = "La aplicaci�n no esta autorizada para iniciar sesi�n en LIN Identity",
                Response = Responses.Unauthorized
            };
        }


        // Obtiene el usuario.
        var response = await Data.Accounts.Read(user, true, true, true);

        // Validaci�n al obtener el usuario
        switch (response.Response)
        {
            // Correcto
            case Responses.Success:
                break;

            // Incorrecto
            default:
                return new(response.Response);
        }

        // Valida el estado de la cuenta
        if (response.Model.Estado != AccountStatus.Normal)
            return new(Responses.NotExistAccount);

        // Valida la contrase�a
        if (response.Model.Contrase�a != EncryptClass.Encrypt(Conexi�n.SecreteWord + password))
            return new(Responses.InvalidPassword);


        // Obtiene la organizaci�n
        var org = response.Model.Organization;

        // Validaciones de la organizaci�n
        if (org != null)
        {

            var have = org.AppList.Where(T => T.App.Key == application).FirstOrDefault();

            if (have == null || have.Estado == false)
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

        if (response.Model.Organization != null)
        {
            response.Model.Organization.AppList = Array.Empty<AppOrganizationModel>().ToList();
            response.Model.Organization.Members = Array.Empty<AccountModel>().ToList();
        }

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