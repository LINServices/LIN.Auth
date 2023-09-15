namespace LIN.Identity.Areas.Organizations;


[Route("orgs/applications")]
public class ApplicationOrgsController : ControllerBase
{


    /// <summary>
    /// Obtiene la lista de aplicaciones asociadas a una organizaci�n
    /// </summary>
    /// <param name="token">Token de acceso</param>
    [HttpGet]
    public async Task<HttpReadAllResponse<ApplicationModel>> ReadApps([FromHeader] string token)
    {

        // Token
        var (isValid, _, _, orgID) = Jwt.Validate(token);

        // Token es invalido
        if (!isValid)
            return new ReadAllResponse<ApplicationModel>
            {
                Message = "Token invalido.",
                Response = Responses.Unauthorized
            };


        // Si no tiene ninguna organizaci�n
        if (orgID <= 0)
            return new ReadAllResponse<ApplicationModel>
            {
                Message = "No estas vinculado con ninguna organizaci�n.",
                Response = Responses.Unauthorized
            };


        // Obtiene las aplicaciones
        var org = await Data.Organizations.Organizations.ReadApps(orgID);

        // Su no se encontraron aplicaciones
        if (org.Response != Responses.Success)
            return new ReadAllResponse<ApplicationModel>
            {
                Message = "No found Organization",
                Response = Responses.Unauthorized
            };

        // Conexi�n
        (Conexi�n context, string connectionKey) = Conexi�n.GetOneConnection();

        context.CloseActions(connectionKey);

        // Retorna el resultado
        return org;

    }



    /// <summary>
    /// Insertar una aplicaci�n en una organizaci�n
    /// </summary>
    /// <param name="appUid">UId de la aplicaci�n</param>
    /// <param name="token">Token de acceso</param>
    [HttpPost("insert")]
    public async Task<HttpCreateResponse> InsertApp([FromQuery] string appUid, [FromHeader] string token)
    {

        // Token
        var (isValid, _, userID, _) = Jwt.Validate(token);


        // Si el token es invalido
        if (!isValid)
            return new CreateResponse
            {
                Message = "Token invalido",
                Response = Responses.Unauthorized
            };

        // Informaci�n del usuario
        var userData = await Data.Accounts.Read(userID, true, true, true);

        // Si no existe el usuario
        if (userData.Response != Responses.Success)
            return new CreateResponse
            {
                Message = "No se encontr� el usuario, talvez fue eliminado o desactivado.",
                Response = Responses.NotExistAccount
            };


        // Si no tiene organizaci�n
        if (userData.Model.OrganizationAccess == null || userData.Model.OrganizationAccess.Organization == null)
            return new CreateResponse
            {
                Message = $"El usuario '{userData.Model.Usuario}' no pertenece a una organizaci�n.",
                Response = Responses.Unauthorized
            };

        // Si el usuario no es admin en la organizaci�n
        if (!userData.Model.OrganizationAccess.Rol.IsAdmin())
            return new CreateResponse
            {
                Message = $"El usuario '{userData.Model.Usuario}' no tiene un rol administrador en la organizaci�n '{userData.Model.OrganizationAccess.Organization.Name}'",
                Response = Responses.Unauthorized
            };

        // Crea la aplicaci�n en la organizaci�n
        var res = await Data.Organizations.Applications.Create(appUid, userData.Model.OrganizationAccess.Organization.ID);

        // Si hubo une error
        if (res.Response != Responses.Success)
            return new CreateResponse
            {
                Message = $"Hubo un error al insertar esta aplicaci�n en la lista blanca permitidas de {userData.Model.OrganizationAccess.Organization.Name}",
                Response = Responses.Unauthorized
            };


        // Conexi�n
        (Conexi�n context, string connectionKey) = Conexi�n.GetOneConnection();

        context.CloseActions(connectionKey);

        // Retorna el resultado
        return new CreateResponse
        {
            LastID = res.LastID,
            Message = "",
            Response = Responses.Success
        }; ;

    }



    /// <summary>
    /// Buscar aplicaciones que no est�n vinculadas a una organizaci�n por medio del un par�metro
    /// </summary>
    /// <param name="param">Par�metro de b�squeda</param>
    /// <param name="token">Token de acceso</param>
    [HttpGet("search")]
    public async Task<HttpReadAllResponse<ApplicationModel>> Search([FromQuery] string param, [FromHeader] string token)
    {

        // Token
        var (isValid, _, _, orgID) = Jwt.Validate(token);

        // Valida el token
        if (!isValid || orgID <= 0)
        {
            return new(Responses.Unauthorized);
        }

        // Encuentra las apps
        var finds = await Data.Organizations.Applications.Search(param, orgID);

        return finds;
    }



}