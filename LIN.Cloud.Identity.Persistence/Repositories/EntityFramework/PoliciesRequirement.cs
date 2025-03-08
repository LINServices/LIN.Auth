namespace LIN.Cloud.Identity.Persistence.Repositories.EntityFramework;

public class PoliciesRequirement(DataContext context)
{

    public async Task<CreateResponse> Create(PolicyRequirementModel modelo)
    {
        try
        {

            modelo.Policy = new()
            {
                Id = modelo.PolicyId
            };

            // Attach.
            context.Attach(modelo.Policy);

            modelo.Requirement ??= System.Text.Json.JsonSerializer.Serialize(modelo.Requirement);

            // Guardar la cuenta.
            await context.PolicyRequirements.AddAsync(modelo);
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


    public async Task<ReadAllResponse<PolicyRequirementModel>> ReadAll(Guid policy)
    {

        // Ejecución
        try
        {

            // Políticas.
            var policies = await (from policyRequirement in context.PolicyRequirements
                                  where policyRequirement.PolicyId == policy
                                  select policyRequirement).Distinct().ToListAsync();

            return new(Responses.Success, policies);

        }
        catch (Exception)
        {
        }
        return new();
    }


    public async Task<ResponseBase> Remove(int policyRequirement)
    {

        // Ejecución
        try
        {

            var deleted = await (from policy in context.PolicyRequirements
                                 where policy.Id == policyRequirement
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