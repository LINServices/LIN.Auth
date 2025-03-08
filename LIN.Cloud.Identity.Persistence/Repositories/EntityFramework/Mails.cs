namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class Mails(DataContext context)
{

    /// <summary>
    /// Crear modelo de resultado de mail.
    /// </summary>
    /// <param name="modelo">Modelo.</param>
    public async Task<ReadOneResponse<MailModel>> Create(MailModel modelo)
    {
        try
        {

            modelo.Id = 0;
            modelo.Account = new()
            {
                Id = modelo.AccountId,
            };

            // Attach.
            context.Attach(modelo.Account);

            // Guardar la cuenta.
            await context.Mails.AddAsync(modelo);
            context.SaveChanges();

            return new()
            {
                Response = Responses.Success,
                Model = modelo
            };

        }
        catch (Exception)
        {
            return new(Responses.ResourceExist);
        }
    }


    /// <summary>
    /// Obtener el correo principal.
    /// </summary>
    /// <param name="unique">Usuario unico.</param>
    public async Task<ReadOneResponse<MailModel>> ReadPrincipal(string unique)
    {
        try
        {

            var x = await (from mail in context.Mails
                           join account in context.Accounts
                           on mail.Account.IdentityId equals account.IdentityId
                           where account.Identity.Unique == unique
                           where mail.IsPrincipal
                           && mail.IsVerified
                           select mail).FirstOrDefaultAsync();

            if (x is null)
                return new()
                {
                    Response = Responses.NotRows
                };


            return new()
            {
                Response = Responses.Success,
                Model = x
            };

        }
        catch (Exception)
        {
            return new()
            {
                Response = Responses.Undefined
            };
        }

    }


    /// <summary>
    /// Validar otp para un correo.
    /// </summary>
    /// <param name="email">Correo.</param>
    /// <param name="code"></param>
    public async Task<ResponseBase> ValidateOtpForMail(string email, string code)
    {
        try
        {

            // Obtener modelo.
            var otpModel = from mail in context.Mails
                            where mail.Mail == email
                            join otp in context.MailOtp
                            on mail.Id equals otp.MailId
                            where otp.OtpDatabaseModel.Code == code
                            && otp.OtpDatabaseModel.IsUsed == false
                            && otp.OtpDatabaseModel.ExpireTime > DateTime.Now
                            select otp;

            // Actualizar.
            await otpModel.Select(t => t.MailModel).ExecuteUpdateAsync(t => t.SetProperty(t => t.IsVerified, true));
            int countUpdate = await otpModel.Select(t => t.OtpDatabaseModel).ExecuteUpdateAsync(t => t.SetProperty(t => t.IsUsed, true));

            // Si no se actualizaron.
            if (countUpdate <= 0)
                return new(Responses.NotRows);

            return new(Responses.Success);
        }
        catch (Exception)
        {
            return new(Responses.Undefined);
        }

    }

}