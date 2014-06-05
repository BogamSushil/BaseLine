using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Repository;

namespace Data.DomainEntity
{
    public class Category
    {
        public long CategoryId { get; set; }

        public long? ParentCategoryId { get; set; }

        public string ImageName { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

    }
}
