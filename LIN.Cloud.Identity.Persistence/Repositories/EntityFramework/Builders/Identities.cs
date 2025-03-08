namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework.Builders;


public class Identities
{


    /// <summary>
    /// Buscar en la identidad estables.
    /// </summary>
    /// <param name="context">Contexto de base de datos.</param>
    public static IQueryable<IdentityModel> OnStable(DataContext context)
    {

        // Hora actual.
        var now = DateTime.Now;

        // Consulta.
        var query = from identity in context.Identities
                    where identity.Status != IdentityStatus.Disable
                    && identity.Status == IdentityStatus.Enable
                    && identity.EffectiveTime > now && identity.ExpirationTime < now
                    select identity;

        // Retornar.
        return query;
    }



    /// <summary>
    /// Buscar en todas las identidades.
    /// </summary>
    /// <param name="context">Contexto de base de datos.</param>
    public static IQueryable<IdentityModel> OnAll(DataContext context)
    {

        // Hora actual.
        var now = DateTime.Now;

        // Consulta.
        var query = from identity in context.Identities
                    select identity;

        // Retornar.
        return query;
    }



    /// <summary>
    /// Obtener identidades.
    /// </summary>
    /// <param name="id">Id</param>
    /// <param name="filters">Filtros</param>
    /// <param name="context">Contexto</param>
    public static IQueryable<IdentityModel> GetIds(int id, QueryIdentityFilter filters, DataContext context)
    {

        // Query general
        IQueryable<IdentityModel> ids;

        if (filters.FindOn == FindOn.StableAccounts)
            ids = from identity in OnStable(context)
                  where identity.Id == id
                  select identity;
        else
            ids = from account in OnAll(context)
                  where account.Id == id
                  select account;

        // Armar el modelo
        ids = BuildModel(ids, filters);

        // Retorno
        return ids;

    }



    /// <summary>
    /// Obtener identidades.
    /// </summary>
    /// <param name="unique">Unique</param>
    /// <param name="filters">Filtros</param>
    /// <param name="context">Contexto</param>
    public static IQueryable<IdentityModel> GetIds(string unique, Services.Models.QueryIdentityFilter filters, DataContext context)
    {

        // Query general
        IQueryable<IdentityModel> ids;

        if (filters.FindOn == FindOn.StableAccounts)
            ids = from identity in OnStable(context)
                  where identity.Unique == unique
                  select identity;
        else
            ids = from identity in OnAll(context)
                  where identity.Unique == unique
                  select identity;

        // Armar el modelo
        ids = BuildModel(ids, filters);

        // Retorno
        return ids;

    }



    /// <summary>
    /// Construir el modelo.
    /// </summary>
    /// <param name="query">Consulta base.</param>
    /// <param name="filters">Filtros.</param>
    private static IQueryable<IdentityModel> BuildModel(IQueryable<IdentityModel> query, Services.Models.QueryIdentityFilter filters)
    {

        var final = from id in query
                    select new IdentityModel
                    {
                        Status = id.Status,
                        CreationTime = filters.IncludeDates ? id.CreationTime : default,
                        EffectiveTime = filters.IncludeDates ? id.EffectiveTime : default,
                        ExpirationTime = filters.IncludeDates ? id.ExpirationTime : default,
                        Id = id.Id,
                        Unique = id.Unique
                    };

        return final;

    }



}