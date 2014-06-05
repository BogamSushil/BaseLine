using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace Data.Repository
{
    public class BaseEntity<TId>
    {
        [NotMapped]
        public virtual TId Id { get; set; }
        [NotMapped]
        public bool IsActive { get; set; }
    }

    public class ChieldBaseEntity<TId, TPId> : BaseEntity<TId>
    {
        TPId ParentId { get; set; }
    }

    public enum DatabaseAction
    {
        None,
        Insert,
        Update,
        Delete,
        DeleteAll,
        Get,
        GetAll,
        ActiveDeactive,
        GetChilds,
        GetComplex
    }

    public enum ValidateAction
    {
        Insert,
        Update,
        Delete,
        Deactivate
    }

   
}
