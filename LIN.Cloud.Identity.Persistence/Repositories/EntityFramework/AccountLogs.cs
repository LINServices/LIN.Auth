namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class AccountLogs(DataContext context)
{

    /// <summary>
    /// Crear un log de inicio de sesión.
    /// </summary>
    /// <param name="log">Modelo.</param>
    public async Task<CreateResponse> Create(AccountLog log)
    {

        // Formato del modelo.
        log.Id = 0;

        try
        {

            // Organizar el modelo.
            log.Account = new() { Id = log.AccountId };

            // Ya existe.
            log.Account = context.AttachOrUpdate(log.Account);

            // Si hay una app.
            if (log.Application is not null)
                log.Application = context.AttachOrUpdate(log.Application);
            else
                log.ApplicationId = 0;

            // Guardar la cuenta.
            await context.AccountLogs.AddAsync(log);
            context.SaveChanges();

            return new(Responses.Success, log.Id);
        }
        catch (Exception)
        {
            return new(Responses.Undefined);
        }
    }


    /// <summary>
    /// Obtener los logs de inicio de sesión.
    /// </summary>
    /// <param name="accountId">Id de la cuenta.</param>
    /// <param name="start">Fecha de inicio.</param>
    /// <param name="end">Fecha de fin.</param>
    public async Task<ReadAllResponse<AccountLog>> ReadAll(int accountId, DateTime? start, DateTime? end)
    {
        try
        {
            var logs = await (from log in context.AccountLogs
                              where log.AccountId == accountId
                              && log.Time > start && log.Time < end
                              select new AccountLog
                              {
                                  Id = log.Id,
                                  Time = log.Time,
                                  AccountId = log.AccountId,
                                  Application = new()
                                  {
                                      Name = log.Application!.Name,
                                      IdentityId = log.Application.IdentityId,
                                  },
                                  ApplicationId = log.ApplicationId,
                                  AuthenticationMethod = log.AuthenticationMethod
                              }).ToListAsync();

            return new(Responses.Success, logs);
        }
        catch (Exception)
        {
            return new(Responses.Undefined);
        }
    }

}