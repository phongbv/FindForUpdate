using FindForUpdate.DAO;
using FindForUpdate.DAO.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindForUpdate
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var dbContext = new DataContext())
            {
                using(var trans = dbContext.Database.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    var a = dbContext.TestLocks.FindForUpdate(1, 1);
                    Console.ReadKey();
                    trans.Rollback();
                }
            }
        }
    }
}
