namespace LIN.Auth.Controllers;


[Route("orgs")]
public class OrganizationsController : ControllerBase
{


    /// <summary>
    /// Crea una organización
    /// </summary>
    /// <param name="modelo">Modelo de la organización</param>
    [HttpPost("create")]
    public async Task<HttpCreateResponse> Create([FromBody] OrganizationModel modelo, [FromHeader] string token)
    {

        // Comprobaciones
        if (modelo == null || modelo.Domain.Length <= 0 || modelo.Name.Length <= 0)
            return new(Responses.InvalidParam);



        // Token
        var (isValid, _, userID, _) = Jwt.Validate(token);

        // Validación del token
        if (!isValid)
            return new CreateResponse()
            {
                Response = Responses.Unauthorized,
                Message = "Token invalido"
            };


        // Conexión
        (Conexión context, string connectionKey) = Conexión.GetOneConnection();

        // Obtiene la cuenta
        var account = await Data.Accounts.Read(userID, true, true, true, context);

        // Validación de la cuenta
        if (account.Response != Responses.Success)
        {
            return new CreateResponse()
            {
                Response = Responses.Unauthorized,
                Message = "No se encontró el usuario"
            };
        }


        // Si ya el usuario tiene organización
        if (account.Model.OrganizationAccess != null)
        {
            return new CreateResponse()
            {
                Response = Responses.UnauthorizedByOrg,
                Message = "Ya perteneces a una organización."
            };
        }


        // Organización del modelo
        modelo.ID = 0;
        modelo.AppList = new();
        modelo.Members = new();

        // Creación de la organización
        var response = await Data.Organizations. Organizations.Create(modelo, userID, context);

        // Evaluación
        if (response.Response != Responses.Success)
            return new(response.Response);

        context.CloseActions(connectionKey);

        // Retorna el resultado
        return new CreateResponse()
        {
            LastID = response.Model.ID,
            Response = Responses.Success,
            Message = "Success"
        };

    }



    /// <summary>
    /// Obtiene una organización por medio del ID
    /// </summary>
    /// <param name="id">ID de la organización</param>
    [HttpGet("read/id")]
    public async Task<HttpReadOneResponse<OrganizationModel>> ReadOneByID([FromQuery] int id)
    {

        if (id <= 0)
            return new(Responses.InvalidParam);

        // Obtiene el usuario
        var response = await Data.Organizations.Organizations.Read(id);

        // Si es erróneo
        if (response.Response != Responses.Success)
            return new ReadOneResponse<OrganizationModel>()
            {
                Response = response.Response,
                Model = new()
            };

        // Retorna el resultado
        return response;

    }



    /// <summary>
    /// Obtiene una organización por medio del ID
    /// </summary>
    /// <param name="id">ID de la organización</param>
    [HttpPatch("update/whitelist")]
    public async Task<HttpResponseBase> Update([FromHeader] string token, [FromQuery] bool haveWhite)
    {


        var (isValid, _, userID, _) = Jwt.Validate(token);


        if (!isValid)
        {
            return new(Responses.Unauthorized);
        }



        var userContext = await Data.Accounts.Read(userID, true, false, true);

        // Error al encontrar el usuario
        if (userContext.Response != Responses.Success)
        {
            return new ResponseBase
            {
                Message = "No se encontró un usuario valido.",
                Response = Responses.Unauthorized
            };
        }

        // Si el usuario no tiene una organización
        if (userContext.Model.OrganizationAccess == null)
        {
            return new ResponseBase
            {
                Message = $"El usuario '{userContext.Model.Usuario}' no pertenece a una organización.",
                Response = Responses.Unauthorized
            };
        }

        // Verificación del rol dentro de la organización
        if (!userContext.Model.OrganizationAccess.Rol.IsAdmin())
        {
            return new ResponseBase
            {
                Message = $"El usuario '{userContext.Model.Usuario}' no puede actualizar el estado de la lista blanca de esta organización.",
                Response = Responses.Unauthorized
            };
        }


        var response = await Data.Organizations.Organizations.UpdateState(userContext.Model.OrganizationAccess.Organization.ID, haveWhite);

        // Retorna el resultado
        return response;

    }





























