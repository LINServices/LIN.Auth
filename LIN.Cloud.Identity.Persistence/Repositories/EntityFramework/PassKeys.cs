namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class PassKeys(DataContext context)
{

    /// <summary>
    /// Contar los logs de autenticación del dia.
    /// </summary>
    /// <param name="id">Id de la cuenta.</param>
    public async Task<ReadOneResponse<int>> Count(int id)
    {
        try
        {
            // Tiempo.
            var time = DateTime.Now;

            // Contar.
            int count = await (from a in context.AccountLogs
                               where a.AccountId == id
                               && a.AuthenticationMethod == AuthenticationMethods.Authenticator
                               where a.Time.Year == time.Year
                               && a.Time.Month == time.Month
                               && a.Time.Day == time.Day
                               select a).CountAsync();

            // Success.
            return new(Responses.Success, count);
        }
        catch (Exception)
        {
            return new(Responses.NotRows);
        }

    }

}