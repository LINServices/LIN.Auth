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



		// Conexi�n
		(Conexi�n context, string connectionKey) = Conexi�n.GetOneConnection();


		// Organizaci�n del modelo
		modelo.ID = 0;
		modelo.AppList = new();

		modelo.Members[0].Member = Processors.AccountProcessor.Process(modelo.Members[0].Member);
		foreach (var member in modelo.Members)
		{
			member.Rol = OrgRoles.SuperManager;
			member.Organization = modelo;
		}

		// Creaci�n de la organizaci�n
		var response = await Data.Organizations.Organizations.Create(modelo, context);

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
		var response = await Data.Organizations.Organizations.Read(id);

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



	[HttpPatch("update/whitelist")]
	public async Task<HttpResponseBase> Update([FromHeader] string token, [FromQuery] bool haveWhite)
	{


		var (isValid, _, userID, _) = Jwt.Validate(token);


		if (!isValid)
			return new(Responses.Unauthorized);


		var userContext = await Data.Accounts.Read(userID, true, true, true);

		// Error al encontrar el usuario
		if (userContext.Response != Responses.Success)
		{
			return new ResponseBase
			{
				Message = "No se encontr� un usuario valido.",
				Response = Responses.Unauthorized
			};
		}

		// Si el usuario no tiene una organizaci�n
		if (userContext.Model.OrganizationAccess == null)
		{
			return new ResponseBase
			{
				Message = $"El usuario '{userContext.Model.Usuario}' no pertenece a una organizaci�n.",
				Response = Responses.Unauthorized
			};
		}

		// Verificaci�n del rol dentro de la organizaci�n
		if (!userContext.Model.OrganizationAccess.Rol.IsAdmin())
		{
			return new ResponseBase
			{
				Message = $"El usuario '{userContext.Model.Usuario}' no puede actualizar el estado de la lista blanca de esta organizaci�n.",
				Response = Responses.Unauthorized
			};
		}


		var response = await Data.Organizations.Organizations.UpdateState(userContext.Model.OrganizationAccess.Organization.ID, haveWhite);

		// Retorna el resultado
		return response;

	}


	[HttpPatch("update/access")]
	public async Task<HttpResponseBase> UpdateAccess([FromHeader] string token, [FromQuery] bool state)
	{


		var (isValid, _, userID, _) = Jwt.Validate(token);


		if (!isValid)
			return new(Responses.Unauthorized);


		var userContext = await Data.Accounts.Read(userID, true, false, true);

		// Error al encontrar el usuario
		if (userContext.Response != Responses.Success)
		{
			return new ResponseBase
			{
				Message = "No se encontr� un usuario valido.",
				Response = Responses.Unauthorized
			};
		}

		// Si el usuario no tiene una organizaci�n
		if (userContext.Model.OrganizationAccess == null)
		{
			return new ResponseBase
			{
				Message = $"El usuario '{userContext.Model.Usuario}' no pertenece a una organizaci�n.",
				Response = Responses.Unauthorized
			};
		}

		// Verificaci�n del rol dentro de la organizaci�n
		if (userContext.Model.OrganizationAccess.Rol != OrgRoles.SuperManager)
		{
			return new ResponseBase
			{
				Message = $"El usuario '{userContext.Model.Usuario}' no puede actualizar el estado de accesos de esta organizaci�n.",
				Response = Responses.Unauthorized
			};
		}


		var response = await Data.Organizations.Organizations.UpdateAccess(userContext.Model.OrganizationAccess.Organization.ID, state);

		// Retorna el resultado
		return response;

	}





























	/// <summary>
	/// Crea una cuenta en una organizaci�n
	/// </summary>
	/// <param name="modelo">Modelo del usuario</param>
	[HttpPost("create/member")]
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

		// Organizaci�n del modelo
		modelo.ID = 0;
		modelo.Creaci�n = DateTime.Now;
		modelo.Estado = AccountStatus.Normal;
		modelo.Insignia = AccountBadges.None;
		modelo.Rol = AccountRoles.User;
		modelo.OrganizationAccess = null;
		modelo.Perfil = modelo.Perfil.Length == 0
							   ? System.IO.File.ReadAllBytes("wwwroot/profile.png")
							   : modelo.Perfil;


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



		// Conexi�n
		(Conexi�n context, string connectionKey) = Conexi�n.GetOneConnection();

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



		// Conexi�n
		(Conexi�n context, string connectionKey) = Conexi�n.GetOneConnection();

		context.CloseActions(connectionKey);

		// Retorna el resultado
		return org;

	}







	[HttpPost("insert/app")]
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
		var res = await Data.Organizations.Applications.CreateOn(appUid, userData.Model.OrganizationAccess.Organization.ID);

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
	/// 
	/// </summary>
	/// <param name="modelo">Modelo del usuario</param>
	[HttpGet("search/apps")]
	public async Task<HttpReadAllResponse<ApplicationModel>> Search([FromQuery] string param, [FromHeader] string token)
	{


		var (isValid, _, _, orgID) = Jwt.Validate(token);

		if (!isValid)
		{
			return new(Responses.Undefined);
		}


		var finds = await Data.Applications.Search(param, orgID);

		return finds;
	}







}