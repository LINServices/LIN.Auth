namespace LIN.Auth.Controllers;


[Route("orgs")]
public class OrganizationsController : ControllerBase
{



    /// <summary>
    /// Crea una organizaci�n
    /// </summary>
    /// <param name="modelo">Modelo de la organizaci�n</param>
    [HttpPost("create")]
    public async Task<HttpCreateResponse> Create([FromBody] OrganizationModel modelo, [FromHeader] string token)
    {

        // Comprobaciones
        if (modelo == null || modelo.Domain.Length <= 0 || modelo.Name.Length <= 0)
            return new(Responses.InvalidParam);

        // Token
        var (isValid, _, userID) = Jwt.Validate(token);

        // Validaci�n del token
        if (!isValid)
            return new CreateResponse()
            {
                Response = Responses.Unauthorized,
                Message = "Token invalido"
            };


        // Conexi�n
        (Conexi�n context, string connectionKey) = Conexi�n.GetOneConnection();

        // Obtiene la cuenta
        var account = await Data.Accounts.Read(userID, true, true, true, context);

        // Validaci�n de la cuenta
        if (account.Response != Responses.Success)
        {
            return new CreateResponse()
            {
                Response = Responses.Unauthorized,
                Message = "No se encontr� el usuario"
            };
        }


        // Si ya el usuario tiene organizaci�n
        if (account.Model.OrganizationAccess != null)
        {
            return new CreateResponse()
            {
                Response = Responses.UnauthorizedByOrg,
                Message = "Ya perteneces a una organizaci�n."
            };
        }


        // Organizaci�n del modelo
        modelo.ID = 0;
        modelo.AppList = new();
        modelo.Members = new();

        // Creaci�n de la organizaci�n
        var response = await Data.Organizations.Create(modelo, userID, context);

        // Evaluaci�n
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
    /// Obtiene una organizaci�n por medio del ID
    /// </summary>
    /// <param name="id">ID de la organizaci�n</param>
    [HttpGet("read/id")]
    public async Task<HttpReadOneResponse<OrganizationModel>> ReadOneByID([FromQuery] int id)
    {

        if (id <= 0)
            return new(Responses.InvalidParam);

        // Obtiene el usuario
        var response = await Data.Organizations.Read(id);

        // Si es err�neo
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
    /// Crea una cuenta
    /// </summary>
    /// <param name="modelo">Modelo del usuario</param>
    [HttpPost("create/member")]
    public async Task<HttpCreateResponse> Create([FromBody] AccountModel modelo, [FromHeader] string token)
    {

        // Comprobaciones
        if (modelo == null || modelo.Contrase�a.Length < 4 || modelo.Nombre.Length <= 0 || modelo.Usuario.Length <= 0)
            return new(Responses.InvalidParam);


        // Organizaci�n del modelo
        modelo.ID = 0;
        modelo.Creaci�n = DateTime.Now;
        modelo.Estado = AccountStatus.Normal;
        modelo.Insignia = AccountBadges.None;
        modelo.Rol = AccountRoles.User;
        modelo.Perfil = modelo.Perfil.Length == 0
                               ? System.IO.File.ReadAllBytes("wwwroot/profile.png")
                               : modelo.Perfil;

        // Contrase�a default
        modelo.Contrase�a = EncryptClass.Encrypt(Conexi�n.SecreteWord + $"ChangePwd@{modelo.Creaci�n:dd-MM-yyyy}");


        var (isValid, _, userID) = Jwt.Validate(token);


        if (!isValid)
        {
            return new CreateResponse
            {
                Message = "",
                Response = Responses.Unauthorized
            };
        }

        var user = await Data.Accounts.Read(userID, true);

        if (user.Response != Responses.Success)
        {
            return new CreateResponse
            {
                Message = "No user found",
                Response = Responses.Unauthorized
            };
        }

        if (user.Model.OrganizationAccess == null)
        {
            return new CreateResponse
            {
                Message = "No org found",
                Response = Responses.Unauthorized
            };
        }



        var org = await Data.Organizations.Read(user.Model.OrganizationAccess.Organization.ID);


        if (org.Response != Responses.Success)
        {
            return new CreateResponse
            {
                Message = "No found Organization",
                Response = Responses.Unauthorized
            };
        }    



        // Conexi�n
        (Conexi�n context, string connectionKey) = Conexi�n.GetOneConnection();

        // Creaci�n del usuario
        var response = await Data.Accounts.Create(modelo, org.Model.ID, context);

        // Evaluaci�n
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



}