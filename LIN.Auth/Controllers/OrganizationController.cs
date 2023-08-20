namespace LIN.Auth.Controllers;


[Route("orgs")]
public class OrganizationsController : ControllerBase
{



    /// <summary>
    /// Crea una organizaci�n
    /// </summary>
    /// <param name="modelo">Modelo de la organizaci�n</param>
    [HttpPost("create")]
    public async Task<HttpCreateResponse> Create([FromBody] OrganizationModel modelo)
    {

        // Comprobaciones
        if (modelo == null || modelo.Domain.Length <= 0 || modelo.Name.Length <= 0 || modelo.Members.Count <= 0)
            return new(Responses.InvalidParam);


        // Organizaci�n del modelo
        modelo.ID = 0;
        modelo.AppList = Array.Empty<AppOrganizationModel>();
       
        // Conexi�n
        (Conexi�n context, string connectionKey) = Conexi�n.GetOneConnection();

        // Creaci�n del usuario
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