namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class Accounts(DataContext context, ILogger<Accounts> logger)
{

    /// <summary>
    /// Crear nueva cuenta. [Transacción]
    /// </summary>
    /// <param name="modelo">Modelo de la cuenta.</param>
    /// <param name="organization">Id de la organización.</param>
    public async Task<ReadOneResponse<AccountModel>> Create(AccountModel modelo, int organization = 0)
    {

        // Pre.
        modelo.Id = 0;
        modelo.IdentityId = 0;

        // Transacción.
        using var transaction = context.Database.GetTransaction();

        try
        {

            // Guardar la cuenta.
            await context.Accounts.AddAsync(modelo);
            context.SaveChanges();

            // Si la organización existe.
            if (organization > 0)
            {

                var generalGroup = (from org in context.Organizations
                                    where org.Id == organization
                                    select org.DirectoryId).FirstOrDefault();

                if (generalGroup <= 0)
                {
                    throw new Exception("Organización no encontrada.");
                }

                var x = new GroupMember()
                {
                    Group = new()
                    {
                        Id = generalGroup
                    },
                    Identity = modelo.Identity,
                    Type = GroupMemberTypes.User
                };

                context.Attach(x.Group);

                context.GroupMembers.Add(x);

                context.SaveChanges();

            }


            // Confirmar los cambios.
            transaction?.Commit();

            return new()
            {
                Response = Responses.Success,
                Model = modelo
            };

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear cuenta");
            transaction?.Rollback();
            return new()
            {
                Response = Responses.ExistAccount
            };
        }

    }


    /// <summary>
    /// Obtener una cuenta según el Id.
    /// </summary>
    /// <param name="id">Id de la cuenta.</param>
    /// <param name="filters">Filtros de búsqueda.</param>
    public async Task<ReadOneResponse<AccountModel>> Read(int id, QueryObjectFilter filters)
    {

        try
        {

            // Consulta de las cuentas.
            var account = await Account.GetAccounts(id, filters, context).FirstOrDefaultAsync();

            // Si la cuenta no existe.
            if (account == null)
                return new()
                {
                    Response = Responses.NotRows
                };

            // Success.
            return new()
            {
                Response = Responses.Success,
                Model = account
            };

        }
        catch (Exception)
        {
            return new()
            {
                Response = Responses.ExistAccount
            };
        }

    }


    /// <summary>
    /// Obtener una cuenta según el identificador único.
    /// </summary>
    /// <param name="unique">Único.</param>
    /// <param name="filters">Filtros de búsqueda.</param>
    public async Task<ReadOneResponse<AccountModel>> Read(string unique, QueryObjectFilter filters)
    {
        try
        {

            // Consulta de las cuentas.
            var account = await Account.GetAccounts(unique, filters, context).FirstOrDefaultAsync();

            // Si la cuenta no existe.
            if (account == null)
                return new()
                {
                    Response = Responses.NotRows
                };

            // Success.
            return new()
            {
                Response = Responses.Success,
                Model = account
            };

        }
        catch (Exception)
        {
            return new()
            {
                Response = Responses.ExistAccount
            };
        }

    }


    /// <summary>
    /// Obtener una cuenta según el id de la identidad.
    /// </summary>
    /// <param name="id">Id de la identidad.</param>
    /// <param name="filters">Filtros de búsqueda.</param>
    public async Task<ReadOneResponse<AccountModel>> ReadByIdentity(int id, QueryObjectFilter filters)
    {

        try
        {

            // Consulta de las cuentas.
            var account = await Account.GetAccountsByIdentity(id, filters, context).FirstOrDefaultAsync();

            // Si la cuenta no existe.
            if (account == null)
                return new()
                {
                    Response = Responses.NotRows
                };

            // Success.
            return new()
            {
                Response = Responses.Success,
                Model = account
            };

        }
        catch (Exception)
        {
            return new()
            {
                Response = Responses.ExistAccount
            };
        }

    }


    /// <summary>
    /// Buscar por patron.
    /// </summary>
    /// <param name="pattern">patron de búsqueda</param>
    /// <param name="filters">Filtros</param>
    public async Task<ReadAllResponse<AccountModel>> Search(string pattern, QueryObjectFilter filters)
    {

        // Ejecución
        try
        {

            List<AccountModel> accountModels = await Account.Search(pattern, filters, context).Take(10).ToListAsync();

            // Si no existe el modelo
            if (accountModels == null || !accountModels.Any())
                return new(Responses.NotRows);

            return new(Responses.Success, accountModels);
        }
        catch (Exception)
        {
        }

        return new();
    }


    /// <summary>
    /// Obtiene los usuarios con IDs coincidentes
    /// </summary>
    /// <param name="ids">Lista de IDs</param>
    public async Task<ReadAllResponse<AccountModel>> FindAll(List<int> ids, QueryObjectFilter filters)
    {

        // Ejecución
        try
        {

            var query = Account.FindAll(ids, filters, context);

            // Ejecuta
            var result = await query.ToListAsync();

            // Si no existe el modelo
            if (result == null || !result.Any())
                return new(Responses.NotRows);

            return new(Responses.Success, result);
        }
        catch (Exception)
        {
        }

        return new();
    }


    /// <summary>
    /// Obtiene los usuarios con IDs coincidentes
    /// </summary>
    /// <param name="ids">Lista de IDs</param>
    public async Task<ReadAllResponse<AccountModel>> FindAllByIdentities(List<int> ids, QueryObjectFilter filters)
    {

        // Ejecución
        try
        {

            var query = Account.FindAllByIdentities(ids, filters, context);

            // Ejecuta
            var result = await query.ToListAsync();

            // Si no existe el modelo
            if (result == null || !result.Any())
                return new(Responses.NotRows);

            return new(Responses.Success, result);
        }
        catch (Exception)
        {
        }

        return new();
    }


    /// <summary>
    /// Actualizar contraseña de una cuenta.
    /// </summary>
    /// <param name="accountId">Id de la cuenta.</param>
    /// <param name="password">Nueva contraseña.</param>
    public async Task<ResponseBase> UpdatePassword(int accountId, string password)
    {

        try
        {
            var account = await (from a in context.Accounts
                                 where a.Id == accountId
                                 select a).ExecuteUpdateAsync(Accounts => Accounts.SetProperty(t => t.Password, password));

            if (account <= 0)
                return new(Responses.NotExistAccount);

            return new(Responses.Success);
        }
        catch (Exception)
        {
            return new()
            {
                Response = Responses.ExistAccount
            };
        }

    }

}