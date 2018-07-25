namespace ConsoleApp5
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class QueuedTests_HistoryModel : DbContext
    {
        public QueuedTests_HistoryModel()
            : base("name=QueuedTests_HistoryModel")
        {
        }

        public virtual DbSet<QueuedTests_History> QueuedTests_History { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QueuedTests_History>()
                .Property(e => e.ApplicationName)
                .IsUnicode(false);

            modelBuilder.Entity<QueuedTests_History>()
                .Property(e => e.TestName)
                .IsUnicode(false);

            modelBuilder.Entity<QueuedTests_History>()
                .Property(e => e.UserName)
                .IsUnicode(false);

            modelBuilder.Entity<QueuedTests_History>()
                .Property(e => e.Log)
                .IsUnicode(false);

            modelBuilder.Entity<QueuedTests_History>()
                .Property(e => e.Environment)
                .IsUnicode(false);
        }
    }
}
