namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework.Builders;

public class Account
{


    /// <summary>
    /// Buscar en la cuentas estables.
    /// </summary>
    /// <param name="context">Contexto de base de datos.</param>
    public static IQueryable<AccountModel> OnStable(DataContext context)
    {

        // Hora actual.
        var now = DateTime.Now;

        // Consulta.
        var query = from account in context.Accounts
                    where account.Identity.Status != IdentityStatus.Disable
                    && account.Identity.EffectiveTime < now && account.Identity.ExpirationTime > now
                    select account;

        // Retornar.
        return query;
    }



    /// <summary>
    /// Buscar en todas las cuentas.
    /// </summary>
    /// <param name="context">Contexto de base de datos.</param>
    public static IQueryable<AccountModel> OnAll(DataContext context)
    {

        // Hora actual.
        var now = DateTime.Now;

        // Consulta.
        var query = from account in context.Accounts
                    select account;

        // Retornar.
        return query;
    }



    /// <summary>
    /// Obtener cuentas.
    /// </summary>
    /// <param name="id">Id de la cuenta</param>
    /// <param name="filters">Filtros</param>
    /// <param name="context">Contexto</param>
    public static IQueryable<AccountModel> GetAccounts(int id, QueryObjectFilter filters, DataContext context)
    {

        // Query general
        IQueryable<AccountModel> accounts;

        if (filters.FindOn == FindOn.StableAccounts)
            accounts = from account in OnStable(context)
                       where account.Id == id
                       select account;
        else
            accounts = from account in OnAll(context)
                       where account.Id == id
                       select account;

        // Armar el modelo
        accounts = BuildModel(accounts, filters, context);

        // Retorno
        return accounts;

    }



    /// <summary>
    /// Obtener cuentas.
    /// </summary>
    /// <param name="user">Identidad unica.</param>
    /// <param name="filters">Filtros</param>
    /// <param name="context">Contexto</param>
    public static IQueryable<AccountModel> GetAccounts(string user, QueryObjectFilter filters, DataContext context)
    {

        // Query general
        IQueryable<AccountModel> accounts;

        if (filters.FindOn == FindOn.StableAccounts)
            accounts = from account in OnStable(context)
                       where account.Identity.Unique == user
                       select account;
        else
            accounts = from account in OnAll(context)
                       where account.Identity.Unique == user
                       select account;

        // Armar el modelo
        accounts = BuildModel(accounts, filters, context);

        // Retorno
        return accounts;

    }



    /// <summary>
    /// Obtener cuentas.
    /// </summary>
    /// <param name="id">Id de la Identidad.</param>
    /// <param name="filters">Filtros</param>
    /// <param name="context">Contexto</param>
    public static IQueryable<AccountModel> GetAccountsByIdentity(int id, QueryObjectFilter filters, DataContext context)
    {

        // Query general
        IQueryable<AccountModel> accounts;

        if (filters.FindOn == FindOn.StableAccounts)
            accounts = from account in OnStable(context)
                       where account.Identity.Id == id
                       select account;
        else
            accounts = from account in OnAll(context)
                       where account.Identity.Id == id
                       select account;

        // Armar el modelo
        accounts = BuildModel(accounts, filters, context);

        // Retorno
        return accounts;

    }





    public static IQueryable<AccountModel> Search(string pattern, QueryObjectFilter filters, DataContext context)
    {

        // Query general.
        IQueryable<AccountModel> accounts = from account in OnStable(context)
                                            where account.Identity.Unique.Contains(pattern)
                                            select account;

        // Armar el modelo.
        accounts = BuildModel(accounts, filters, context);

        // Retorno
        return accounts;

    }




    public static IQueryable<AccountModel> FindAll(IEnumerable<int> ids, QueryObjectFilter filters, DataContext context)
    {
        IQueryable<AccountModel> accounts;

        if (filters.FindOn == FindOn.StableAccounts)
        {
            accounts = from account in OnStable(context)
                       where ids.Contains(account.Id)
                       select account;
        }
        else
        {
            accounts = from account in OnAll(context)
                       where ids.Contains(account.Id)
                       select account;
        }


        // Armar el modelo.
        accounts = BuildModel(accounts, filters, context);

        // Retorno
        return accounts;

    }






    public static IQueryable<AccountModel> FindAllByIdentities(IEnumerable<int> ids, QueryObjectFilter filters, DataContext context)
    {
        IQueryable<AccountModel> accounts;

        if (filters.FindOn == FindOn.StableAccounts)
        {
            accounts = from account in OnStable(context)
                       where ids.Contains(account.IdentityId)
                       select account;
        }
        else
        {
            accounts = from account in OnAll(context)
                       where ids.Contains(account.IdentityId)
                       select account;
        }


        // Armar el modelo.
        accounts = BuildModel(accounts, filters, context);

        // Retorno
        return accounts;

    }

    private static readonly string selector = string.Empty;







    /// <summary>
    /// Construir el modelo.
    /// </summary>
    /// <param name="query">Consulta base.</param>
    /// <param name="filters">Filtros.</param>
    private static IQueryable<AccountModel> BuildModel(IQueryable<AccountModel> query, QueryObjectFilter filters, DataContext context)
    {

        byte[] profile = [];

        try
        {
            profile = File.ReadAllBytes("wwwroot/user.png");
        }
        catch { }

        var queryFinal = from account in query
                         select new AccountModel
                         {
                             Id = account.Id,
                             Name = filters.IsAdmin
                                    || account.Visibility == Visibility.Visible
                                    || filters.AccountContext == account.Id
                                    || context.GroupMembers.FirstOrDefault(t => t.Group.Members.Any(t => t.IdentityId == filters.IdentityContext)) != null
                                    
                                    ? account.Name
                                    : "Usuario privado",
                             Identity = new()
                             {
                                 Id = account.Identity.Id,
                                 Unique = account.Identity.Unique,
                                 CreationTime = account.Identity.CreationTime,
                                 EffectiveTime = account.Identity.EffectiveTime,
                                 ExpirationTime = account.Identity.ExpirationTime,
                                 Status = account.Identity.Status
                             },
                             Password = account.Password,
                             Visibility = account.Visibility,
                             Profile = filters.IncludePhoto ? string.Empty : selector,
                             IdentityId = account.Identity.Id,
                             IdentityService = account.IdentityService,
                             AccountType = account.AccountType,
                             IsLINAdmin = account.IsLINAdmin
                         };

        var s = queryFinal.ToQueryString();

        return queryFinal;

    }



}