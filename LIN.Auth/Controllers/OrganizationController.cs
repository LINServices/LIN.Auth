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

        // Obtiene la cuenta
        var account = await Data.Accounts.Read(userID, true, true, true);

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
        if (account.Model.Organization != null)
        {
            return new CreateResponse()
            {
                Response = Responses.UnauthorizedByOrg,
                Message = "Ya perteneces a una organizaci�n."
            };
        }


        // Organizaci�n del modelo
        modelo.ID = 0;
        modelo.AppList = Array.Empty<AppOrganizationModel>();
        modelo.Members = Array.Empty<AccountModel>();


        // Conexi�n
        (Conexi�n context, string connectionKey) = Conexi�n.GetOneConnection();


        modelo.Members.Add(account.Model);

        // Creaci�n de la organizaci�n
        var response = await Data.Organizations.Create(modelo, context);

        // Evaluaci�n
        if (response.Response != Responses.Success)
            return new(response.Response);


        context.CloseActions(connectionKey);

        // Retorna el resultado
        return new CreateResponse()
        {
            LastID = response.LastID,
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



}