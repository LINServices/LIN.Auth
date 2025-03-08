using IdentityService = LIN.Types.Cloud.Identity.Enumerations.IdentityService;

namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class Organizations(DataContext context, IamRoles iam)
{


    /// <summary>
    /// Crear nueva organización. [Transacción]
    /// </summary>
    /// <param name="modelo">Modelo.</param>
    /// <param name="context">Contexto de conexión.</param>
    public async Task<CreateResponse> Create(OrganizationModel modelo)
    {

        using var transaction = context.Database.BeginTransaction();

        try
        {

            // Metadata.
            modelo.Directory.Name = "Directorio General";
            modelo.Directory.Description = "Directorio General de la organización";
            modelo.Directory.Owner = null;
            modelo.Directory.OwnerId = null;
            modelo.Directory.Identity.Type = IdentityType.Group;

            await context.Organizations.AddAsync(modelo);

            context.SaveChanges();

            // Cuenta de usuario
            var account = new AccountModel()
            {
                Id = 0,
                Visibility = Visibility.Hidden,
                Name = "Admin",
                Password = $"pwd@{DateTime.Now.Year}",
                IdentityService = IdentityService.LIN,
                Identity = new IdentityModel()
                {
                    Status = IdentityStatus.Enable,
                    CreationTime = DateTime.Now,
                    EffectiveTime = DateTime.Now,
                    ExpirationTime = DateTime.Now.AddYears(10),
                    Unique = $"admin@{modelo.Directory.Identity.Unique}"
                }
            };


            var resultAccount = new AccountModel()
            {
                Password = account.Password,
                Identity = new()
                {
                    Unique = account.Identity.Unique,
                }
            };

            account = Services.Formats.Account.Process(account);

            await context.Accounts.AddAsync(account);

            context.SaveChanges();

            // IamRoles.
            var rol = new IdentityRolesModel
            {
                Identity = account.Identity,
                Organization = modelo,
                Rol = Roles.Administrator
            };


            await context.IdentityRoles.AddAsync(rol);

            context.SaveChanges();

            modelo.Directory.Owner = modelo;
            modelo.Directory.Members.Add(new()
            {
                Group = modelo.Directory,
                Identity = account.Identity,
                Type = GroupMemberTypes.User
            });

            context.SaveChanges();


            transaction.Commit();


            var responseFinal = new CreateResponse()
            {
                Response = Responses.Success,
                LastId = modelo.Id
            };


            return responseFinal;

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
    /// Obtener una organización según el Id.
    /// </summary>
    /// <param name="id">Id.</param>
    /// <param name="context">Contexto de base de datos.</param>
    public async Task<ReadOneResponse<OrganizationModel>> Read(int id)
    {

        try
        {

            var org = await (from g in context.Organizations
                             where g.Id == id
                             select g).FirstOrDefaultAsync();

            // Si la cuenta no existe.
            if (org == null)
                return new()
                {
                    Response = Responses.NotRows
                };

            // Success.
            return new()
            {
                Response = Responses.Success,
                Model = org
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
    /// Obtener las organizaciones donde una identidad pertenece.
    /// </summary>
    /// <param name="id">Identidad</param>
    public async Task<ReadAllResponse<OrganizationModel>> ReadAll(int id)
    {

        try
        {

            // Consulta.
            var query = await (from org in context.Organizations
                               join gm in context.GroupMembers
                               on org.DirectoryId equals gm.GroupId
                               where gm.IdentityId == id
                               select org).ToListAsync();


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
                Models = query
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
    /// Obtener el dominio de una organización.
    /// </summary>
    /// <param name="id">Id de la organización.</param>
    /// <param name="context">Contexto de base de datos.</param>
    public async Task<ReadOneResponse<IdentityModel>> GetDomain(int id)
    {

        try
        {

            var org = await (from g in context.Organizations
                             where g.Id == id
                             select g.Directory.Identity).FirstOrDefaultAsync();

            // Si la cuenta no existe.
            if (org == null)
                return new()
                {
                    Response = Responses.NotRows
                };

            // Success.
            return new()
            {
                Response = Responses.Success,
                Model = org
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



    public async Task<ReadOneResponse<int>> ReadDirectory(int id)
    {

        try
        {

            var org = await (from g in context.Organizations
                             where g.Id == id
                             select g.DirectoryId).FirstOrDefaultAsync();

            // Si la cuenta no existe.
            if (org <= 0)
                return new()
                {
                    Response = Responses.NotRows
                };

            // Success.
            return new()
            {
                Response = Responses.Success,
                Model = org
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

    public async Task<ReadOneResponse<int>> ReadDirectoryIdentity(int id)
    {

        try
        {

            var org = await (from g in context.Organizations
                             where g.Id == id
                             select g.Directory.IdentityId).FirstOrDefaultAsync();

            // Si la cuenta no existe.
            if (org <= 0)
                return new()
                {
                    Response = Responses.NotRows
                };

            // Success.
            return new()
            {
                Response = Responses.Success,
                Model = org
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