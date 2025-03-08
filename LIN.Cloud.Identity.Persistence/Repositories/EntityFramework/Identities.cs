namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class Identities(DataContext context)
{


    /// <summary>
    /// Crear nueva identidad.
    /// </summary>
    /// <param name="modelo">Modelo.</param>
    /// <param name="context">Contexto de conexión.</param>
    public async Task<ReadOneResponse<IdentityModel>> Create(IdentityModel modelo)
    {
        // Pre.
        modelo.Id = 0;

        try
        {

            foreach (var e in modelo.Roles)
                e.Identity = modelo;

            // Guardar la identidad.
            await context.Identities.AddAsync(modelo);
            context.SaveChanges();

            return new()
            {
                Response = Responses.Success,
                Model = modelo
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
    /// Obtener una identidad según el Id.
    /// </summary>
    /// <param name="id">Id de la identidad.</param>
    /// <param name="filters">Filtros de búsqueda.</param>
    /// <param name="context">Contexto de base de datos.</param>
    public async Task<ReadOneResponse<IdentityModel>> Read(int id, QueryIdentityFilter filters)
    {

        try
        {

            // Consulta de las cuentas.
            var account = await Builders.Identities.GetIds(id, filters, context).FirstOrDefaultAsync();

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
    /// Obtener una identidad según el Unique.
    /// </summary>
    /// <param name="unique">Unique.</param>
    /// <param name="filters">Filtros de búsqueda.</param>
    /// <param name="context">Contexto de base de datos.</param>
    public async Task<ReadOneResponse<IdentityModel>> Read(string unique, QueryIdentityFilter filters)
    {

        try
        {

            // Consulta de las cuentas.
            var account = await Builders.Identities.GetIds(unique, filters, context).FirstOrDefaultAsync();

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



}