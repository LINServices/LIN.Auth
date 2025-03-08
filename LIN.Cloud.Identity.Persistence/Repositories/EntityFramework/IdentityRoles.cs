namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class IdentityRoles(DataContext context)
{

    /// <summary>
    /// Crear nuevo rol en identidad.
    /// </summary>
    /// <param name="modelo">Modelo.</param>
    /// <param name="context">Contexto de conexión.</param>
    public async Task<ResponseBase> Create(IdentityRolesModel modelo)
    {

        try
        {

            // Attach.
            context.Attach(modelo.Identity);
            context.Attach(modelo.Organization);

            // Guardar la identidad.
            await context.IdentityRoles.AddAsync(modelo);
            context.SaveChanges();

            return new()
            {
                Response = Responses.Success
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
    /// Obtener los roles asociados a una identidad en una organización determinada.
    /// </summary>
    /// <param name="identity">Identidad.</param>
    /// <param name="organization">Organización.</param>
    /// <param name="context">Contexto de base de datos.</param>
    public async Task<ReadAllResponse<IdentityRolesModel>> ReadAll(int identity, int organization)
    {

        try
        {

            List<IdentityRolesModel> Roles = [];

            await RolesOn(identity, organization, [], Roles);

            return new()
            {
                Models = Roles,
                Response = Responses.Success
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
    /// Eliminar el rol de una identidad en una organización.
    /// </summary>
    /// <param name="identity">Identidad.</param>
    /// <param name="rol">Rol.</param>
    /// <param name="organization">Organización.</param>
    /// <param name="context">Contexto de base de datos.</param>
    public async Task<ResponseBase> Remove(int identity, Roles rol, int organization)
    {

        try
        {

            // Ejecutar eliminación.
            var count = await (from ir in context.IdentityRoles
                               where ir.IdentityId == identity
                               && ir.Rol == rol
                               && ir.OrganizationId == organization
                               select ir).ExecuteDeleteAsync();

            return new()
            {
                Response = Responses.Success
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














    private async Task RolesOn(int identity, int organization, List<int> ids, List<IdentityRolesModel> roles)
    {

        var query = from id in context.Identities
                    where id.Id == identity
                    select new
                    {
                        Id = new { id.Unique, id.ExpirationTime, id.CreationTime },
                        In = (from member in context.GroupMembers
                              where !ids.Contains(member.Group.IdentityId)
                              && member.IdentityId == identity
                              select member.Group.IdentityId).ToList(),

                        Roles = (from IR in context.IdentityRoles
                                 where IR.IdentityId == identity
                                 where IR.OrganizationId == organization
                                 select IR.Rol).ToList()
                    };


        // Si hay elementos.
        if (query.Any())
        {

            // Ejecuta la consulta.
            var local = query.ToList();

            // Obtiene los roles.
            var localRoles = local.SelectMany(t => t.Roles);

            // Obtiene las bases.
            var bases = local.SelectMany(t => t.In);

            // Agregar a los objetos.
            roles.AddRange(localRoles.Select(t => new IdentityRolesModel
            {
                Identity = new()
                {
                    Id = identity,
                    Unique = local[0].Id.Unique,
                    CreationTime = local[0].Id.CreationTime,
                    ExpirationTime = local[0].Id.ExpirationTime,
                },
                Rol = t
            }));

            ids.AddRange(bases);

            // Recorrer.
            foreach (var @base in bases)
                await RolesOn(@base, organization, ids, roles);

        }

    }



}