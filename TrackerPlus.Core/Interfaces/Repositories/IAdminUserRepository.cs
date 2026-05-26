namespace TrackerPlus.Core.Interfaces.Repositories;

public interface IAdminUserRepository
{
    Task<bool> ValidateAsync(string userId, string password);
}
