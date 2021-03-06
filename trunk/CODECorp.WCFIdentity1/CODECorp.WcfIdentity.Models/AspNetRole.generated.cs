#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the ClassGenerator.ttinclude code generation file.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Data.Common;
using System.Collections.Generic;
using Telerik.OpenAccess;
using Telerik.OpenAccess.Metadata;
using Telerik.OpenAccess.Data.Common;
using Telerik.OpenAccess.Metadata.Fluent;
using Telerik.OpenAccess.Metadata.Fluent.Advanced;
using System.ComponentModel.DataAnnotations;
using CODECorp.WcfIdentity.Models;

namespace CODECorp.WcfIdentity.Models
{
    public partial class AspNetRole
    {
        private string _id;
        [StringLength(128)]
        [Required()]
        [Key()]
        public virtual string Id
        {
            get
            {
                return this._id;
            }
            set
            {
                this._id = value;
            }
        }

        private string _name;
        [Required()]
        public virtual string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }

        private IList<AspNetUser> _aspNetUsers = new List<AspNetUser>();
        public virtual IList<AspNetUser> AspNetUsers
        {
            get
            {
                return this._aspNetUsers;
            }
        }

    }
}
#pragma warning restore 1591
