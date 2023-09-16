namespace LIN.Identity.Areas.Organizations;


[Route("orgs/members")]
public class MemberController : ControllerBase
{


    /// <summary>
    /// Crea un nuevo miembro en una organizaci�n
    /// </summary>
    /// <param name="modelo">Modelo de la cuenta</param>
    /// <param name="token">Token de acceso de un administrador</param>
    /// <param name="rol">Rol asignado</param>
    [HttpPost("create")]
    public async Task<HttpCreateResponse> Create([FromBody] AccountModel modelo, [FromHeader] string token, [FromHeader] OrgRoles rol)
    {

        // Validaci�n del modelo.
        if (modelo == null || !modelo.Usuario.Trim().Any() || !modelo.Nombre.Trim().Any())
        {
            return new CreateResponse
            {
                Response = Responses.InvalidParam,
                Message = "Uno o varios par�metros inv�lidos."
            };
        }

        // Visibilidad oculta
        modelo.Visibilidad = AccountVisibility.Hidden;

        // Organizaci�n del modelo
        modelo = Controllers.Processors.AccountProcessor.Process(modelo);


        // Establece la contrase�a default
        string password = $"ChangePwd@{modelo.Creaci�n:dd.MM.yyyy}";

        // Contrase�a default
        modelo.Contrase�a = EncryptClass.Encrypt(Conexi�n.SecreteWord + password);

        // Validaci�n del token
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
                Message = "No se encontr� un usuario valido.",
                Response = Responses.Unauthorized
            };
        }

        // Si el usuario no tiene una organizaci�n
        if (userContext.Model.OrganizationAccess == null)
        {
            return new CreateResponse
            {
                Message = $"El usuario '{userContext.Model.Usuario}' no pertenece a una organizaci�n.",
                Response = Responses.Unauthorized
            };
        }

        // Verificaci�n del rol dentro de la organizaci�n
        if (!userContext.Model.OrganizationAccess.Rol.IsAdmin())
        {
            return new CreateResponse
            {
                Message = $"El usuario '{userContext.Model.Usuario}' no puede crear nuevos usuarios en esta organizaci�n.",
                Response = Responses.Unauthorized
            };
        }


        // Verificaci�n del rol dentro de la organizaci�n
        if (userContext.Model.OrganizationAccess.Rol.IsGretter(rol))
        {
            return new CreateResponse
            {
                Message = $"El '{userContext.Model.Usuario}' no puede crear nuevos usuarios con mas privilegios de los propios.",
                Response = Responses.Unauthorized
            };
        }


        // ID de la organizaci�n
        var org = userContext.Model.OrganizationAccess.Organization.ID;


        // Conexi�n
        (Conexi�n context, string connectionKey) = Conexi�n.GetOneConnection();

        // Creaci�n del usuario
        var response = await Data.Organizations.Members.Create(modelo, org, rol, context);

        // Evaluaci�n
        if (response.Response != Responses.Success)
            return new(response.Response);

        // Cierra la conexi�n
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
    /// Obtiene la lista de miembros asociados a una organizaci�n
    /// </summary>
    /// <param name="token">Token de acceso</param>
    [HttpGet]
    public async Task<HttpReadAllResponse<AccountModel>> ReadAll([FromHeader] string token)
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



        // Conexi�n
        (Conexi�n context, string connectionKey) = Conexi�n.GetOneConnection();

        context.CloseActions(connectionKey);

        // Retorna el resultado
        return members;

    }



}