﻿namespace LIN.Identity.Data.Organizations;


public class Organizations
{


    #region Abstracciones


    /// <summary>
    /// Crea una organización
    /// </summary>
    /// <param name="data">Modelo</param>
    public static async Task<ReadOneResponse<OrganizationModel>> Create(OrganizationModel data)
    {
        var (context, contextKey) = Conexión.GetOneConnection();
        var response = await Create(data, context);
        context.CloseActions(contextKey);
        return response;
    }



    /// <summary>
    /// Obtiene una organización
    /// </summary>
    /// <param name="id">ID de la organización</param>
    public static async Task<ReadOneResponse<OrganizationModel>> Read(int id)
    {
        var (context, contextKey) = Conexión.GetOneConnection();

        var res = await Read(id, context);
        context.CloseActions(contextKey);
        return res;
    }





    #endregion



    /// <summary>
    /// Crear una organización
    /// </summary>
    /// <param name="data">Modelo</param>
    /// <param name="context">Contexto de conexión</param>
    public static async Task<ReadOneResponse<OrganizationModel>> Create(OrganizationModel data, Conexión context)
    {

        // Modelo.
        data.ID = 0;

        // Ejecución
        using (var transaction = context.DataBase.Database.BeginTransaction())
        {
            try
            {

                // Lista de cuentas.
                List<AccountModel> accounts = [];

                // Enumerar las cuentas actuales.
                foreach (var account in data.Members.Select(t => t.Member))
                {
                    AccountModel accountModel = new()
                    {
                        Birthday = account.Birthday,
                        Contraseña = account.Contraseña,
                        Creación = account.Creación,
                        Estado = account.Estado,
                        ID = 0,
                        Identity = account.Identity,
                        Insignia = account.Insignia,
                        Nombre = account.Nombre,
                        Visibilidad = account.Visibilidad,
                        Rol = account.Rol,
                        Perfil = account.Perfil,
                        OrganizationAccess = null,
                        DirectoryMembers = [],
                        IdentityId = 0,
                    };

                    accounts.Add(accountModel);
                    context.DataBase.Accounts.Add(accountModel);
                }

                // Guardar cambios.
                context.DataBase.SaveChanges();

                // Modelo la organización.
                OrganizationModel org = new()
                {
                    ID = 0,
                    Domain = data.Domain,
                    Name = data.Name,
                    IsPublic = data.IsPublic,
                    Members = [],
                    Directory = new()
                    {
                        Creación = DateTime.Now,
                        ID = 0,
                        Identity = new()
                        {
                            Unique = data.Directory.Identity.Unique,
                            Type = IdentityTypes.Directory
                        },
                        Nombre = data.Directory.Nombre,
                        IdentityId = 0,
                        Policies =
                        [
                            new PolicyModel
                            {
                                 Creation = DateTime.Now,
                                 Id =0,
                                 Type = PolicyTypes.PasswordLength,
                                 ValueJson = """ {"length": 8} """
                            }
                        ]
                    }
                };


                // Agregar miembros.
                foreach (var account in accounts)
                {
                    // Miembros de la organización.
                    org.Members.Add(new()
                    {
                        Member = account,
                        Organization = org,
                        Rol = OrgRoles.SuperManager
                    });

                    // Miembros del directorio.
                    org.Directory.Members.Add(new()
                    {
                        Account = account,
                        Directory = org.Directory,
                        Rol = DirectoryRoles.Administrator
                    });
                }


                // Agregar el modelo.
                await context.DataBase.Organizations.AddAsync(org);

                // Guardar en la BD.
                context.DataBase.SaveChanges();

                // Commit cambios.
                transaction.Commit();

                return new(Responses.Success, data);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                if ((ex.InnerException?.Message.Contains("Violation of UNIQUE KEY constraint") ?? false) || (ex.InnerException?.Message.Contains("duplicate key") ?? false))
                    return new(Responses.Undefined);

            }
        }
        return new();
    }




    /// <summary>
    /// Obtiene una organización.
    /// </summary>
    /// <param name="id">ID de la organización</param>
    /// <param name="context">Contexto de conexión</param>
    public static async Task<ReadOneResponse<OrganizationModel>> Read(int id, Conexión context)
    {

        // Ejecución
        try
        {

            // Query
            var org = await (from E in context.DataBase.Organizations
                             where E.ID == id

                             select new OrganizationModel
                             {
                                 Directory = null!,
                                 DirectoryId = id,
                                 Domain = E.Domain,
                                 ID = E.ID,
                                 IsPublic = E.IsPublic,
                                 Members = null!,
                                 Name = E.Name,
                             }).FirstOrDefaultAsync();

            // Email no existe
            if (org == null)
                return new(Responses.NotRows);

            return new(Responses.Success, org);
        }
        catch
        {
        }

        return new();
    }




}