namespace ConsoleApp5
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class QueuedTestsModel : DbContext
    {
        public QueuedTestsModel()
            : base("name=QueuedTestsModel")
        {
        }

        public virtual DbSet<QueuedTest> QueuedTests { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QueuedTest>()
                .Property(e => e.ApplicationName)
                .IsUnicode(false);

            modelBuilder.Entity<QueuedTest>()
                .Property(e => e.TestName)
                .IsUnicode(false);

            modelBuilder.Entity<QueuedTest>()
                .Property(e => e.UserName)
                .IsUnicode(false);

            modelBuilder.Entity<QueuedTest>()
                .Property(e => e.Environment)
                .IsUnicode(false);
        }
    }
}
