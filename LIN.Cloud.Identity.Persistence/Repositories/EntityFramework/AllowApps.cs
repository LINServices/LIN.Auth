namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class AllowApps(DataContext context, IIdentityService identityService)
{

    /// <summary>
    /// Crear acceso a app.
    /// </summary>
    /// <param name="modelo">Modelo.</param>
    public async Task<ReadOneResponse<AllowApp>> Create(AllowApp modelo)
    {

        // Transacción.
        using var transaction = context.Database.BeginTransaction();

        try
        {
            // Attach.
            context.Attach(modelo.Application);
            context.Attach(modelo.Identity);

            // Guardar la cuenta.
            await context.AllowApps.AddAsync(modelo);
            context.SaveChanges();

            // Confirmar los cambios.
            transaction.Commit();

            return new()
            {
                Response = Responses.Success,
                Model = modelo
            };

        }
        catch (Exception)
        {
            transaction.Rollback();
            return new()
            {
                Response = Responses.ResourceExist
            };
        }

    }


    /// <summary>
    /// btener las apps a las que una identidad tiene acceso o no.
    /// </summary>
    /// <param name="id">Id de la identidad.</param>
    public async Task<ReadAllResponse<AllowApp>> ReadAll(int id)
    {

        // Ejecución
        try
        {

            var identities = await identityService.GetIdentities(id);

            var query = await (from allow in context.AllowApps
                               where identities.Contains(allow.IdentityId)
                               select new AllowApp
                               {
                                   ApplicationRestrictionId = allow.ApplicationRestrictionId,
                                   IdentityId = allow.IdentityId,
                                   IsAllow = allow.IsAllow,
                                   Application = new()
                                   {
                                       Id = allow.Application.Id
                                   }
                               }).ToListAsync();

            return new(Responses.Success, query);

        }
        catch (Exception)
        {
        }
        return new();
    }

}