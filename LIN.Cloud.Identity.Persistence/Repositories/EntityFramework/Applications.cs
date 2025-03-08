namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class Applications(DataContext context)
{

    /// <summary>
    /// Crear una nueva aplicación.
    /// </summary>
    /// <param name="modelo">Modelo.</param>
    public async Task<CreateResponse> Create(ApplicationModel modelo)
    {
        // Pre.
        modelo.Id = 0;

        try
        {
            // Modelo ya existe.
            modelo.Owner = context.AttachOrUpdate(modelo.Owner);

            // Guardar la identidad.
            await context.Applications.AddAsync(modelo);
            context.SaveChanges();

            return new(Responses.Success, modelo.Id);
        }
        catch (Exception)
        {
            return new(Responses.Undefined);
        }
    }


    /// <summary>
    /// Obtener una aplicación.
    /// </summary>
    /// <param name="key">Key de la app.</param>
    public async Task<ReadOneResponse<ApplicationModel>> Read(string key)
    {
        try
        {

            // Obtener el modelo.
            var application = await (from ar in context.Applications
                                     where ar.Key == Guid.Parse(key)
                                     select ar).FirstOrDefaultAsync();

            // Success.
            return new(application is null ? Responses.NotRows : Responses.Success, application!);

        }
        catch (Exception)
        {
            return new(Responses.Undefined);
        }
    }


    /// <summary>
    /// Validar si existe una app.
    /// </summary>
    /// <param name="key">Key de la app.</param>
    public async Task<ReadOneResponse<bool>> ExistApp(string key)
    {
        try
        {

            var exist = await (from ar in context.Applications
                               where ar.Key == Guid.Parse(key)
                               select ar).AnyAsync();

            // Success.
            return new(Responses.Success, exist);

        }
        catch (Exception)
        {
            return new(Responses.Undefined);
        }
    }

}