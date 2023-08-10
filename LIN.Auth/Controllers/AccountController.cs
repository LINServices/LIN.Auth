namespace LIN.Auth.Controllers;


[Route("account")]
public class AccountController : ControllerBase
{


    /// <summary>
    /// Crea una cuenta
    /// </summary>
    /// <param name="modelo">Modelo del usuario</param>
    [HttpPost("create")]
    public async Task<HttpCreateResponse> Create([FromBody] AccountModel modelo)
    {

        // Comprobaciones
        if (modelo == null || modelo.Contrase�a.Length < 4 || modelo.Nombre.Length <= 0 || modelo.Usuario.Length <= 0)
            return new(Responses.InvalidParam);


        // Organizaci�n del modelo
        modelo.ID = 0;
        modelo.Contrase�a = EncryptClass.Encrypt(Conexi�n.SecreteWord + modelo.Contrase�a);
        modelo.Creaci�n = DateTime.Now;
        modelo.Estado = AccountStatus.Normal;
        modelo.Insignia = AccountBadges.None;
        modelo.Rol = AccountRoles.User;
        modelo.Perfil = modelo.Perfil.Length == 0
                               ? System.IO.File.ReadAllBytes("wwwroot/profile.png")
                               : modelo.Perfil;

        // IA Nombre (Genero)
        try
        {
            if (modelo.Genero == Genders.Undefined)
            {
                // Consulta
                //var sex = await Developers.IAName(modelo.Nombre.Trim().Split(" ")[0]);

                // Manejo
               // modelo.Sexo = sex.Model;
            }
        }
        catch
        {
        }

        // Conexi�n
        (Conexi�n context, string connectionKey) = Conexi�n.GetOneConnection();

        // Creaci�n del usuario
        var response = await Data.Users.Create(modelo, context);

        // Evaluaci�n
        if (response.Response != Responses.Success)
            return new(response.Response);

        context.CloseActions(connectionKey);

        // Genera el token

        // Retorna el resultado
        return response ?? new();

    }



    /// <summary>
    /// Obtiene un usuario por medio del ID
    /// </summary>
    /// <param name="id">ID del usuario</param>
    [HttpGet("read/id")]
    public async Task<HttpReadOneResponse<AccountModel>> ReadOneByID([FromQuery] int id)
    {

        if (id <= 0)
            return new(Responses.InvalidParam);

        // Obtiene el usuario
        var response = await Data.Users.Read(id, true);

        // Si es err�neo
        if (response.Response != Responses.Success)
            return new ReadOneResponse<AccountModel>()
            {
                Response = response.Response,
                Model = new()
            };

        // Retorna el resultado
        return response;

    }



    /// <summary>
    /// Obtiene un usuario por medio de el usuario Unico
    /// </summary>
    /// <param name="user">Usuario</param>
    [HttpGet("read/user")]
    public async Task<HttpReadOneResponse<AccountModel>> ReadOneByUser([FromQuery] string user)
    {

        if (!user.Any())
            return new(Responses.InvalidParam);

        // Obtiene el usuario
        var response = await Data.Users.Read(user, true);

        // Si es err�neo
        if (response.Response != Responses.Success)
            return new ReadOneResponse<AccountModel>()
            {
                Response = response.Response,
                Model = new()
            };

        // Retorna el resultado
        return response;

    }



    /// <summary>
    /// Obtiene una lista de 10 usuarios cullo usuario cumpla con un patron
    /// </summary>
    /// <param name="pattern">Patron de b�squeda</param>
    /// <param name="id">ID del usuario que esta buscando</param>
    [HttpGet("searchByPattern")]
    public async Task<HttpReadAllResponse<AccountModel>> ReadAllSearch([FromHeader] string pattern, [FromHeader] int id)
    {

        // Comprobaci�n
        if (id <= 0 || pattern.Trim().Length <= 0)
            return new(Responses.InvalidParam);


        // Obtiene el usuario
        var response = await Data.Users.SearchByPattern(pattern, id);

        return response;
    }




    /// <summary>
    /// Obtiene usuarios
    /// </summary>
    /// <param name="ids">Lista de IDs de los usuarios</param>
    [HttpPost("find")]
    public async Task<HttpReadAllResponse<AccountModel>> ReadAll([FromBody] List<int> ids)
    {

        // Obtiene el usuario
        var response = await Data.Users.FindAll(ids);

        return response;

    }











    /// <summary>
    /// Obtiene una lista de 5 usuarios cullo usuario cumpla con un patron (Solo admins)
    /// </summary>
    /// <param name="pattern">Patron de b�squeda</param>
    /// <param name="id">ID del usuario que esta buscando</param>
    [HttpGet("findAllUsers")]
    public async Task<HttpReadAllResponse<AccountModel>> ReadAllSearch([FromHeader] string pattern, [FromHeader] string token)
    {

        var (isValid, _, id) = Jwt.Validate(token);


        if (!isValid)
        {
            return new(Responses.Unauthorized);
        }


        var rol = (await Data.Users.Read(id, false)).Model.Rol;


        if (rol != AccountRoles.Admin)
            return new(Responses.InvalidParam);

        // Obtiene el usuario
        var response = await Data.Users.GetAll(pattern);

        return response;

    }



    /// <summary>
    /// Actualiza los datos de un usuario
    /// </summary>
    /// <param name="modelo">Nuevo modelo</param>
    [HttpPut("update")]
    public async Task<HttpResponseBase> Update([FromBody] AccountModel modelo, [FromHeader] string token)
    {

        var (isValid, _, userID) = Jwt.Validate(token);

        if (!isValid)
            return new ResponseBase
            {
                Response = Responses.Unauthorized,
                Message = "Token Invalido"
            };

        modelo.ID = userID;

        if (modelo.ID <= 0 || modelo.Nombre.Any())
            return new(Responses.InvalidParam);

        return await Data.Users.Update(modelo);

    }



    /// <summary>
    /// Actualiza el genero de un usuario
    /// </summary>
    /// <param name="token">Token de acceso</param>
    /// <param name="genero">Nuevo genero</param>
    [HttpPatch("update/gender")]
    public async Task<HttpResponseBase> UpdateGender([FromHeader] string token, [FromHeader] Genders genero)
    {


        var (isValid, _, id) = Jwt.Validate(token);


        if (!isValid)
        {
            return new(Responses.Unauthorized);
        }

        return await Data.Users.UpdateGender(id, genero);

    }



}