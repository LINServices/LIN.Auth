namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class OtpService(DataContext context)
{

    public async Task<CreateResponse> Create(OtpDatabaseModel model)
    {

        try
        {
            model.Account = context.AttachOrUpdate(model.Account);

            // Guardar OTP.
            await context.OTPs.AddAsync(model);
            context.SaveChanges();

            return new(Responses.Success);

        }
        catch (Exception)
        {
        }
        return new(Responses.Undefined);

    }


    public async Task<ResponseBase> ReadAndUpdate(int accountId, string code)
    {

        try
        {


            var update = await (from A in context.OTPs
                                where A.AccountId == accountId
                                && A.Code == code
                                && A.ExpireTime > DateTime.Now
                                && A.IsUsed == false
                                select A).ExecuteUpdateAsync(t => t.SetProperty(t => t.IsUsed, true));


            if (update <= 0)
                return new(Responses.NotRows);


            return new(Responses.Success);

        }
        catch (Exception)
        {
        }
        return new(Responses.Undefined);

    }



    public async Task<CreateResponse> Create(MailOtpDatabaseModel model)
    {

        try
        {

            // A
            model.OtpDatabaseModel.Account = context.AttachOrUpdate(model.OtpDatabaseModel.Account);

            model.MailModel = new()
            {
                Id = model.MailModel.Id
            };
            model.MailModel = context.AttachOrUpdate(model.MailModel);

            // Guardar OTP.
            await context.MailOtp.AddAsync(model);
            context.SaveChanges();

            return new(Responses.Success);

        }
        catch (Exception)
        {
        }
        return new(Responses.Undefined);

    }



}