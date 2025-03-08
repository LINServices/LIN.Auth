using LIN.Types.Cloud.Identity.Abstracts;

namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class DirectoryMembers(DataContext context)
{

    /// <summary>
    /// Obtener los directorios (Grupos de organización) donde una identidad pertenece.
    /// </summary>
    /// <param name="id">Identidad</param>
    /// <param name="organization">Organización de contexto.</param>
    public async Task<ReadAllResponse<GroupMember>> Read(int id, int organization)
    {
        try
        {
            var members = await (from gm in context.GroupMembers
                                 where gm.IdentityId == id
                                 join o in context.Organizations
                                 on gm.GroupId equals o.DirectoryId
                                 where o.Id == organization
                                 select new GroupMember
                                 {
                                     Type = gm.Type,
                                     Group = gm.Group,
                                     GroupId = gm.GroupId,
                                     IdentityId = gm.IdentityId,
                                 }).ToListAsync();


            // Si la cuenta no existe.
            if (members == null)
                return new(Responses.NotRows);

            // Success.
            return new(Responses.Success, members);
        }
        catch (Exception)
        {
            return new(Responses.Undefined);
        }

    }


    /// <summary>
    /// Valida si una identidad es miembro de una organización.
    /// </summary>
    /// <param name="id">Identidad</param>
    /// <param name="organization">Id de la organización</param>
    public async Task<ReadOneResponse<GroupMemberTypes>> IamIn(int id, int organization)
    {

        try
        {

            // Consulta.
            var query = await (from org in context.Organizations
                               where org.Id == organization
                               join gm in context.GroupMembers
                               on org.DirectoryId equals gm.GroupId
                               where gm.IdentityId == id
                               select new
                               {
                                   gm.Type
                               }).FirstOrDefaultAsync();


            // Si la cuenta no existe.
            if (query == null)
            {

                var x = await (from A in context.Organizations
                               where A.Directory.IdentityId == id
                               && A.Id == organization
                               select A).AnyAsync();


                if (!x)
                    return new()
                    {
                        Response = Responses.NotRows
                    };

            }


            // Success.
            return new()
            {
                Response = Responses.Success,
                Model = query?.Type ?? GroupMemberTypes.Group
            };

        }
        catch (Exception)
        {
            return new()
            {
                Response = Responses.NotRows
            };
        }

    }



    /// <summary>
    /// Valida si una lista de identidades son miembro de una organización.
    /// </summary>
    /// <param name="ids">Identidades</param>
    /// <param name="organization">Id de la organización</param>
    /// <param name="context">Contexto</param>
    public async Task<(IEnumerable<int> success, List<int> failure)> IamIn(IEnumerable<int> ids, int organization)
    {

        try
        {

            // Consulta.
            var query = await (from org in context.Organizations
                               where org.Id == organization
                               join gm in context.GroupMembers
                               on org.DirectoryId equals gm.GroupId
                               where ids.Contains(gm.IdentityId)
                               select gm.IdentityId).ToListAsync();

            // Lista.
            List<int> success = [.. query];
            List<int> failure = [.. ids.Except(success)];

            return (success, failure);
        }
        catch (Exception)
        {
        }
        return ([], []);
    }



    /// <summary>
    /// Valida si una identidad es miembro de una organización.
    /// </summary>
    /// <param name="id">Identidad</param>
    /// <param name="directory">Id del directorio</param>
    /// <param name="context">Contexto</param>
    public async Task<ReadOneResponse<GroupMemberTypes>> IamInByDir(int id, int directory)
    {

        try
        {

            // Consulta.
            var query = await (from org in context.Organizations
                               where org.DirectoryId == directory
                               join gm in context.GroupMembers
                               on org.DirectoryId equals gm.GroupId
                               where gm.IdentityId == id
                               select new
                               {
                                   gm.Type
                               }).FirstOrDefaultAsync();


            // Si la cuenta no existe.
            if (query == null)
                return new()
                {
                    Response = Responses.NotRows
                };

            // Success.
            return new()
            {
                Response = Responses.Success,
                Model = query.Type
            };

        }
        catch (Exception)
        {
            return new()
            {
                Response = Responses.NotRows
            };
        }

    }



    /// <summary>
    ///
    /// </summary>
    /// <param name="id">Directorio</param>
    /// <param name="context">Contexto</param>
    public async Task<ReadAllResponse<GroupMember>> ReadMembers(int id)
    {

        try
        {

            var members = await (from org in context.Organizations
                                 where org.DirectoryId == id
                                 join gm in context.GroupMembers
                                 on org.DirectoryId equals gm.GroupId
                                 select new GroupMember
                                 {
                                     GroupId = gm.GroupId,
                                     Identity = gm.Identity,
                                     Type = gm.Type,
                                     IdentityId = gm.IdentityId
                                 }).ToListAsync();

            // Si la cuenta no existe.
            if (members == null)
                return new()
                {
                    Response = Responses.NotRows
                };

            // Success.
            return new()
            {
                Response = Responses.Success,
                Models = members
            };

        }
        catch (Exception)
        {
            return new()
            {
                Response = Responses.ExistAccount
            };
        }

    }



    /// <summary>
    ///
    /// </summary>
    /// <param name="id">Directorio</param>
    /// <param name="context">Contexto</param>
    public async Task<ReadAllResponse<SessionModel<GroupMember>>> ReadMembersByOrg(int id)
    {

        try
        {

            var members = await (from org in context.Organizations
                                 where org.Id == id
                                 join gm in context.GroupMembers
                                 on org.DirectoryId equals gm.GroupId
                                 join a in context.Accounts
                                 on gm.IdentityId equals a.IdentityId
                                 select new SessionModel<GroupMember>
                                 {
                                     Account = new()
                                     {
                                         Id = a.Id,
                                         Name = a.Name,
                                         Visibility = a.Visibility,
                                         IdentityService = a.IdentityService,
                                         Identity = new()
                                         {
                                             Id = a.Identity.Id,
                                             Unique = a.Identity.Unique
                                         }
                                     },
                                     Profile = gm
                                 }).ToListAsync();


            // Si la cuenta no existe.
            if (members == null)
                return new()
                {
                    Response = Responses.NotRows
                };

            // Success.
            return new()
            {
                Response = Responses.Success,
                Models = members
            };

        }
        catch (Exception)
        {
            return new()
            {
                Response = Responses.ExistAccount
            };
        }

    }


    /// <summary>
    /// Expulsar identidades de la organización.
    /// </summary>
    /// <param name="ids">Lista de identidades.</param>
    /// <param name="organization">Id de la organización.</param>
    /// <returns>Respuesta del proceso.</returns>
    public async Task<ResponseBase> Expulse(IEnumerable<int> ids, int organization)
    {

        try
        {

            // Desactivar identidades (Solo creadas dentro de la propia organización).
            var baseQuery = from member in context.GroupMembers
                             where ids.Contains(member.IdentityId)
                             where member.Group.OwnerId == organization
                             select member;

            // Desactivar identidades (Solo creadas dentro de la propia organización).
            await baseQuery.Where(m => m.Type != GroupMemberTypes.Guest).Select(m => m.Identity).ExecuteUpdateAsync(t => t.SetProperty(t => t.Status, IdentityStatus.Disable));

            // Eliminar accesos (Tanto propios de la organización como los externos).
            await baseQuery.ExecuteDeleteAsync();

            // Eliminar roles asociados.
            await (from rol in context.IdentityRoles
                   where ids.Contains(rol.IdentityId)
                   && rol.OrganizationId == organization
                   select rol).ExecuteDeleteAsync();

            // Success.
            return new()
            {
                Response = Responses.Success
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

}