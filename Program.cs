using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

class BaseEntity
{
    public int Id { get; set; }
}

class Product : BaseEntity
{
    public string Name { get; set; }
}

interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get;  }
    List<Expression<Func<T, object>>> Includes { get; }
}

abstract class BaseSpecification<T> : ISpecification<T> where T : BaseEntity
{
    public Expression<Func<T, bool>> Criteria { get; }
    public List<Expression<Func<T, object>>> Includes { get; } 
        = new List<Expression<Func<T, object>>>();

    public BaseSpecification() { }

    public BaseSpecification(Expression<Func<T, bool>> criteria)
        => Criteria = criteria;

    protected void AddIncludes(Expression<Func<T, object>> includeExpression)
        => Includes.Add(includeExpression);
}

class GetProductSpecification : BaseSpecification<Product>
{
    public GetProductSpecification() { }
    public GetProductSpecification(int id) : base(x => x.Id == id) { }
}


class SpecificationEvaluator<T>
{
    public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> spec)
    {
        var query = inputQuery;

        if (spec.Criteria != null)
            query = query.Where(spec.Criteria);

        // relations
        // query = spec.Includes.Aggregate(query, (current, include) 
            // => current.Include(include));

        return query;
    }
}

interface IGenericRepository<T>
{
    T GetById(ISpecification<T> spec);
    List<T> ListAll(ISpecification<T> spec);
}

class GenericRepository<T> : IGenericRepository<T>
{
    private readonly List<T> list;

    public GenericRepository(List<T> list)
    {
        this.list = list;
    }

    public T GetById(ISpecification<T> spec)
    {
        return ApplySpecification(spec).FirstOrDefault();
    }

    public List<T> ListAll(ISpecification<T> spec)
    {
        return ApplySpecification(spec).ToList();
    }

    private IQueryable<T> ApplySpecification(ISpecification<T> spec)
    {
        return SpecificationEvaluator<T>.GetQuery(list.AsQueryable(), spec);
    }
}

class Program 
{   
    static void Main() 
    {
        var productList = new List<Product>()
        {
            new Product{ Id = 1, Name = "one" },
            new Product{ Id = 2, Name = "two" },
            new Product{ Id = 3, Name = "three" },
        };

        var genericRepository = new GenericRepository<Product>(productList);

        var spec = new GetProductSpecification(2);
        var product = genericRepository.GetById(spec);
        Console.WriteLine($"Single element:{product.Name}");
        
        Console.WriteLine();

        var spec2 = new GetProductSpecification();
        var products = genericRepository.ListAll(spec2);
        foreach (var p in products)
            Console.WriteLine($"{p.Id}.{p.Name}");
    } 
}