using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Domain.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly DbContext Context;
    protected readonly DbSet<TEntity> Entities;

    public Repository(DbContext context)
    {
        Context = context;
        Entities = Context.Set<TEntity>();
    }

    public void Add(TEntity entity)
    {
        Entities.Add(entity);
    }

    public void Update(TEntity entity)
    {
        Entities.Update(entity);
    }

    public void Delete(TEntity entity)
    {
        Entities.Remove(entity);
    }

    public void Delete(Guid id)
    {
        Entities.Remove(Entities.Find(id));
    }

    public TEntity? GetById(Guid id)
    {
        return Entities.Find(id);
    }

    public IEnumerable<TEntity> GetAll()
    {
        return Entities.ToList();
    }

    public IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
    {
        return Entities.Where(predicate);
    }
}
