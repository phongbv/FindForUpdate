using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FindForUpdate.DAO.Extensions
{
    public static class EntityExtension
    {
        static ConcurrentDictionary<string, TableMapping> tablesMapping = new ConcurrentDictionary<string, TableMapping>();
       
        public static T FindForUpdate<T>(this DbSet<T> dbSet, decimal updSeq, params object[] keyValues) where T : IAuditEntity
        {
            using (var dbContext = new DataContext())
            {
                var dbUpdSeq = dbContext.Set<T>().Find(keyValues)?.UdpSeq;
                var currentEntity = dbSet.Find(keyValues);
                if (dbUpdSeq == null || updSeq == dbUpdSeq)
                {
                    if (currentEntity != null)
                    {
                        var currentCtx = dbSet.GetContext();
                        DoLock(dbSet, currentCtx, keyValues);
                    }
                    return currentEntity;
                }
                else
                {
                    throw new Exception("Record has been modified by another user");
                }
            }
        }

        private static void DoLock<T>(this DbSet<T> dbSet, params object[] keyValues) where T : class
        {
            DoLock(dbSet, null, keyValues);
        }
        private static void DoLock<T>(this DbSet<T> dbSet, DbContext dbContext = null, params object[] keyValues) where T : class
        {
            dbContext = dbContext ?? dbSet.GetContext();

            var _keyValuePairs = dbSet.GetEntityKeys().Zip(keyValues, (name, value) => new KeyValuePair<string, object>(name, value));
            var queryBuilder = new StringBuilder();
            queryBuilder.AppendFormat("SELECT 1 FROM {0} X WHERE ", GetTableMapping(typeof(T),dbContext).TableName);

            var entityKeyValues = _keyValuePairs.ToArray();
            var parameters = new OracleParameter[entityKeyValues.Length];

            for (var i = 0; i < entityKeyValues.Length; i++)
            {
                if (i > 0)
                {
                    queryBuilder.Append(" AND ");
                }

                var name = string.Format(CultureInfo.InvariantCulture, "p{0}", i.ToString(CultureInfo.InvariantCulture));
                queryBuilder.AppendFormat("X.{0} = :{1}", entityKeyValues[i].Key, name);
                parameters[i] = new OracleParameter(name, entityKeyValues[i].Value);
            }
            queryBuilder.Append(" FOR UPDATE");
            dbContext.Database.ExecuteSqlCommand(queryBuilder.ToString(), parameters);
        }

        private static DbContext GetContext<TEntity>(this DbSet<TEntity> dbSet)
            where TEntity : class
        {
            object internalSet = dbSet
                .GetType()
                .GetField("_internalSet", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(dbSet);
            object internalContext = internalSet
                .GetType()
                .BaseType
                .GetField("_internalContext", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(internalSet);
            return (DbContext)internalContext
                .GetType()
                .GetProperty("Owner", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(internalContext, null);
        }

        private static IEnumerable<string> GetEntityKeys<T>(this DbSet<T> dbSet) where T : class
        {
            return GetEntityKeys<T>(dbSet.GetContext());
        }

        private static IEnumerable<string> GetEntityKeys<T>(this DbContext dbContext) where T : class
        {
            ObjectContext objectContext = ((IObjectContextAdapter)dbContext).ObjectContext;
            var metada = objectContext.MetadataWorkspace.GetItemCollection(DataSpace.SSpace).GetItems<EntityContainer>().Single()
                     .BaseEntitySets
                     .OfType<EntitySet>().First();
            ObjectSet<T> set = objectContext.CreateObjectSet<T>();
            IEnumerable<string> keyNames = set.EntitySet.ElementType
                                                        .KeyMembers
                                                        .Select(k => k.Name);
            return keyNames;
        }

        private static TableMapping GetTableMapping(Type type, DbContext dbContext)
        {
            if (tablesMapping.ContainsKey(type.Name)) return tablesMapping[type.Name];
            ObjectContext objectContext = ((IObjectContextAdapter)dbContext).ObjectContext;
            var metadata = objectContext.MetadataWorkspace;
            // Get the part of the model that contains info about the actual CLR types
            var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));
            // Get the entity type from the model that maps to the CLR type
            var entityType = metadata.GetItems<EntityType>(DataSpace.OSpace)
                                .Single(e => objectItemCollection.GetClrType(e) == type);
            // Get the entity set that uses this entity type
            var entitySet = metadata
                .GetItems<EntityContainer>(DataSpace.CSpace)
                      .Single()
                      .EntitySets
                      .Single(s => s.ElementType.Name == entityType.Name);
            // Find the mapping between conceptual and storage model for this entity set
            var mapping = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace)
                          .Single()
                          .EntitySetMappings
                          .Single(s => s.EntitySet == entitySet);

            // Find the storage entity set (table) that the entity is mapped
            var tableEntitySet = mapping
                .EntityTypeMappings.Single()
                .Fragments.Single()
                .StoreEntitySet;

            // Return the table name from the storage entity set
            var tableName = tableEntitySet.MetadataProperties["Table"].Value ?? tableEntitySet.Name;

            var columnMappings = mapping
                .EntityTypeMappings.Single()
                .Fragments.Single()
                .PropertyMappings
                .OfType<ScalarPropertyMapping>();
            var tableMaping = new TableMapping()
            {
                TableName = tableName.ToString()
            };

            foreach (var column in columnMappings)
            {
                tableMaping.ColumnMappings[column.Property.Name] = column.Column.Name;
            }
            tablesMapping[type.Name] = tableMaping;
            return tableMaping;
        }

    }

    internal class TableMapping
    {
        public string TableName { get; set; }
        public Dictionary<string, string> ColumnMappings { get; set; } = new Dictionary<string, string>();
    }
}
