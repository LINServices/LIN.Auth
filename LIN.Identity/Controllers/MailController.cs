namespace LIN.Identity.Controllers;


[Route("mails")]
public class MailController : ControllerBase
{


    /// <summary>
    /// Obtiene los mails asociados a una cuenta
    /// </summary>
    /// <param name="token">Token de acceso</param>
    [HttpGet("all")]
    public async Task<HttpReadAllResponse<EmailModel>> GetMails([FromHeader] string token)
    {

        // Informaci�n del token.
        var (isValid, _, id, _, _) = Jwt.Validate(token);

        // Validaci�n del token
        if (!isValid)
            return new(Responses.Unauthorized)
            {
                Message = "Token invalido."
            };

        return await Data.Mails.ReadAll(id);

    }



}