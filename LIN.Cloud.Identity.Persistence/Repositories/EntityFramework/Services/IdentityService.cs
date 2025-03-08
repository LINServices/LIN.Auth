namespace LIN.Cloud.Identity.Services.Utils;

public class IdentityDataService(DataContext context) 
{

    /// <summary>
    /// Obtener la identidades asociadas a una identidad base.
    /// </summary>
    /// <param name="identity">Identidad base.</param>
    public async Task<List<int>> GetIdentities(int identity)
    {
        List<int> result = [identity];
        await GetIdentities(identity, result);
        return result;
    }


    /// <summary>
    /// Obtener la identidades asociadas a una identidad base.
    /// </summary>
    /// <param name="identity">Identidad base.</param>
    /// <param name="ids">Identidades encontradas.</param>
    private async Task GetIdentities(int identity, List<int> ids)
    {
        // Consulta.
        var query = from id in context.Identities
                    where id.Id == identity
                    select new
                    {
                        // Encontrar grupos donde la identidad pertenece.
                        In = (from member in context.GroupMembers
                              where !ids.Contains(member.Group.IdentityId)
                              && member.IdentityId == identity
                              select member.Group.IdentityId).ToList(),
                    };

        // Si no hay elementos.
        if (!query.Any())
            return;

        // Ejecuta la consulta.
        var local = query.ToList();

        // Obtiene las bases.
        var bases = local.SelectMany(t => t.In);

        // Agregar a los objetos.
        ids.AddRange(bases);

        // Recorrer.
        foreach (var @base in bases)
            await GetIdentities(@base, ids);

    }

}