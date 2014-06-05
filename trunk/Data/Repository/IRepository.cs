using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Data.Repository
{
    public interface IRepository<TBaseEntity, TId> where TBaseEntity : class
    {
        #region "Get all Childs "

        /// <summary>
        /// Gets the list of data for a given parent id
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        IEnumerable<TBaseEntity> GetAllChilds(Dictionary<string, object> parameters);

        #endregion

        #region Get All And Complex Get all

        /// <summary>
        /// Get Filtered records
        /// </summary>
        /// <param name="filter">Where Filter optional</param>
        /// <param name="orderBy">Order by optional</param>
        /// <param name="includeProperties"></param>
        /// <returns></returns>
        IEnumerable<TBaseEntity> GetAll(Expression<Func<TBaseEntity, bool>> filter = null,
            Func<IQueryable<TBaseEntity>, IOrderedQueryable<TBaseEntity>> orderBy = null,
            string includeProperties = "");

        Tuple<List<TBaseEntity>, T1> GetAllComplex<T1>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class;

        Tuple<List<TBaseEntity>, T1, T2> GetAllComplex<T1, T2>(
            Dictionary<string, object> parameters,
            string storedProcedureName) where T1 : class where T2 : class;

        Tuple<List<TBaseEntity>, List<T1>> GetAllComplexList<T1>(
            Dictionary<string, object> parameters,
            string storedProcedureName) where T1 : class;

        Tuple<List<TBaseEntity>, List<T1>, List<T2>> GetAllComplexList<T1, T2>(
            Dictionary<string, object> parameters,
            string storedProcedureName) where T1 : class where T2 : class ;

        Tuple<List<TBaseEntity>, T1, List<T2>> GetAllComplexSingleList<T1, T2>(
            Dictionary<string, object> parameters,
            string storedProcedureName) where T1 : class where T2 : class;

        #endregion

        #region "Get and Complex Get"
        /// <summary>
        /// Get the record by ID
        /// </summary>
        /// <param name="id">primary key</param>
        /// <returns></returns>
        TBaseEntity Get(TId id);
        Tuple<TBaseEntity, T1> GetComplex<T1>(Dictionary<string, object> parameters, string storedProcedureName) where T1 : class;

        Tuple<TBaseEntity, T1, T2> GetComplex<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class;

        Tuple<TBaseEntity, List<T1>> GetComplexList<T1>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class;

        Tuple<TBaseEntity, List<T1>, List<T2>> GetComplexList<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class;

        Tuple<TBaseEntity, T1, List<T2>> GetComplexSingleList<T1, T2>(Dictionary<string, object> parameters, string storedProcedureName)
            where T1 : class
            where T2 : class;

        #endregion

        #region "Add"

        /// <summary>
        /// Inserts a new entity into the repository, out number of records updated
        /// </summary>
        /// <param name="entity">Entity to insert.</param>
        /// <param name="recordUpdated"></param>
        TId Add(TBaseEntity entity, out int recordUpdated);

        /// <summary>
        /// Inserts a new entity into the repository
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        TId Add(TBaseEntity entity);

        /// <summary>
        /// Inserts a new entity into the repository, and out the error collection
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="errors">List of error or validation messages</param>
        /// <param name="recordUpdated"></param>
        /// <returns></returns>
        TId Add(TBaseEntity entity, out List<string> errors, out int recordUpdated);

        #endregion

        #region "Update"
        /// <summary>
        /// Updates an existing entity in the repository, out number of records uopdated 
        /// </summary>
        /// <param name="entity">Entity to update.</param>
        /// <param name="recordUpdated"></param>
        void Update(TBaseEntity entity, out int recordUpdated);

        /// <summary>
        /// Updates an existing entity in the repository.
        /// </summary>
        /// <param name="entity"></param>
        void Update(TBaseEntity entity);

        /// <summary>
        /// Updates an existing entity in the repository, and out the error collection
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="errors">List of error or validation messages</param>
        /// <param name="recordUpdated"></param>
        void Update(TBaseEntity entity, out List<string> errors, out int recordUpdated);

        #endregion

        #region "Remove"
        /// <summary>
        /// Removes an entity from the repository, out number of records updated 
        /// </summary>
        /// <param name="entity">Entity to remove.</param>
        /// <param name="recordUpdated"></param>
        void Remove(TBaseEntity entity, out int recordUpdated);

        /// <summary>
        /// Removes an entity from the repository
        /// </summary>
        /// <param name="entity"></param>
        void Remove(TBaseEntity entity);
        
        #endregion

        #region "Activate / Deactivate"
        /// <summary>
        /// Disabled/Enabled an entity item in the repository.
        /// </summary>
        /// <returns>False if the entity does not exist in the repository, or true if successfully deactivate.</returns>
        void ActiveDeactive(TBaseEntity entity);

        #endregion

    }
}
