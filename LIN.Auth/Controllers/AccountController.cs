namespace LIN.Auth.Controllers;


[Route("account")]
public class AccountController : ControllerBase
{


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


        // Conexi�n
        (Conexi�n context, string connectionKey) = Conexi�n.GetOneConnection();

        // Creaci�n del usuario
        var response = await Data.Accounts.Create(modelo, context);

        // Evaluaci�n
        if (response.Response != Responses.Success)
            return new(response.Response);

        context.CloseActions(connectionKey);

        // Obtiene el usuario
        string token = Jwt.Generate(response.Model);


        // Retorna el resultado
        return new CreateResponse()
        {
            LastID = response.Model.ID,
            Response = Responses.Success,
            Token = token,
            Message = "Success"
        };

    }




    [HttpGet("read/id")]
    public async Task<HttpReadOneResponse<AccountModel>> ReadOneByID([FromQuery] int id)
    {

        if (id <= 0)
            return new(Responses.InvalidParam);

        // Obtiene el usuario
        var response = await Data.Accounts.Read(id, true, false);

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



    [HttpGet("read/user")]
    public async Task<HttpReadOneResponse<AccountModel>> ReadOneByUser([FromQuery] string user)
    {

        if (!user.Any())
            return new(Responses.InvalidParam);

        // Obtiene el usuario
        var response = await Data.Accounts.Read(user, true, false);

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



    [HttpGet("searchByPattern")]
    public async Task<HttpReadAllResponse<AccountModel>> ReadAllSearch([FromHeader] string pattern, [FromHeader] string token)
    {

        // Comprobaci�n
        if (pattern.Trim().Length <= 0)
            return new(Responses.InvalidParam);

        // Info del token
        var (isValid, _, userID, _) = Jwt.Validate(token);

        // Token es invalido
        if (!isValid)
        {
            return new ReadAllResponse<AccountModel>
            {
                Message = "Token es invalido",
                Response = Responses.Unauthorized
            };
        }

        // Obtiene el usuario
        var response = await Data.Accounts.Search(pattern, userID, false);

        return response;
    }


    [HttpPost("find")]
    public async Task<HttpReadAllResponse<AccountModel>> ReadAll([FromBody] List<int> ids)
    {

        // Obtiene el usuario
        var response = await Data.Accounts.FindAll(ids);

        return response;

    }



    
    [HttpPatch("update/password")]
    public async Task<HttpResponseBase> Update([FromBody] UpdatePasswordModel modelo)
    {

        if (modelo.Account <= 0 || modelo.OldPassword.Length < 4 || modelo.NewPassword.Length < 4)
            return new(Responses.InvalidParam);


        var actualData = await Data.Accounts.Read(modelo.Account, true);

        if (actualData.Response != Responses.Success)
            return new(Responses.NotExistAccount);

        var oldEncrypted = actualData.Model.Contrase�a;


        if (oldEncrypted != actualData.Model.Contrase�a)
        {
            return new ResponseBase(Responses.InvalidPassword);
        }

        return await Data.Accounts.Update(modelo);

    }



    [HttpDelete("delete")]
    public async Task<HttpResponseBase> Delete([FromHeader] string token)
    {

        var (isValid, _, userID, _) = Jwt.Validate(token);

        if (!isValid)
            return new ResponseBase
            {
                Response = Responses.Unauthorized,
                Message = "Token invalido"
            };

        if (userID <= 0)
            return new(Responses.InvalidParam);

        var response = await Data.Accounts.Delete(userID);
        return response;
    }



    [HttpPatch("disable/account")]
    public async Task<HttpResponseBase> Disable([FromBody] AccountModel user)
    {

        if (user.ID <= 0)
        {
            return new(Responses.ExistAccount);
        }

        // Modelo de usuario de la BD
        var userModel = await Data.Accounts.Read(user.ID,true);

        if (userModel.Model.Contrase�a != EncryptClass.Encrypt(Conexi�n.SecreteWord + user.Contrase�a))
        {
            return new(Responses.InvalidPassword);
        }


        return await Data.Accounts.Update(user.ID, AccountStatus.Disable);

    }



    
    [HttpGet("findAllUsers")]
    public async Task<HttpReadAllResponse<AccountModel>> Finde([FromHeader] string pattern, [FromHeader] string token)
    {

        var (isValid, _, id, _) = Jwt.Validate(token);


        if (!isValid)
        {
            return new(Responses.Unauthorized);
        }


        var rol = (await Data.Accounts.Read(id, true)).Model.Rol;


        if (rol != AccountRoles.Admin)
            return new(Responses.Unauthorized);

        // Obtiene el usuario
        var response = await Data.Accounts.Search(pattern, 0, true);

        return response;

    }



    [HttpPut("update")]
    public async Task<HttpResponseBase> Update([FromBody] AccountModel modelo, [FromHeader] string token)
    {

        var (isValid, _, userID, _) = Jwt.Validate(token);

        if (!isValid)
            return new ResponseBase
            {
                Response = Responses.Unauthorized,
                Message = "Token Invalido"
            };

        modelo.ID = userID;

        if (modelo.ID <= 0 || modelo.Nombre.Any())
            return new(Responses.InvalidParam);

        return await Data.Accounts.Update(modelo);

    }



    [HttpPatch("update/gender")]
    public async Task<HttpResponseBase> UpdateGender([FromHeader] string token, [FromHeader] Genders genero)
    {


        var (isValid, _, id, _) = Jwt.Validate(token);


        if (!isValid)
        {
            return new(Responses.Unauthorized);
        }

        return await Data.Accounts.Update(id, genero);

    }



    [HttpPatch("update/visibility")]
    public async Task<HttpResponseBase> UpdateVisibility([FromHeader] string token, [FromHeader] AccountVisibility visibility)
    {


        var (isValid, _, id, _) = Jwt.Validate(token);

        if (!isValid)
        {
            return new(Responses.Unauthorized);
        }

        return await Data.Accounts.Update(id, visibility);

    }



}