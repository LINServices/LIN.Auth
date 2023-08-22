﻿namespace LIN.Auth.Data.Organizations;


public class Members
{


    #region Abstracciones


    /// <summary>
    /// Obtiene la lista de integrantes de una organización
    /// </summary>
    /// <param name="id">ID de la organización</param>
    public async static Task<ReadAllResponse<AccountModel>> ReadAll(int id)
    {
        var (context, contextKey) = Conexión.GetOneConnection();

        var res = await ReadAll(id, context);
        context.CloseActions(contextKey);
        return res;
    }




    // <summary>
    /// Crea una cuenta en una organización
    /// </summary>
    /// <param name="data">Modelo</param>
    /// <param name="orgID">ID de la organización</param>
    /// <param name="rol">Rol dentro de la organización</param>
    public async static Task<ReadOneResponse<AccountModel>> Create(AccountModel data, int orgID, OrgRoles rol)
    {

        var (context, contextKey) = Conexión.GetOneConnection();
        var res = await Create(data, orgID, rol, context);
        context.CloseActions(contextKey);
        return res;
    }


    #endregion



    /// <summary>
    /// Obtiene la lista de integrantes de una organización.
    /// </summary>
    /// <param name="id">ID de la organización</param>
    /// <param name="context">Contexto de conexión</param>
    public async static Task<ReadAllResponse<AccountModel>> ReadAll(int id, Conexión context)
    {

        // Ejecución
        try
        {

            // Organización
            var org = from O in context.DataBase.OrganizationAccess
                      where O.Organization.ID == id
                      select new AccountModel
                      {
                          Creación = O.Member.Creación,
                          ID = O.Member.ID,
                          Nombre = O.Member.Nombre,
                          Genero = O.Member.Genero,
                          Usuario = O.Member.Usuario,
                          OrganizationAccess = new()
                          {
                              Rol = O.Member.OrganizationAccess == null ? OrgRoles.Undefine : O.Member.OrganizationAccess.Rol
                          }
                      };

            var orgList = await org.ToListAsync();

            // Email no existe
            if (org == null)
                return new(Responses.NotRows);

            return new(Responses.Success, orgList);
        }
        catch
        {
        }

        return new();
    }




    /// <summary>
    /// Crea una cuenta en una organización
    /// </summary>
    /// <param name="data">Modelo</param>
    /// <param name="orgID">ID de la organización</param>
    /// <param name="rol">Rol dentro de la organización</param>
    /// <param name="context">Contexto de conexión</param>
    public async static Task<ReadOneResponse<AccountModel>> Create(AccountModel data, int orgID, OrgRoles rol, Conexión context)
    {

        data.ID = 0;

        // Ejecución
        using (var transaction = context.DataBase.Database.BeginTransaction())
        {
            try
            {

                // Obtiene la organización
                var org = await (from U in context.DataBase.Organizations
                                 where U.ID == orgID
                                 select U).FirstOrDefaultAsync();

                // No existe la organización
                if (org == null)
                {
                    transaction.Rollback();
                    return new(Responses.NotRows);
                }

                // Modelo de acceso
                data.OrganizationAccess = new()
                {
                    Member = data,
                    Rol = rol,
                    Organization = org
                };

                var res = await context.DataBase.Accounts.AddAsync(data);
                context.DataBase.SaveChanges();

                transaction.Commit();

                return new(Responses.Success, data);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                if ((ex.InnerException?.Message.Contains("Violation of UNIQUE KEY constraint") ?? false) || (ex.InnerException?.Message.Contains("duplicate key") ?? false))
                    return new(Responses.ExistAccount);

            }
        }

        return new();
    }



}