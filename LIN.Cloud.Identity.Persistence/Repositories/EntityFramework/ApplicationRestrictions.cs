namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class ApplicationRestrictions(DataContext context)
{

    /// <summary>
    /// Crear nueva restricción de aplicación.
    /// </summary>
    /// <param name="modelo">Modelo.</param>
    public async Task<CreateResponse> Create(ApplicationRestrictionModel modelo)
    {
        // Pre.
        modelo.Id = 0;

        try
        {
            // Modelo ya existe.
            modelo.Application = context.AttachOrUpdate(modelo.Application);

            // Guardar la identidad.
            await context.ApplicationRestrictions.AddAsync(modelo);
            context.SaveChanges();
            return new(Responses.Success, modelo.Id);
        }
        catch (Exception)
        {
            return new(Responses.Undefined);
        }
    }


    /// <summary>
    /// Obtener las restricciones de aplicación.
    /// </summary>
    /// <param name="id">Id de la aplicación.</param>
    public async Task<ReadOneResponse<ApplicationRestrictionModel>> Read(string id)
    {
        try
        {

            var restriction = await (from ar in context.ApplicationRestrictions
                                     where ar.Application.Key == Guid.Parse(id)
                                     select ar).FirstOrDefaultAsync();

            // Success.
            return new(Responses.Success, restriction!);

        }
        catch (Exception)
        {
            return new(Responses.Undefined);
        }
    }


    /// <summary>
    /// Obtener las restricciones de tiempo.
    /// </summary>
    /// <param name="id">Id de la aplicación.</param>
    public async Task<ReadAllResponse<ApplicationRestrictionTime>> ReadTimes(int id)
    {
        try
        {

            var restrictions = await (from tr in context.TimeRestriction
                                      where tr.ApplicationRestrictionModel.ApplicationId == id
                                      select tr).ToListAsync();

            // Success.
            return new(Responses.Success, restrictions);

        }
        catch (Exception)
        {
            return new(Responses.Undefined);
        }
    }

}