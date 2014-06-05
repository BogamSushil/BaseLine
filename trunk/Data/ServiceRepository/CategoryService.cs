using Data.DomainEntity;
using Data.Repository;
using System;
using System.Collections.Generic;

namespace Data.ServiceRepository
{
    using System.Data;

    public class CategoryService : Repository<Category, Int64>
    {
        #region "Const, Properties, Members"

        private const string ManageSpName = "ManageCategory";

        private const string GetAllSpName = "ManageCategoryGet";

        #endregion

        #region "Overridden Methods"

        public override Dictionary<string, Tuple<object, ParameterDirection, bool>> SpParameters(
            DatabaseAction action,
            Category entity)
        {
            var parameters = new Dictionary<string, Tuple<object, ParameterDirection, bool>>();

            parameters.Add(
                "ParentCategoryId",
                new Tuple<object, ParameterDirection, bool>(entity.ParentCategoryId, ParameterDirection.Input, false));
            switch (action)
            {
                case DatabaseAction.Insert:
                case DatabaseAction.Update:
                case DatabaseAction.Delete:
                case DatabaseAction.ActiveDeactive:
                case DatabaseAction.DeleteAll:
                    parameters.Add(
                        "CategoryId",
                        new Tuple<object, ParameterDirection, bool>(
                            entity.CategoryId,
                            ParameterDirection.InputOutput,
                            true));
                    parameters.Add(
                        "Name",
                        new Tuple<object, ParameterDirection, bool>(entity.Name, ParameterDirection.Input, false));
                    parameters.Add(
                        "Description",
                        new Tuple<object, ParameterDirection, bool>(entity.Description, ParameterDirection.Input, false));
                    parameters.Add(
                        "ImageName",
                        new Tuple<object, ParameterDirection, bool>(entity.ImageName, ParameterDirection.Input, false));
                    parameters.Add(
                        "IsActive",
                        new Tuple<object, ParameterDirection, bool>(entity.IsActive, ParameterDirection.Input, false));
                    parameters.Add(
                        "UpdatedBy",
                        new Tuple<object, ParameterDirection, bool>(null, ParameterDirection.Input, false));
                    break;
                case DatabaseAction.Get:
                case DatabaseAction.GetAll:
                case DatabaseAction.GetChilds:
                    parameters.Add(
                        "CategoryId",
                        new Tuple<object, ParameterDirection, bool>(entity.CategoryId, ParameterDirection.Input, true));
                    break;
                default:
                    throw new ArgumentOutOfRangeException("action");
            }
            return parameters;
        }

        public override bool IsDuplicate(Category entity, ValidateAction action, List<string> errors = null)
        {
            return false;
        }

        public override bool IsValidData(Category entity, ValidateAction action, List<string> errors = null)
        {
            return true;
        }

        public override string GetSpName(DatabaseAction action)
        {
            switch (action)
            {
                case DatabaseAction.Insert:
                case DatabaseAction.Update:
                case DatabaseAction.Delete:
                case DatabaseAction.ActiveDeactive:
                case DatabaseAction.DeleteAll:
                    return ManageSpName;
                case DatabaseAction.Get:
                case DatabaseAction.GetChilds:
                case DatabaseAction.GetAll:
                    return GetAllSpName;
            }
            return null;
        }

        public override Int64 GetId(Category entity)
        {
            return entity.CategoryId;
        }

        public override void SetId(Category entity, Int64 id)
        {
            entity.CategoryId = id;
        }

        #endregion


    }


}
