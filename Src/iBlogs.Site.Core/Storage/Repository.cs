﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using iBlogs.Site.Core.Common.Extensions;
using iBlogs.Site.Core.Common.Request;
using iBlogs.Site.Core.Common.Response;

namespace iBlogs.Site.Core.Storage
{

    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class, IEntityBase
    {
        /// <summary>
        /// Gets EF DbContext object.
        /// </summary>
        private readonly ConcurrentDictionary<int, TEntity> _entityDic;

        public Repository()
        {
            _entityDic = StorageWarehouse.Get<TEntity>();
        }


        public IEnumerable<TEntity> GetAll()
        {
            return _entityDic.Values;
        }

        public TEntity Insert(TEntity entity)
        {
            if (entity.Id == 0)
            {
                entity.Id = _entityDic.Max(u => u.Key) + 1;
            }
            _entityDic.TryAdd(entity.Id, entity);
            return entity;
        }

        public Task<TEntity> InsertAsync(TEntity entity)
        {
            return Task.FromResult(Insert(entity));
        }

        public int InsertAndGetId(TEntity entity)
        {
            entity = Insert(entity);
            return entity.Id;
        }

        public async Task<int> InsertAndGetIdAsync(TEntity entity)
        {
            entity = await InsertAsync(entity);
            return entity.Id;
        }

        public int InsertOrUpdateAndGetId(TEntity entity)
        {
            entity = InsertOrUpdate(entity);
            return entity.Id;
        }

        public async Task<int> InsertOrUpdateAndGetIdAsync(TEntity entity)
        {
            entity = await InsertOrUpdateAsync(entity);
            return entity.Id;
        }

        public TEntity Update(TEntity entity)
        {
            _entityDic[entity.Id] = entity;
            return entity;
        }

        public Task<TEntity> UpdateAsync(TEntity entity)
        {
            entity = Update(entity);
            return Task.FromResult(entity);
        }

        public void Delete(TEntity entity)
        {
            _entityDic.TryRemove(entity.Id, out _);
            entity.Deleted = true;
        }

        public void Delete(int id)
        {
            _entityDic.TryRemove(id, out _);
        }

        public int Count()
        {
            return _entityDic.Keys.Count;
        }

        public TEntity InsertOrUpdate(TEntity entity)
        {
            return entity.Id <= 0 ? Insert(entity) : Update(entity);
        }

        public async Task<TEntity> InsertOrUpdateAsync(TEntity entity)
        {
            return entity.Id <= 0 ? await InsertAsync(entity) : await UpdateAsync(entity);
        }
        public TEntity FirstOrDefault(int id)
        {
            return _entityDic.TryGetValue(id, out var result) ? result : null;
        }

        public Page<TEntity> Page(IOrderedEnumerable<TEntity> source, PageParam pageParam)
        {

            var orderByName = pageParam.OrderBy;

            if (pageParam.OrderBy.IsNullOrWhiteSpace() ||
                typeof(TEntity).GetProperties().FirstOrDefault(p => p.Name == pageParam.OrderBy) == null)
                orderByName = "Id";
            var orderProp = TypeDescriptor.GetProperties(typeof(TEntity)).Find(orderByName, true);
            switch (pageParam.OrderType)
            {
                case OrderType.Asc:
                    source = source.OrderBy(s => orderProp.GetValue(s));
                    break;
                default:
                    source = source.OrderByDescending(s => orderProp.GetValue(s));
                    break;
            }

            var total = _entityDic.Keys.Count;
            var rows = source.Skip((pageParam.Page - 1) * pageParam.Limit).Take(pageParam.Limit).ToList();
            return new Page<TEntity>(total, pageParam.Page++, pageParam.Limit, rows);
        }
        public IEnumerable<TEntity> GetAllIncluding(params Expression<Func<TEntity, object>>[] propertySelectors)
        {
            if (!propertySelectors.IsNullOrEmpty())
            {
                foreach (var propertySelector in propertySelectors)
                {
                    var propertyInfo = GetPropertyInfo(propertySelector);
                    var foreignKeyAttribute = (ForeignKeyAttribute)propertyInfo.GetCustomAttributes().FirstOrDefault(a => a is ForeignKeyAttribute);
                    if (foreignKeyAttribute == null)
                    {
                        throw new ArgumentException($"无法在{propertyInfo.ToString()}上找到ForeignKey");
                    }

                    var foreignKeyType = propertyInfo.PropertyType;

                    foreach (var value in _entityDic.Values)
                    {
                        var foreignKeyId = (int?)value.GetType().GetProperty(foreignKeyAttribute.Name)?.GetValue(value);
                        if(!foreignKeyId.HasValue)
                            throw new ArgumentException($"Id:{value.Id},外键{foreignKeyAttribute.Name}没有值");
                        var foreignKeyValue = StorageWarehouse.Get<foreignKeyType>();
                    }



                }
            }
        }

        public PropertyInfo GetPropertyInfo<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda)
        {
            var type = typeof(TSource);

            if (!(propertyLambda.Body is MemberExpression member))
                throw new ArgumentException($"Expression '{propertyLambda.ToString()}' refers to a method, not a property.");

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException($"Expression '{propertyLambda.ToString()}' refers to a field, not a property.");

            if (type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException($"Expression '{propertyLambda.ToString()}' refers to a property that is not from type {type}.");

            return propInfo;
        }

    }
}
