using Data.Contexts;
using Data.Entities;

namespace Data.Repositories;

public interface IUserRepository : IBaseRepository<UserEntity, UserEntity>
{
}

public class UserRepository : BaseRepository<UserEntity, UserEntity>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }
}
