namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class Groups(DataContext context)
{


    /// <summary>
    /// Crear nuevo grupo.
    /// </summary>
    /// <param name="modelo">Modelo.</param>
    /// <param name="context">Contexto de conexión.</param>
    public async Task<ReadOneResponse<GroupModel>> Create(GroupModel modelo)
    {
        // Pre.
        modelo.Id = 0;

        // Transacción.
        using var transaction = context.Database.BeginTransaction();

        try
        {

            // Miembros.
            foreach (var e in modelo.Members)
            {
                e.Group = modelo;
                context.Attach(e.Identity);
            }

            // Fijar la organización.
            modelo.Owner = new()
            {
                Id = modelo.OwnerId ?? 0
            };

            // Si no existe.
            if (modelo.Owner != null)
                context.Attach(modelo.Owner);

            // Guardar la identidad.
            await context.Groups.AddAsync(modelo);


            // Obtener el directorio general.
            var dir = (from org in context.Organizations
                       where org.Id == modelo.OwnerId
                       select new { org.DirectoryId, org.Directory.Identity.Unique }).FirstOrDefault();

            // Si no se encontró el directorio.
            if (dir == null)
            {
                transaction.Rollback();
                return new()
                {
                    Response = Responses.NotRows
                };
            }

            // Nueva identidad.
            modelo.Identity.Unique = $"{modelo.Identity.Unique}@{dir.Unique}";

            // Guardar.
            context.SaveChanges();


            var dirModel = new GroupModel
            {
                Id = dir.DirectoryId
            };

            context.Attach(dirModel);

            context.GroupMembers.Add(new()
            {
                Group = dirModel,
                Identity = modelo.Identity,
                Type = GroupMemberTypes.Group
            });


            context.SaveChanges();

            transaction.Commit();

            return new()
            {
                Response = Responses.Success,
                Model = modelo
            };

        }
        catch (Exception)
        {
            transaction.Rollback();
            return new()
            {
                Response = Responses.Undefined
            };
        }

    }



    /// <summary>
    /// Obtener un grupo según el Id.
    /// </summary>
    /// <param name="id">Id.</param>
    /// <param name="context">Contexto de base de datos.</param>
    public async Task<ReadOneResponse<GroupModel>> Read(int id)
    {

        try
        {
            // Consulta.
            var group = await (from g in context.Groups
                               where g.Id == id
                               select new GroupModel
                               {
                                   Id = g.Id,
                                   Identity = g.Identity,
                                   Name = g.Name,
                                   IdentityId = g.IdentityId,
                                   Description = g.Description
                               }).FirstOrDefaultAsync();

            // Si la cuenta no existe.
            if (group == null)
                return new()
                {
                    Response = Responses.NotRows
                };

            // Success.
            return new()
            {
                Response = Responses.Success,
                Model = group
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
    /// Obtener un grupo según el Id de la identidad.
    /// </summary>
    /// <param name="id">Identidad.</param>
    /// <param name="context">Contexto de base de datos.</param>
    public async Task<ReadOneResponse<GroupModel>> ReadByIdentity(int id)
    {

        try
        {
            // Consulta.
            var group = await (from g in context.Groups
                               where g.IdentityId == id
                               select new GroupModel
                               {
                                   Id = g.Id,
                                   Identity = g.Identity,
                                   Name = g.Name,
                                   IdentityId = g.IdentityId,
                                   Description = g.Description
                               }).FirstOrDefaultAsync();

            // Si la cuenta no existe.
            if (group == null)
                return new()
                {
                    Response = Responses.NotRows
                };

            // Success.
            return new()
            {
                Response = Responses.Success,
                Model = group
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
    /// Obtener los grupos asociados a una organización.
    /// </summary>
    /// <param name="organization">Organización.</param>
    /// <param name="context">Contexto de base de datos.</param>
    public async Task<ReadAllResponse<GroupModel>> ReadAll(int organization)
    {

        try
        {

            // Consulta.
            var groups = await (from g in context.Groups
                                where g.OwnerId == organization
                                select new GroupModel
                                {
                                    Id = g.Id,
                                    Identity = g.Identity,
                                    Name = g.Name,
                                    OwnerId = g.OwnerId
                                }).ToListAsync();

            // Success.
            return new()
            {
                Response = Responses.Success,
                Models = groups ?? []
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
    /// Obtener la organización propietaria de un grupo.
    /// </summary>
    /// <param name="id">Id del grupo.</param>
    /// <param name="context">Contexto de base de datos.</param>
    public async Task<ReadOneResponse<int>> GetOwner(int id)
    {

        try
        {

            // Consulta.
            var ownerId = await (from g in context.Groups
                                 where g.Id == id
                                 select g.OwnerId).FirstOrDefaultAsync();

            // Si la cuenta no existe.
            if (ownerId == null)
                return new()
                {
                    Response = Responses.NotRows
                };

            // Success.
            return new()
            {
                Response = ownerId == null ? Responses.NotRows : Responses.Success,
                Model = ownerId ?? 0
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
    /// Obtener la organización propietaria de un grupo.
    /// </summary>
    /// <param name="id">Id de la identidad.</param>
    /// <param name="context">Contexto de base de datos.</param>
    public async Task<ReadOneResponse<int>> GetOwnerByIdentity(int id)
    {

        try
        {

            // Consulta.
            var ownerId = await (from g in context.Groups
                                 where g.IdentityId == id
                                 select g.OwnerId).FirstOrDefaultAsync();

            // Si la cuenta no existe.
            if (ownerId == null)
                return new()
                {
                    Response = Responses.NotRows
                };

            // Success.
            return new()
            {
                Response = ownerId == null ? Responses.NotRows : Responses.Success,
                Model = ownerId ?? 0
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



}