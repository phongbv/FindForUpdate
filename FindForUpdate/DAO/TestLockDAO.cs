using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindForUpdate.DAO
{
    [Table("TEST_LOCK")]
    public class TestLockDAO : IAuditEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public int Id { get; set; }
        [Column("VALUE_1")]
        public string Value1 { get; set; }
        [Column("VALUE_2")]
        public string Value2 { get; set; }
    }
}