    /// <summary>
    /// Crea una cuenta en una organización
    /// </summary>
    /// <param name="modelo">Modelo del usuario</param>
    [HttpPost("create/member")]
    public async Task<HttpCreateResponse> Create([FromBody] AccountModel modelo, [FromHeader] string token, [FromHeader] OrgRoles rol)
    {

        // Validación del modelo.
        if (modelo == null || !modelo.Usuario.Trim().Any() || !modelo.Nombre.Trim().Any())
        {
            return new CreateResponse
            {
                Response = Responses.InvalidParam,
                Message = "Uno o varios parámetros inválidos."
            };
        }

        // Organización del modelo
        modelo.ID = 0;
        modelo.Creación = DateTime.Now;
        modelo.Estado = AccountStatus.Normal;
        modelo.Insignia = AccountBadges.None;
        modelo.Rol = AccountRoles.User;
        modelo.OrganizationAccess = null;
        modelo.Perfil = modelo.Perfil.Length == 0
                               ? System.IO.File.ReadAllBytes("wwwroot/profile.png")
                               : modelo.Perfil;


        // Establece la contraseña default
        string password = $"ChangePwd@{modelo.Creación:dd.MM.yyyy}";

        // Contraseña default
        modelo.Contraseña = EncryptClass.Encrypt(Conexión.SecreteWord + password);

        // Validación del token
        var (isValid, _, userID, _) = Jwt.Validate(token);

        // Token es invalido
        if (!isValid)
        {
            return new CreateResponse
            {
                Message = "Token invalido.",
                Response = Responses.Unauthorized
            };
        }


        // Obtiene el usuario
        var userContext = await Data.Accounts.Read(userID, true, false, true);

        // Error al encontrar el usuario
        if (userContext.Response != Responses.Success)
        {
            return new CreateResponse
            {
                Message = "No se encontró un usuario valido.",
                Response = Responses.Unauthorized
            };
        }

        // Si el usuario no tiene una organización
        if (userContext.Model.OrganizationAccess == null)
        {
            return new CreateResponse
            {
                Message = $"El usuario '{userContext.Model.Usuario}' no pertenece a una organización.",
                Response = Responses.Unauthorized
            };
        }

        // Verificación del rol dentro de la organización
        if (!userContext.Model.OrganizationAccess.Rol.IsAdmin())
        {
            return new CreateResponse
            {
                Message = $"El usuario '{userContext.Model.Usuario}' no puede crear nuevos usuarios en esta organización.",
                Response = Responses.Unauthorized
            };
        }


        // Verificación del rol dentro de la organización
        if (userContext.Model.OrganizationAccess.Rol.IsGretter(rol))
        {
            return new CreateResponse
            {
                Message = $"El '{userContext.Model.Usuario}' no puede crear nuevos usuarios con mas privilegios de los propios.",
                Response = Responses.Unauthorized
            };
        }




        // ID de la organización
        var org = userContext.Model.OrganizationAccess.Organization.ID;


        // Conexión
        (Conexión context, string connectionKey) = Conexión.GetOneConnection();

        // Creación del usuario
        var response = await Data.Organizations.Members.Create(modelo, org, rol, context);

        // Evaluación
        if (response.Response != Responses.Success)
            return new(response.Response);

        // Cierra la conexión
        context.CloseActions(connectionKey);

        // Retorna el resultado
        return new CreateResponse()
        {
            LastID = response.Model.ID,
            Response = Responses.Success,
            Message = "Success"
        };

    }











    /// <summary>
    /// 
    /// </summary>
    /// <param name="modelo">Modelo del usuario</param>
    [HttpGet("members")]
    public async Task<HttpReadAllResponse<AccountModel>> Create([FromHeader] string token)
    {

        var (isValid, _, _, orgID) = Jwt.Validate(token);


        if (!isValid)
        {
            return new ReadAllResponse<AccountModel>
            {
                Message = "",
                Response = Responses.Unauthorized
            };
        }

        var members = await Data.Organizations.Members.ReadAll(orgID);


        if (members.Response != Responses.Success)
        {
            return new ReadAllResponse<AccountModel>
            {
                Message = "No found Organization",
                Response = Responses.Unauthorized
            };
        }



        // Conexión
        (Conexión context, string connectionKey) = Conexión.GetOneConnection();

        context.CloseActions(connectionKey);

        // Retorna el resultado
        return members;

    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="modelo">Modelo del usuario</param>
    [HttpGet("apps")]
    public async Task<HttpReadAllResponse<ApplicationModel>> w([FromHeader] string token)
    {

        var (isValid, _, _, orgID) = Jwt.Validate(token);


        if (!isValid)
        {
            return new ReadAllResponse<ApplicationModel>
            {
                Message = "",
                Response = Responses.Unauthorized
            };
        }


        var org = await Data.Organizations.Organizations.ReadApps(orgID);


        if (org.Response != Responses.Success)
        {
            return new ReadAllResponse<ApplicationModel>
            {
                Message = "No found Organization",
                Response = Responses.Unauthorized
            };
        }



        // Conexión
        (Conexión context, string connectionKey) = Conexión.GetOneConnection();

        context.CloseActions(connectionKey);

        // Retorna el resultado
        return org;

    }


}