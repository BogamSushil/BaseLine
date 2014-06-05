using Data.DataModel;
using Data.Helpers;
using Framework;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Data;
using EntityState = System.Data.Entity.EntityState;

namespace Data.Repository
{
    public abstract class Repository<TBaseEntity, TId> : DataEntities, IRepository<TBaseEntity, TId>
        where TBaseEntity : class
    {
        #region "Constants / Member variables"

        private const string OperationSpParameter = "Operation";

        #endregion

        #region Repository metods SP Functions / Public Methods IRepository Implemented Methods

        #region "Repository Get All Complex"

        public Tuple<List<TBaseEntity>, T1> GetAllComplex<T1>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
        {
            return this.GetAllComplexRaw<T1>(parameters, storedProcedureName);
        }

        public Tuple<List<TBaseEntity>, T1, T2> GetAllComplex<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            return this.GetAllComplexRaw<T1, T2>(parameters, storedProcedureName);
        }

        public Tuple<List<TBaseEntity>, List<T1>> GetAllComplexList<T1>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
        {
            return this.GetAllComplexListRaw<T1>(parameters, storedProcedureName);
        }

        public Tuple<List<TBaseEntity>, List<T1>, List<T2>> GetAllComplexList<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            return this.GetAllComplexListRaw<T1, T2>(parameters, storedProcedureName);
        }

        public Tuple<List<TBaseEntity>, T1, List<T2>> GetAllComplexSingleList<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            return GetAllComplexSingleListRaw<T1, T2>(parameters, storedProcedureName);
        }

        #endregion

        #region "Repository Get Complex"

        public Tuple<TBaseEntity, T1> GetComplex<T1>(Dictionary<string, object> parameters, string storedProcedureName) where T1 : class
        {
            return this.GetComplexRaw<T1>(parameters, storedProcedureName);
        }

        public Tuple<TBaseEntity, T1, T2> GetComplex<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            return this.GetComplexRaw<T1, T2>(parameters, storedProcedureName);
        }

        public Tuple<TBaseEntity, List<T1>> GetComplexList<T1>(Dictionary<string, object> parameters, string storedProcedureName) where T1 : class
        {
            return this.GetComplexListRaw<T1>(parameters, storedProcedureName);
        }

        public Tuple<TBaseEntity, List<T1>, List<T2>> GetComplexList<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            return this.GetComplexListRaw<T1, T2>(parameters, storedProcedureName);
        }

        public Tuple<TBaseEntity, T1, List<T2>> GetComplexSingleList<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            return GetComplexSingleListRaw<T1, T2>(parameters, storedProcedureName);
        }

        #endregion

        #region "Repository Get "

        public virtual TBaseEntity Get(TId id)
        {
            return this.GetRaw(id);
        }

        #endregion

        #region "Repository Get All"

        public virtual IEnumerable<TBaseEntity> GetAll(Expression<Func<TBaseEntity, bool>> filter = null,
            Func<IQueryable<TBaseEntity>, IOrderedQueryable<TBaseEntity>> orderBy = null,
            string includeProperties = "")
        {
            return GetAllRaw(filter, orderBy, includeProperties);
        }

        #endregion

        #region "Repository Add"

        public virtual TId Add(TBaseEntity entity, out int recordUpdated)
        {
            return this.AddRaw(entity, out recordUpdated);
        }

        public virtual TId Add(TBaseEntity entity)
        {
            return this.AddRaw(entity);
        }

        public virtual TId Add(TBaseEntity entity, out List<string> errors, out int recordUpdated)
        {
            return this.AddRaw(entity, out  errors, out recordUpdated);
        }

        #endregion

        #region "Repository Update"

        public virtual void Update(TBaseEntity entity, out int recordUpdated)
        {
            this.UpdateRaw(entity, out recordUpdated);
        }

        public virtual void Update(TBaseEntity entity)
        {
            this.UpdateRaw(entity);
        }

        public virtual void Update(TBaseEntity entity, out List<string> errors, out int recordUpdated)
        {
            this.UpdateRaw(entity, out errors, out recordUpdated);
        }

        #endregion

        #region "Repository Remove"

        public virtual void Remove(TBaseEntity entity)
        {
            this.RemoveRaw(entity);
        }

        public virtual void Remove(TBaseEntity entity, out int recordUpdated)
        {
            this.RemoveRaw(entity, out recordUpdated);
        }

        #endregion

        #region "Repository Activate / Deactivate"

        public virtual void ActiveDeactive(TBaseEntity entity)
        {
            this.ActiveDeactiveRaw(entity);
        }

        #endregion

        #region "Repository Get all Childs"

        public virtual IEnumerable<TBaseEntity> GetAllChilds(Dictionary<string, object> parameters)
        {
            return GetAllChildsRaw(parameters);
        }

        #endregion

        #endregion

        #region "Virtual/abstract  helper methods"

        /// <summary>
        /// Fill the Stored procedure parameters depending on the db action
        /// </summary>
        /// <param name="action">DB Action</param>
        /// <param name="entity">Entity </param>
        /// <returns>Returns the Dictionary of Parameter key as string and Object as value and parameter direction </returns>
        public abstract Dictionary<string, Tuple<object, ParameterDirection, bool>> SpParameters(
            DatabaseAction action,
            TBaseEntity entity);
    
        public abstract bool IsDuplicate(TBaseEntity entity, ValidateAction action, List<string> errors = null);

        public abstract bool IsValidData(TBaseEntity entity, ValidateAction action, List<string> errors = null);

        public abstract string GetSpName(DatabaseAction action);

        public abstract TId GetId(TBaseEntity entity);

        public abstract void SetId(TBaseEntity entity, TId id);

        /// <summary>
        /// Add the database operation parameter for manage sp
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public virtual Tuple<string, object> AddDatabaseOperationParameter(DatabaseAction action)
        {
            switch (action)
            {
                case DatabaseAction.Insert:
                    return new Tuple<string, object>(OperationSpParameter, (int)DatabaseAction.Insert);
                case DatabaseAction.Update:
                    return new Tuple<string, object>(OperationSpParameter, (int)DatabaseAction.Update);
                case DatabaseAction.Delete:
                    return new Tuple<string, object>(OperationSpParameter, (int)DatabaseAction.Delete);
                case DatabaseAction.DeleteAll:
                    return new Tuple<string, object>(OperationSpParameter, (int)DatabaseAction.DeleteAll);
                case DatabaseAction.Get:
                    return new Tuple<string, object>(OperationSpParameter, (int)DatabaseAction.Get);
                case DatabaseAction.GetAll:
                    return new Tuple<string, object>(OperationSpParameter, (int)DatabaseAction.GetAll);
                case DatabaseAction.ActiveDeactive:
                    return new Tuple<string, object>(OperationSpParameter, (int)DatabaseAction.ActiveDeactive);
                case DatabaseAction.GetChilds:
                    return new Tuple<string, object>(OperationSpParameter, (int)DatabaseAction.GetChilds);
            }
            return null;
        }

        #endregion

        #region "Protected Raw methods which are responsible for crud operation's"

        #region "Virtual Raw Add raw"
        /// <summary>
        /// Bare minimum implementation for adding an entity instance
        /// </summary>
        /// <remarks>Default implementation forwards the call to DAL manager
        /// </remarks>
        protected virtual TId AddRaw(TBaseEntity entity, out int recordUpdated)
        {
            return AddInternal(entity, out recordUpdated);
        }

        protected virtual TId AddRaw(TBaseEntity entity)
        {
            int recordUpdated;
            return AddInternal(entity, out recordUpdated);
        }


        protected virtual TId AddRaw(TBaseEntity entity, out List<string> errors, out int recordUpdated)
        {
            return AddInternal(entity, out errors, out recordUpdated);
        }

        #endregion

        #region "Virtual Raw Get Raw and Complex Raw"
        /// <summary>
        /// Bare minimum implementation to get the entity instance.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected virtual TBaseEntity GetRaw(TId id)
        {
            return GetInternal(id);
        }

        #region " Get Complex Raw"

        protected virtual Tuple<TBaseEntity, T1> GetComplexRaw<T1>(Dictionary<string, object> parameters, string storedProcedureName) where T1 : class
        {
            return this.GetComplexRawInternal<T1>(parameters, storedProcedureName);
        }

        protected virtual Tuple<TBaseEntity, T1, T2> GetComplexRaw<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            return GetComplexRawInternal<T1, T2>(parameters, storedProcedureName);
        }

        protected virtual Tuple<TBaseEntity, List<T1>> GetComplexListRaw<T1>(Dictionary<string, object> parameters, string storedProcedureName) where T1 : class
        {
            return GetComplexListRawInternal<T1>(parameters, storedProcedureName);
        }

        protected virtual Tuple<TBaseEntity, List<T1>, List<T2>> GetComplexListRaw<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            return GetComplexListRawInternal<T1, T2>(parameters, storedProcedureName);
        }

        protected virtual Tuple<TBaseEntity, T1, List<T2>> GetComplexSingleListRaw<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            return GetComplexSingleListRawInternal<T1, T2>(parameters, storedProcedureName);
        }

        #endregion

        #endregion

        #region "Get All And Complex get all"
        /// <summary>
        /// Bare minimum implementation for get all 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderBy"></param>
        /// <param name="includeProperties"></param>
        /// <returns></returns>
        protected virtual IEnumerable<TBaseEntity> GetAllRaw(Expression<Func<TBaseEntity, bool>> filter = null,
            Func<IQueryable<TBaseEntity>, IOrderedQueryable<TBaseEntity>> orderBy = null,
            string includeProperties = "")
        {
            return GetAllInternal(filter, orderBy);
        }

        #region " Get Complex Raw"

        protected virtual Tuple<List<TBaseEntity>, T1> GetAllComplexRaw<T1>(Dictionary<string, object> parameters, string storedProcedureName) where T1 : class 
        {
            return this.GetAllComplexRawInternal<T1>(parameters, storedProcedureName);
        }

        protected virtual Tuple<List<TBaseEntity>, T1, T2> GetAllComplexRaw<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            return GetAllComplexRawInternal<T1, T2>(parameters, storedProcedureName);
        }

        protected virtual Tuple<List<TBaseEntity>, List<T1>> GetAllComplexListRaw<T1>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
        {
            return GetAllComplexListRawInternal<T1>(parameters, storedProcedureName);
        }

        protected virtual Tuple<List<TBaseEntity>, List<T1>, List<T2>> GetAllComplexListRaw<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            return GetAllComplexListRawInternal<T1, T2>(parameters, storedProcedureName);
        }

        protected virtual Tuple<List<TBaseEntity>, T1, List<T2>> GetAllComplexSingleListRaw<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            return GetAllComplexSingleListRawInternal<T1, T2>(parameters, storedProcedureName);
        }

        #endregion

        #endregion

        #region "Virtual Raw Update raw"

        /// <summary>
        /// Bare minimum implementation for update an entity instance
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="recordUpdated"></param>
        protected virtual void UpdateRaw(TBaseEntity entity, out int recordUpdated)
        {
            UpdateInternal(entity, out recordUpdated);
        }

        protected virtual void UpdateRaw(TBaseEntity entity)
        {
            int recordUpdated;
            UpdateInternal(entity, out recordUpdated);
        }

        protected virtual void UpdateRaw(TBaseEntity entity, out List<string> errors, out int recordUpdated)
        {
            UpdateInternal(entity, out errors,  out recordUpdated);
        }

        #endregion

        #region "Virtual Raw Remove raw"
        /// <summary>
        /// Bare minimum implementation for remove an entity instance
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="recordUpdated"></param>
        protected virtual void RemoveRaw(TBaseEntity entity, out int recordUpdated)
        {
            RemoveInternal(entity, out recordUpdated);
        }

        protected virtual void RemoveRaw(TBaseEntity entity)
        {
            int recordUpdated; 
            RemoveInternal(entity, out recordUpdated);
        }

        #endregion

        #region "Virtual Raw Activate / Deactivate Raw"
        /// <summary>
        /// Bare minimum implementation for change the state of an entity instance
        /// </summary>
        /// <param name="entity"></param>
        protected virtual void ActiveDeactiveRaw(TBaseEntity entity)
        {
            this.ActiveDeactiveInternal(entity);
        }

        #endregion

        #region "Virtual Raw Get All Childs Raw"

        protected virtual IEnumerable<TBaseEntity> GetAllChildsRaw(Dictionary<string, object> parameters)
        {
            return this.GetAllChildsInternal(parameters);
        }

        #endregion
        
        #endregion
        
        #region "Entity Framework Private functions"

        private IEnumerable<TBaseEntity> GetAllEx()
        {
            IQueryable<TBaseEntity> query = this.Set<TBaseEntity>();
            return query.ToList();
        }

        private TBaseEntity GetByIdEx(TId id)
        {
            return this.Set<TBaseEntity>().Find(id);
        }

        private TId Insert(TBaseEntity entity)
        {
            this.Set<TBaseEntity>().Add(entity);
            this.SaveChanges();
            return GetId(entity);
        }

        private void Delete(TBaseEntity entityToDelete)
        {
            if (this.Entry(entityToDelete).State == EntityState.Detached)
            {
                this.Set<TBaseEntity>().Attach(entityToDelete);
            }
            this.Set<TBaseEntity>().Remove(entityToDelete);
            this.SaveChanges();
        }

        private void UpdateEx(TBaseEntity entityToUpdate)
        {
            this.Set<TBaseEntity>().Attach(entityToUpdate);
            this.Entry(entityToUpdate).State = EntityState.Modified;
            this.SaveChanges();
        }


        #endregion

        #region Private/ helper methods

        #region "Get All Childs Internal"

        public IEnumerable<TBaseEntity> GetAllChildsInternal(Dictionary<string, object> parameters)
        {
            try
            {
                const DatabaseAction Operation = DatabaseAction.GetChilds;
                var entity = Activator.CreateInstance<TBaseEntity>();
                if (null == entity)
                    return null;
                var storedProdecureName = this.GetSpName(Operation);
                if (string.IsNullOrWhiteSpace(storedProdecureName))
                {
                    return this.GetAllEx();
                }
                var param = parameters.ToDictionary(item => item.Key, item => 
                    new Tuple<object, ParameterDirection, bool>(item.Value, ParameterDirection.Input, false));
                var operationParameter = AddDatabaseOperationParameter(Operation);
                if (null != operationParameter)
                {
                    param.Add(
                        operationParameter.Item1,
                        new Tuple<object, ParameterDirection, bool>(
                            operationParameter.Item2,
                            ParameterDirection.Input,
                            false));
                }
                return DbHelper.ExecuteScalerList<TBaseEntity>(storedProdecureName, param);
            }
            catch (DbException e)
            {
                // Parse the exception
                if (DbHelper.HandleDbException(DatabaseAction.GetChilds, e))
                {
                    throw;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new CustomException("Server error", ex);
            }
        }

        #endregion

        #region "Get All and Complex Gel all Internal"

        private IEnumerable<TBaseEntity> GetAllInternalEx()
        {
            var entity = Activator.CreateInstance<TBaseEntity>();
            if (null == entity)
                return null;
            const DatabaseAction Operation = DatabaseAction.GetAll;
            var storedProdecureName = this.GetSpName(Operation);
            if (string.IsNullOrWhiteSpace(storedProdecureName))
            {
                return this.GetAllEx();
            }
            var param = this.SpParameters(Operation, entity)
                                 ?? new Dictionary<string, Tuple<object, ParameterDirection, bool>>();
            var operationParameter = AddDatabaseOperationParameter(Operation);
            if (null != operationParameter)
            {
                param.Add(
                    operationParameter.Item1,
                    new Tuple<object, ParameterDirection, bool>(
                        operationParameter.Item2,
                        ParameterDirection.Input,
                        false));
            }
            return DbHelper.ExecuteScalerList<TBaseEntity>(storedProdecureName, param);
        }

        private IEnumerable<TBaseEntity> GetAllInternal(Expression<Func<TBaseEntity, bool>> filter = null,
            Func<IQueryable<TBaseEntity>, IOrderedQueryable<TBaseEntity>> orderBy = null,
            string includeProperties = "")
        {
            try
            {
                var collection = GetAllInternalEx();

                if (filter != null)
                {
                    collection = collection.AsQueryable().Where(filter);
                }
                if (!string.IsNullOrWhiteSpace(includeProperties))
                {
                    collection = includeProperties.Split(new[] {','},
                        StringSplitOptions.RemoveEmptyEntries).Aggregate(collection,
                            (current, includeProperty) => current.AsQueryable().Include(includeProperty));
                }
                return orderBy != null ? orderBy(collection.AsQueryable()) : collection;
            }
            catch (DbException e)
            {
                // Parse the exception
                if (DbHelper.HandleDbException(DatabaseAction.GetAll, e))
                {
                    throw;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new CustomException("Server error",ex);
            }
        }

        #region Complex Get All

        private Tuple<List<TBaseEntity>, T1> GetAllComplexRawInternal<T1>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
        {
            var param = parameters.ToDictionary(item => item.Key, item =>
                   new Tuple<object, ParameterDirection, bool>(item.Value, ParameterDirection.Input, false));
            return DbHelper.ExecuteScalerComplexGetAllSingle<TBaseEntity, T1>(storedProcedureName, param);
        }

        private Tuple<List<TBaseEntity>, List<T1>> GetAllComplexListRawInternal<T1>(Dictionary<string, object> parameters, string storedProcedureName) where T1 : class
        {
            var param = parameters.ToDictionary(item => item.Key, item =>
                   new Tuple<object, ParameterDirection, bool>(item.Value, ParameterDirection.Input, false));
            return DbHelper.ExecuteScalerComplexGetAllSingleList<TBaseEntity, T1>(storedProcedureName, param);
        }


        private Tuple<List<TBaseEntity>, T1, T2> GetAllComplexRawInternal<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            var param = parameters.ToDictionary(item => item.Key, item =>
                   new Tuple<object, ParameterDirection, bool>(item.Value, ParameterDirection.Input, false));
            return DbHelper.ExecuteScalerComplexGetAllDouble<TBaseEntity, T1, T2> (storedProcedureName, param);
        }

        private Tuple<List<TBaseEntity>, List<T1>, List<T2>> GetAllComplexListRawInternal<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            var param = parameters.ToDictionary(item => item.Key, item =>
                   new Tuple<object, ParameterDirection, bool>(item.Value, ParameterDirection.Input, false));
            return DbHelper.ExecuteScalerComplexGetAllDoubleList<TBaseEntity, T1, T2>(storedProcedureName, param);
        }

        private Tuple<List<TBaseEntity>, T1, List<T2>> GetAllComplexSingleListRawInternal<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            var param = parameters.ToDictionary(item => item.Key, item =>
                   new Tuple<object, ParameterDirection, bool>(item.Value, ParameterDirection.Input, false));
            return DbHelper.ExecuteScalerComplexGetAllDoubleMixed<TBaseEntity, T1, T2>(storedProcedureName, param);
        }

        #endregion

        #endregion

        #region "Internal Get"

        private TBaseEntity GetInternal(TId id)
        {
            try
            {
                const DatabaseAction Operation = DatabaseAction.Get;
                var entity = Activator.CreateInstance<TBaseEntity>();
                SetId(entity, id);
                var storedProdecureName = this.GetSpName(Operation);
                if (string.IsNullOrWhiteSpace(storedProdecureName))
                {
                    return this.GetByIdEx(id);
                }
                var param = this.SpParameters(Operation, entity)
                                ?? new Dictionary<string, Tuple<object, ParameterDirection, bool>>();
                var operationParameter = AddDatabaseOperationParameter(Operation);
                if (null != operationParameter)
                {
                    param.Add(
                        operationParameter.Item1,
                        new Tuple<object, ParameterDirection, bool>(
                            operationParameter.Item2,
                            ParameterDirection.Input,
                            false));
                }
                return DbHelper.ExecuteScalerSingle<TBaseEntity>(storedProdecureName, param);
            }
            catch (DbException e)
            {
                // Parse the exception
                if (DbHelper.HandleDbException(DatabaseAction.Get, e))
                {
                    throw;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new CustomException("Server error", ex);
            }
        }


        #region Complex Get

        private Tuple<TBaseEntity, T1> GetComplexRawInternal<T1>(Dictionary<string, object> parameters, string storedProcedureName) where T1 : class
        {
            var param = parameters.ToDictionary(item => item.Key, item =>
                   new Tuple<object, ParameterDirection, bool>(item.Value, ParameterDirection.Input, false));
            return DbHelper.ExecuteScalerComplexSingle<TBaseEntity, T1>(storedProcedureName, param);
        }

        private Tuple<TBaseEntity, T1,T2> GetComplexRawInternal<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName) where T1 : class where T2:class 
        {
            var param = parameters.ToDictionary(item => item.Key, item =>
                   new Tuple<object, ParameterDirection, bool>(item.Value, ParameterDirection.Input, false));
            return DbHelper.ExecuteScalerComplexSingle<TBaseEntity, T1, T2>(storedProcedureName, param);
        }

        private Tuple<TBaseEntity, List<T1>> GetComplexListRawInternal<T1>(Dictionary<string, object> parameters, string storedProcedureName) where T1 : class
        {
            var param = parameters.ToDictionary(item => item.Key, item =>
                   new Tuple<object, ParameterDirection, bool>(item.Value, ParameterDirection.Input, false));
            return DbHelper.ExecuteScalerComplexList<TBaseEntity, T1>(storedProcedureName, param);
        }

        private Tuple<TBaseEntity, List<T1>, List<T2>> GetComplexListRawInternal<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            var param = parameters.ToDictionary(item => item.Key, item =>
                   new Tuple<object, ParameterDirection, bool>(item.Value, ParameterDirection.Input, false));
            return DbHelper.ExecuteScalerComplexList<TBaseEntity, T1, T2>(storedProcedureName, param);
        }

        private Tuple<TBaseEntity, T1, List<T2>> GetComplexSingleListRawInternal<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class
        {
            var param = parameters.ToDictionary(item => item.Key, item =>
                   new Tuple<object, ParameterDirection, bool>(item.Value, ParameterDirection.Input, false));
            return DbHelper.ExecuteScalerComplexSingleList<TBaseEntity, T1, T2>(storedProcedureName, param);
        }

        #endregion

        #endregion

        #region "Internal Update"

        private void UpdateInternal(TBaseEntity entity, out int recordUpdated)
        {
            if (this.IsDuplicate(entity, ValidateAction.Update))
                throw new Exception("Duplicate Record");
            if (!this.IsValidData(entity, ValidateAction.Update))
                throw new Exception("Invalid data");
            UpdateInternalCommon(entity, out recordUpdated);
        }

        private void UpdateInternal(TBaseEntity entity, out List<string> errors, out int recordUpdated)
        {
            recordUpdated = 0;
            var i = 0;
            errors = new List<string>();
            if (this.IsDuplicate(entity, ValidateAction.Update, errors))
            {
                i = errors.Count;
                if (errors.Count == 0)
                {
                    throw new Exception("Duplicate Record");
                }
            }
            else if (this.IsValidData(entity, ValidateAction.Update, errors))
            {
                if (errors.Count - i <= 0)
                {
                    throw new Exception("Invalid data");
                }
            }
            else
            {
              
                UpdateInternalCommon(entity, out recordUpdated);
            }
        }

        private void UpdateInternalCommon(TBaseEntity entity, out int recordUpdated)
        {
            recordUpdated = 0;
            try
            {
                const DatabaseAction Operation = DatabaseAction.Update;
                var storedProdecureName = this.GetSpName(Operation);
                if (string.IsNullOrWhiteSpace(storedProdecureName))
                {
                    this.UpdateEx(entity);
                }
                else
                {
                    var param = this.SpParameters(Operation, entity)
                               ?? new Dictionary<string, Tuple<object, ParameterDirection, bool>>();
                    var operationParameter = AddDatabaseOperationParameter(Operation);
                    if (null != operationParameter)
                    {
                        param.Add(
                            operationParameter.Item1,
                            new Tuple<object, ParameterDirection, bool>(
                                operationParameter.Item2,
                                ParameterDirection.Input,
                                false));
                    }
                    
                    DbHelper.ExecuteNonQuery(storedProdecureName, param, out recordUpdated);
                }
            }
            catch (DbException e)
            {
                // Parse the exception
                if (DbHelper.HandleDbException(DatabaseAction.Update, e))
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new CustomException("Server error", ex);
            }
        }

        #endregion

        #region "Internal Remove"

        private void RemoveInternal(TBaseEntity entity, out int recordUpdated)
        {
            recordUpdated = 0;
            try
            {
                const DatabaseAction Operation = DatabaseAction.Delete;
                var storedProdecureName = this.GetSpName(Operation);
                if (string.IsNullOrWhiteSpace(storedProdecureName))
                {
                    this.Delete(entity);
                }
                else
                {
                    var param = this.SpParameters(Operation, entity)
                               ?? new Dictionary<string, Tuple<object, ParameterDirection, bool>>();
                    var operationParameter = AddDatabaseOperationParameter(Operation);
                    if (null != operationParameter)
                    {
                        param.Add(
                            operationParameter.Item1,
                            new Tuple<object, ParameterDirection, bool>(
                                operationParameter.Item2,
                                ParameterDirection.Input,
                                false));
                    }
                
                    DbHelper.ExecuteNonQuery(storedProdecureName, param, out recordUpdated);
                }
            }
            catch (DbException e)
            {
                // Parse the exception
                if (DbHelper.HandleDbException(DatabaseAction.Delete, e))
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new CustomException("Server error", ex);
            }
        }

        #endregion

        #region "Internal Activate / Deactivate"

        private void ActiveDeactiveInternal(TBaseEntity entity)
        {
            try
            {
                const DatabaseAction Operation = DatabaseAction.ActiveDeactive;
                var storedProdecureName = this.GetSpName(Operation);
                if (string.IsNullOrWhiteSpace(storedProdecureName))
                {
                    this.UpdateEx(entity);
                }
                else
                {
                    var param = this.SpParameters(Operation, entity)
                                ?? new Dictionary<string, Tuple<object, ParameterDirection, bool>>();
                    var operationParameter = AddDatabaseOperationParameter(Operation);
                    if (null != operationParameter)
                    {
                        param.Add(
                            operationParameter.Item1,
                            new Tuple<object, ParameterDirection, bool>(
                                operationParameter.Item2,
                                ParameterDirection.Input,
                                false));
                    }
                    int recordUpdated;
                    DbHelper.ExecuteNonQuery(storedProdecureName, param, out recordUpdated);
                }
            }
            catch (DbException e)
            {
                // Parse the exception
                if (DbHelper.HandleDbException(DatabaseAction.ActiveDeactive, e))
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new CustomException("Server error", ex);
            }
        }

        #endregion

        #region "Internal Add"

        private TId AddInternal(TBaseEntity entity, out int recordUpdated)
        {
            if (this.IsDuplicate(entity, ValidateAction.Insert))
                throw new Exception("Duplicate Record");
            if (!this.IsValidData(entity, ValidateAction.Insert))
                throw new Exception("Invalid data");
            return AddInternalCommon(entity, out recordUpdated);
        }

        private TId AddInternal(TBaseEntity entity, out List<string> errors, out int recordUpdated)
        {
            recordUpdated = 0;
            errors = new List<string>();
            var i = 0;
            if (this.IsDuplicate(entity, ValidateAction.Insert, errors))
            {
                i = errors.Count;
                if (errors.Count == 0)
                {
                    throw new Exception("Duplicate Record");
                }
            }
            else if (this.IsValidData(entity, ValidateAction.Insert, errors))
            {
                if (i - errors.Count <= 0)
                {
                    throw new Exception("Invalid data");
                }
            }
            else
            {
                return AddInternalCommon(entity, out recordUpdated);
            }
            return this.GetId(entity);
        }

        private TId AddInternalCommon(TBaseEntity entity, out int recordUpdated)
        {
            recordUpdated = 0;
            try
            {
                const DatabaseAction Operation = DatabaseAction.Insert;
                var storedProdecureName = this.GetSpName(Operation);
                if (string.IsNullOrWhiteSpace(storedProdecureName))
                {
                    return this.Insert(entity);
                }
                var param = this.SpParameters(Operation, entity)
                            ?? new Dictionary<string, Tuple<object, ParameterDirection, bool>>();
                var operationParameter = AddDatabaseOperationParameter(Operation);
                if (null != operationParameter)
                {
                    param.Add(
                        operationParameter.Item1,
                        new Tuple<object, ParameterDirection, bool>(
                            operationParameter.Item2,
                            ParameterDirection.Input,
                            false));
                }
                var id = DbHelper.ExecuteNonQueryOnlyId(storedProdecureName, param, out recordUpdated);
                if (null != id)
                {
                    return (TId)id;
                }
                return this.GetId(entity);
            }
            catch (DbException e)
            {
                // Parse the exception
                if (DbHelper.HandleDbException(DatabaseAction.Insert, e))
                {
                    throw;
                }
                return default(TId);
            }
            catch (Exception ex)
            {
                throw new CustomException("Server error", ex);
            }
        }



        #endregion
        #endregion
    }
}
