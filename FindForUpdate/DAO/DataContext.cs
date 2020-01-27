using FindForUpdate.DAO;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace FindForUpdate
{
    public class DataContext : DbContext
    {
        public DataContext() : base("name=DataContext")
        {
            this.Database.Log = s => WriteLine(s);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("DEV");
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<TestLockDAO> TestLocks { get; set; }
    }
}
