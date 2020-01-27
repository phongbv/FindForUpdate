using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindForUpdate.DAO
{
    public abstract class IAuditEntity
    {
        [Column("UPD_SEQ")]
        public int UdpSeq { get; set; }
    }
}
