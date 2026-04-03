using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;

namespace Vktun.PhoneLogin.EntityFrameworkCore;

public class EfCorePhoneLoginUserLookup(IDbContextProvider<IIdentityDbContext> dbContextProvider) : IPhoneLoginUserLookup
{
    private readonly IDbContextProvider<IIdentityDbContext> _dbContextProvider = dbContextProvider;

    public async Task<IdentityUser?> FindByPhoneNumberAsync(string phoneNumber)
    {
        var dbContext = await _dbContextProvider.GetDbContextAsync();
        return await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
    }
}
