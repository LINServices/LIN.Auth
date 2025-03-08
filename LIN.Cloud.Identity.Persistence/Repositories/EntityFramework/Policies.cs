using LIN.Cloud.Identity.Services.Utils;

namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class Policies(DataContext context, PoliciesRequirement policiesRequirement, IdentityDataService identityService, PolicyService policyService)
{

    /// <summary>
    /// Crear nueva política.
    /// </summary>
    /// <param name="modelo">Modelo.</param>
    /// <returns>Retorna el id.</returns>
    public async Task<CreateResponse> Create(PolicyModel modelo)
    {
        try
        {

            modelo.Id = Guid.NewGuid();

            // Attach.
            context.Attach(modelo.OwnerIdentity);

            foreach (var e in modelo.ApplyFor)
            {
                e.Identity = context.AttachOrUpdate(e.Identity);
                e.Policy = modelo;
            }

            // Guardar la cuenta.
            await context.Policies.AddAsync(modelo);
            context.SaveChanges();

            return new()
            {
                Response = Responses.Success,
                LastUnique = modelo.Id.ToString()
            };

        }
        catch (Exception)
        {
            return new()
            {
                Response = Responses.ResourceExist
            };
        }

    }


    /// <summary>
    /// Obtener las políticas donde el usuario es dueño.
    /// </summary>
    /// <param name="id">Id de la identidad.</param>
    /// <returns>Retorna la lista de políticas.</returns>
    public async Task<ReadAllResponse<PolicyModel>> ReadAllOwn(int id)
    {

        // Ejecución
        try
        {

            // Políticas.
            var policies = await (from policy in context.Policies
                                  where policy.OwnerIdentityId == id
                                  select policy).ToListAsync();

            return new(Responses.Success, policies);

        }
        catch (Exception)
        {
        }
        return new();
    }


    /// <summary>
    /// Obtener una política.
    /// </summary>
    /// <param name="guid">Id.</param>
    public async Task<ReadOneResponse<PolicyModel>> Read(Guid guid)
    {

        // Ejecución
        try
        {

            // Políticas.
            var policie = await (from policy in context.Policies
                                 where policy.Id == guid
                                 select new PolicyModel
                                 {
                                     Description = policy.Description,
                                     Id = policy.Id,
                                     Name = policy.Name,
                                     OwnerIdentity = new()
                                     {
                                         Id = policy.OwnerIdentityId,
                                         Unique = policy.OwnerIdentity.Unique,
                                         Type = policy.OwnerIdentity.Type
                                     }
                                 }).FirstOrDefaultAsync();

            if (policie == null)
                return new(Responses.NotRows);

            return new(Responses.Success, policie);

        }
        catch (Exception)
        {
        }
        return new();
    }


    /// <summary>
    /// Obtener las políticas asociadas a una organización.
    /// </summary>
    /// <param name="id">Id de la organización.</param>
    public async Task<ReadAllResponse<PolicyModel>> ReadAll(int id)
    {

        // Ejecución
        try
        {

            // Políticas.
            var policies = await (from policy in context.Policies
                                  join gr in context.Groups
                                  on id equals gr.OwnerId
                                  where policy.OwnerIdentityId == gr.IdentityId
                                  select new PolicyModel
                                  {
                                      Description = policy.Description,
                                      Id = policy.Id,
                                      Name = policy.Name,
                                      OwnerIdentity = new()
                                      {
                                          Id = policy.OwnerIdentityId,
                                          Unique = policy.OwnerIdentity.Unique,
                                          Type = policy.OwnerIdentity.Type
                                      }
                                  }).Distinct().ToListAsync();

            return new(Responses.Success, policies);

        }
        catch (Exception)
        {
        }
        return new();
    }


    /// <summary>
    /// Obtener las políticas aplicables a una identidad.
    /// </summary>
    /// <param name="id">Id de la identidad.</param>
    public async Task<ReadAllResponse<PolicyModel>> ApplicablePolicies(int id)
    {

        // Ejecución
        try
        {

            // Obtener las identidades
            var identities = await identityService.GetIdentities(id);

            // Políticas.
            var policies = await (from policy in context.IdentityOnPolicies
                                  where identities.Contains(policy.IdentityId)
                                  select policy.Policy).Distinct().ToListAsync();

            return new(Responses.Success, policies);

        }
        catch (Exception)
        {
        }
        return new();
    }


    /// <summary>
    /// Validar si una identidad y sus padres tienen acceso a una política.
    /// </summary>
    /// <param name="id">Id de la identidad base.</param>
    /// <param name="policyId">Id de la política.</param>
    public async Task<ResponseBase> HasFor(int id, string policyId)
    {

        // Ejecución
        try
        {

            // Convertir el id.
            var policyResult = Guid.TryParse(policyId, out Guid result);

            // Si hubo un error.
            if (!policyResult)
                return new(Responses.InvalidParam);

            // Obtener identidades base.
            var ids = await identityService.GetIdentities(id);

            // Políticas.
            var have = await (from policy in context.Policies
                              where policy.Id == result
                              && policy.ApplyFor.Any(t => ids.Contains(t.IdentityId))
                              select policy).AnyAsync();

            // No tiene acceso.
            if (!have)
                return new(Responses.Unauthorized);

            // Obtener requerimientos.
            var requirements = await policiesRequirement.ReadAll(result);

            // Validar.
            var validate = policyService.Validate(requirements.Models);

            // Respuesta.
            return new(validate is null ? Responses.Success : Responses.Unauthorized)
            {
                Message = "Error",
                Errors = [validate]
            };

        }
        catch (Exception)
        {
        }
        return new();
    }


    /// <summary>
    /// Eliminar una política.
    /// </summary>
    /// <param name="policyId">Id de la política.</param>
    public async Task<ResponseBase> Remove(string policyId)
    {

        // Ejecución
        try
        {

            // Convertir el id.
            var policyResult = Guid.TryParse(policyId, out Guid result);

            // Si hubo un error.
            if (!policyResult)
                return new(Responses.InvalidParam);

            // Eliminar vinculos a políticas.
            var deleted = await (from policy in context.IdentityOnPolicies
                                 where policy.PolicyId == result
                                 select policy).ExecuteDeleteAsync();

            // Políticas.
            deleted = await (from policy in context.Policies
                             where policy.Id == result
                             select policy).ExecuteDeleteAsync();

            // Respuesta.
            return new(Responses.Success);

        }
        catch (Exception)
        {
        }
        return new();
    }


    /// <summary>
    /// Agregar una identidad a una política.
    /// </summary>
    /// <param name="id">Id de la identidad base.</param>
    /// <param name="policyId">Id de la política.</param>
    public async Task<ResponseBase> AddMember(int id, string policyId)
    {

        // Ejecución
        try
        {

            // Convertir el id.
            var policyResult = Guid.TryParse(policyId, out Guid result);

            // Si hubo un error.
            if (!policyResult)
                return new(Responses.InvalidParam);

            IdentityAllowedOnPolicyModel allow = new()
            {
                Policy = new()
                {
                    Id = result
                },
                Identity = new()
                {
                    Id = id
                }
            };

            context.Attach(allow.Policy);
            context.Attach(allow.Identity);

            await context.IdentityOnPolicies.AddAsync(allow);

            context.SaveChanges();

            // Respuesta.
            return new(Responses.Success);

        }
        catch (Exception)
        {
        }
        return new();
    }


    /// <summary>
    /// Eliminar una identidad a una política.
    /// </summary>
    /// <param name="id">Id de la identidad base.</param>
    /// <param name="policyId">Id de la política.</param>
    public async Task<ResponseBase> RemoveMember(int id, string policyId)
    {

        // Ejecución
        try
        {

            // Convertir el id.
            var policyResult = Guid.TryParse(policyId, out Guid result);

            // Si hubo un error.
            if (!policyResult)
                return new(Responses.InvalidParam);

            // Eliminar vínculos a políticas.
            var deleted = await (from policy in context.IdentityOnPolicies
                                 where policy.PolicyId == result
                                 && policy.IdentityId == id
                                 select policy).ExecuteDeleteAsync();

            // Respuesta.
            return new(Responses.Success);

        }
        catch (Exception)
        {
        }
        return new();
    }

}