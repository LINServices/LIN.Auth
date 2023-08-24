﻿namespace LIN.Auth.Hubs;


public class PassKeyHub : Hub
{


    /// <summary>
    /// Lista de intentos Passkey
    /// </summary>
    public readonly static Dictionary<string, List<PassKeyModel>> Attempts = new();



    /// <summary>
    /// Nuevo intento passkey
    /// </summary>
    /// <param name="attempt">Intento passkey</param>
    public async Task JoinIntent(PassKeyModel attempt)
    {

        // Aplicación
        var application = await Data.Applications.Read(attempt.ApplicationKey);

        // Si la app no existe o no esta activa
        if (application.Response != Responses.Success)
            return;

        // Preparar el modelo
        attempt.Application.Name = application.Model.Name;
        attempt.Application.Badge = application.Model.Badge;
        attempt.Application.Key = application.Model.Key;
        attempt.Application.ID = application.Model.ID;

        // Vencimiento
        var expiración = DateTime.Now.AddMinutes(2);

        // Caducidad el modelo
        attempt.HubKey = Context.ConnectionId;
        attempt.Status = PassKeyStatus.Undefined;
        attempt.Hora = DateTime.Now;
        attempt.Expiración = expiración;

        // Agrega el modelo
        if (!Attempts.ContainsKey(attempt.User.ToLower()))
            Attempts.Add(attempt.User.ToLower(), new() { attempt });
        else
            Attempts[attempt.User.ToLower()].Add(attempt);

        // Yo
        await Groups.AddToGroupAsync(Context.ConnectionId, $"dbo.{Context.ConnectionId}");

        await SendRequest(attempt);

    }
























    public override Task OnDisconnectedAsync(Exception? exception)
    {

        var e = Attempts.Values.Where(T => T.Where(T => T.HubKey == Context.ConnectionId).Any()).FirstOrDefault() ?? new();


        _ = e.Where(T =>
          {
              if (T.HubKey == Context.ConnectionId && T.Status == PassKeyStatus.Undefined)
                  T.Status = PassKeyStatus.Failed;

              return false;
          });


        return base.OnDisconnectedAsync(exception);
    }


    /// <summary>
    /// Un dispositivo envia el PassKey intent
    /// </summary>
    public async Task JoinAdmin(string usuario)
    {

        // Grupo de la cuenta
        await Groups.AddToGroupAsync(Context.ConnectionId, usuario.ToLower());

    }







    //=========== Dispositivos ===========//


    /// <summary>
    /// Envía la solicitud a los admins
    /// </summary>
    public async Task SendRequest(PassKeyModel modelo)
    {

        var pass = new PassKeyModel()
        {
            Expiración = modelo.Expiración,
            Hora = modelo.Hora,
            Status = modelo.Status,
            User = modelo.User,
            HubKey = modelo.HubKey,
            Application = new()
            {
                Name = modelo.Application.Name,
                Badge = modelo.Application.Badge
            }
        };

        await Clients.Group(modelo.User.ToLower()).SendAsync("newintent", pass);
    }




    /// <summary>
    /// 
    /// </summary>
    public async void ReceiveRequest(PassKeyModel modelo)
    {

        try
        {
            // Obtiene la cuenta
            var cuenta = Attempts[modelo.User.ToLower()];

            // Obtiene el dispositivo
            var intent = cuenta.Where(T => T.HubKey == modelo.HubKey).ToList().FirstOrDefault();

            if (intent == null)
                return;

            intent.Status = modelo.Status;

            if (DateTime.Now > modelo.Expiración)
            {
                intent.Status = PassKeyStatus.Expired;
                modelo.Status = PassKeyStatus.Expired;
                modelo.Token = string.Empty;
                intent.Token = string.Empty;
            }


            var (isValid, _, userID, orgID) = Jwt.Validate(modelo.Token);
            if (isValid && modelo.Status == PassKeyStatus.Success)
            {

                // Validacion de la app
                var application = await Data.Applications.AppOnOrg(intent.Application.Key, orgID);



                var badPass = new PassKeyModel()
                {
                    Status = PassKeyStatus.BlockedByOrg,
                    User = modelo.User,
                };


                // Si la app no existe o no esta activa
                if (application.Response != Responses.Success)
                {
                    await Clients.Groups($"dbo.{modelo.HubKey}").SendAsync("recieveresponse", badPass);
                    return;
                }

            }


            var pass = new PassKeyModel()
            {
                Expiración = modelo.Expiración,
                Status = modelo.Status,
                User = modelo.User,
                Token = modelo.Token
            };



            await Clients.Groups($"dbo.{modelo.HubKey}").SendAsync("recieveresponse", pass);

        }
        catch
        {
        }



    }





}
