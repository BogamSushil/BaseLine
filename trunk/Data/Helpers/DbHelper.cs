using Data.DataModel;
using Data.Repository;
using Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace Data.Helpers
{
    public static class DbHelper
    {
        #region "Parsing Database Error Information"

        /// <summary>
        /// A single value mapped from Db Exception by ParseDbException method
        /// </summary>
        public enum DbError
        {
            /// <summary>
            /// Unknown database error
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// Error initiated by application code - user message id will
            /// be returned
            /// </summary>
            Application = 1,

            /// <summary>
            /// Error due to Foreign Key Constraint Violation
            /// </summary>
            ForeignKeyViolation = 2,

            /// <summary>
            /// Error due to Primary Key Constraint Violation
            /// </summary>
            PrimaryKeyViolation = 3,

            /// <summary>
            /// Error due to Unique Key Constraint Violation
            /// </summary>
            UniqueKeyViolation = 4
        }

        /// <summary>
        /// Analyzes the given DB Exception for retrieving more information
        /// about the error. 
        /// </summary>
        /// <param name="execption"></param>
        /// <param name="messageId">User Message Id retrieved from Db Exception.
        /// Applicable only when return value is DbError.Application</param>
        public static DbError ParseDbException(DbException execption, out int messageId)
        {
            const string MessageIdKey1 = "[**WINDSTAR_MESSAGE:";
            const string MessageIdKey2 = "**]";

            messageId = 0;
            // Search for special keys within the exception message
            var message = execption.Message;
            var key1Index = message.IndexOf(MessageIdKey1, StringComparison.Ordinal);
            if (key1Index >= 0)
            {
                var start = key1Index + MessageIdKey1.Length;
                var end = message.IndexOf(MessageIdKey2, start, StringComparison.Ordinal);
                if (end >= 0)
                {
                    // Found, parse the message id
                    if (int.TryParse(message.Substring(start, end - start), out messageId))
                    {
                        return DbError.Application;
                    }
                }
            }

            // Handle Sql Server specific error code
            var sqlExecption = execption as SqlException;
            if (null == sqlExecption)
            {
                return DbError.Unknown;
            }
            bool isConstraintViolation = false;
            if (sqlExecption.Number == 547)
            {
                isConstraintViolation = true;
            }
            else if (sqlExecption.Number == 50000
                     && (message.IndexOf("Error Number: 547", StringComparison.Ordinal) >= 0
                         || message.IndexOf("Error Number: 2627", StringComparison.Ordinal) >= 0))
            {
                // Our Common logging sp converts sql errors into user
                // errors and original sql error can be found in message
                // string - so here we have again found constraint 
                // violation error.
                isConstraintViolation = true;
            }
            // In case of constraint violation, see if its foreign key or
            // unique key or primary key
            if (!isConstraintViolation)
            {
                return DbError.Unknown;
            }
            if (message.IndexOf("REFERENCE", StringComparison.Ordinal) >= 0)
            {
                return DbError.ForeignKeyViolation;
            }
            if (message.IndexOf("PRIMARY KEY", StringComparison.Ordinal) >= 0)
            {
                return DbError.PrimaryKeyViolation;
            }
            if (message.IndexOf("UNIQUE KEY", StringComparison.Ordinal) >= 0)
            {
                return DbError.UniqueKeyViolation;
            }
            return DbError.Unknown;
        }

        /// <summary>
        /// Attempts to map the generic database exception to specific 
        /// application exception. Returns true if existing exception to
        /// be re-thrown.
        /// </summary>
        public static bool HandleDbException(DatabaseAction operation, DbException e)
        {
            // Parse exception
            int messageId;
            switch (ParseDbException(e, out messageId))
            {
                case DbError.Application:
                    throw new CustomException("Application error", e);

                case DbError.ForeignKeyViolation:
                    if (operation == DatabaseAction.Delete)
                    {
                        throw new CommonDbException(CustomException.MESSAGE_DELETE_DATA_IN_USE);
                    }
                    break;
                case DbError.UniqueKeyViolation:
                case DbError.PrimaryKeyViolation:
                    if (operation == DatabaseAction.Insert || operation == DatabaseAction.Update)
                    {
                        throw new CommonDbException(CustomException.MESSAGE_DUPLICATE_DATA);
                    }
                    break;
            }
            return true;
        }

        #endregion

        #region "DB Command Execute"

        #region "ExecuteNonQuery"

        public static Dictionary<string, object> ExecuteNonQuery(
          string storedProcedureName,
          Dictionary<string, Tuple<object, ParameterDirection, bool>> parameters,
          out int recordUpdated)
        {
            using (var context = new DataEntities())
            {
                var connection = context.Database.Connection;
                using (connection)
                {
                    connection.Open();
                    // Create the command that we are going to be sending
                    using (var command = (SqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = storedProcedureName;
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (var param in parameters)
                        {
                            var p = command.Parameters.AddWithValue(param.Key, param.Value.Item1);
                            p.Direction = param.Value.Item2;
                        }
                        recordUpdated = command.ExecuteNonQuery();
                        return
                            parameters.Where(f => f.Value.Item2 == ParameterDirection.InputOutput)
                                .ToDictionary(item => item.Key, item => command.Parameters[item.Key].Value);
                    }
                }
            }
        }


        public static object ExecuteNonQueryOnlyId(
         string storedProcedureName,
         Dictionary<string, Tuple<object, ParameterDirection, bool>> parameters,
         out int recordUpdated)
        {
            using (var context = new DataEntities())
            {
                var connection = context.Database.Connection;
                using (connection)
                {
                    connection.Open();
                    // Create the command that we are going to be sending
                    using (var command = (SqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = storedProcedureName;
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (var param in parameters)
                        {
                            var p = command.Parameters.AddWithValue(param.Key, param.Value.Item1);
                            p.Direction = param.Value.Item2;
                        }
                        recordUpdated = command.ExecuteNonQuery();
                        return
                            parameters.Where(f => f.Value.Item2 == ParameterDirection.InputOutput && f.Value.Item3)
                                .Select(f => command.Parameters[f.Key].Value)
                                .FirstOrDefault();
                    }
                }
            }
        }


        #endregion

        #region "Get and Complex Db Methods"
        
        public static T ExecuteScalerSingle<T>(
            string storedProcedureName,
            Dictionary<string, Tuple<object, ParameterDirection, bool>> parameters) where T : class
        {
            using (var context = new DataEntities())
            {
                var connection = context.Database.Connection;
                using (connection)
                {
                    connection.Open();
                    // Create the command that we are going to be sending
                    using (var command = (SqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = storedProcedureName;
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (var param in parameters)
                        {
                            var p = command.Parameters.AddWithValue(param.Key, param.Value.Item1);
                            p.Direction = param.Value.Item2;
                        }
                        using (IDataReader dataReader = command.ExecuteReader())
                        {
                            var dataBaseContext = ((IObjectContextAdapter)context).ObjectContext;
                            var data = dataBaseContext.Translate<T>(dataReader as DbDataReader).FirstOrDefault();
                            return data;
                        }
                    }
                }
            }
        }


        public static Tuple<T, T1> ExecuteScalerComplexSingle<T, T1>(
            string storedProcedureName,
            Dictionary<string, Tuple<object, ParameterDirection, bool>> parameters) where T : class where T1 : class
        {
            using (var context = new DataEntities())
            {
                var connection = context.Database.Connection;
                using (connection)
                {
                    connection.Open();
                    // Create the command that we are going to be sending
                    using (var command = (SqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = storedProcedureName;
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (var param in parameters)
                        {
                            var p = command.Parameters.AddWithValue(param.Key, param.Value.Item1);
                            p.Direction = param.Value.Item2;
                        }
                        using (IDataReader dataReader = command.ExecuteReader())
                        {
                            var dataBaseContext = ((IObjectContextAdapter)context).ObjectContext;
                            var data = dataBaseContext.Translate<T>(dataReader as DbDataReader).FirstOrDefault();


                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem1 = dataBaseContext.Translate<T1>(dataReader as DbDataReader).FirstOrDefault<T1>();

                            return new Tuple<T, T1>(data, tem1);
                        }
                    }
                }
            }
        }


        public static Tuple<T, T1, List<T2>> ExecuteScalerComplexSingleList<T, T1, T2>(
            string storedProcedureName,
            Dictionary<string, Tuple<object, ParameterDirection, bool>> parameters) where T : class where T1 : class
            where T2 : class
        {
            using (var context = new DataEntities())
            {
                var connection = context.Database.Connection;
                using (connection)
                {
                    connection.Open();
                    // Create the command that we are going to be sending
                    using (var command = (SqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = storedProcedureName;
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (var param in parameters)
                        {
                            var p = command.Parameters.AddWithValue(param.Key, param.Value.Item1);
                            p.Direction = param.Value.Item2;
                        }
                        using (IDataReader dataReader = command.ExecuteReader())
                        {
                            var dataBaseContext = ((IObjectContextAdapter)context).ObjectContext;
                            var data = dataBaseContext.Translate<T>(dataReader as DbDataReader).FirstOrDefault();


                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem1 = dataBaseContext.Translate<T1>(dataReader as DbDataReader).SingleOrDefault<T1>();
                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem2 = dataBaseContext.Translate<T2>(dataReader as DbDataReader).ToList<T2>();

                            return new Tuple<T, T1, List<T2>>(data, tem1, tem2);
                        }
                    }
                }
            }
        }

        public static Tuple<T, T1, T2> ExecuteScalerComplexSingle<T, T1, T2>(
            string storedProcedureName,
            Dictionary<string, Tuple<object, ParameterDirection, bool>> parameters) where T : class where T1 : class
            where T2 : class
        {
            using (var context = new DataEntities())
            {
                var connection = context.Database.Connection;
                using (connection)
                {
                    connection.Open();
                    // Create the command that we are going to be sending
                    using (var command = (SqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = storedProcedureName;
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (var param in parameters)
                        {
                            var p = command.Parameters.AddWithValue(param.Key, param.Value.Item1);
                            p.Direction = param.Value.Item2;
                        }
                        using (IDataReader dataReader = command.ExecuteReader())
                        {
                            var dataBaseContext = ((IObjectContextAdapter)context).ObjectContext;
                            var data = dataBaseContext.Translate<T>(dataReader as DbDataReader).FirstOrDefault();


                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem1 = dataBaseContext.Translate<T1>(dataReader as DbDataReader).SingleOrDefault<T1>();
                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem2 = dataBaseContext.Translate<T2>(dataReader as DbDataReader).SingleOrDefault<T2>();

                            return new Tuple<T, T1, T2>(data, tem1, tem2);
                        }
                    }
                }
            }
        }

        public static Tuple<T, List<T1>, List<T2>> ExecuteScalerComplexList<T, T1, T2>(
            string storedProcedureName,
            Dictionary<string, Tuple<object, ParameterDirection, bool>> parameters) where T : class where T1 : class
            where T2 : class
        {
            using (var context = new DataEntities())
            {
                var connection = context.Database.Connection;
                using (connection)
                {
                    connection.Open();
                    // Create the command that we are going to be sending
                    using (var command = (SqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = storedProcedureName;
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (var param in parameters)
                        {
                            var p = command.Parameters.AddWithValue(param.Key, param.Value.Item1);
                            p.Direction = param.Value.Item2;
                        }
                        using (IDataReader dataReader = command.ExecuteReader())
                        {
                            var dataBaseContext = ((IObjectContextAdapter)context).ObjectContext;
                            var data = dataBaseContext.Translate<T>(dataReader as DbDataReader).FirstOrDefault();


                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem1 = dataBaseContext.Translate<T1>(dataReader as DbDataReader).ToList<T1>();
                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem2 = dataBaseContext.Translate<T2>(dataReader as DbDataReader).ToList<T2>();

                            return new Tuple<T, List<T1>, List<T2>>(data, tem1, tem2);
                        }
                    }
                }
            }
        }

        public static Tuple<T, List<T1>> ExecuteScalerComplexList<T, T1>(
            string storedProcedureName,
            Dictionary<string, Tuple<object, ParameterDirection, bool>> parameters) where T : class where T1 : class
        {
            using (var context = new DataEntities())
            {
                var connection = context.Database.Connection;
                using (connection)
                {
                    connection.Open();
                    // Create the command that we are going to be sending
                    using (var command = (SqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = storedProcedureName;
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (var param in parameters)
                        {
                            var p = command.Parameters.AddWithValue(param.Key, param.Value.Item1);
                            p.Direction = param.Value.Item2;
                        }
                        using (IDataReader dataReader = command.ExecuteReader())
                        {
                            var dataBaseContext = ((IObjectContextAdapter)context).ObjectContext;
                            var data = dataBaseContext.Translate<T>(dataReader as DbDataReader).FirstOrDefault();


                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem1 = dataBaseContext.Translate<T1>(dataReader as DbDataReader).ToList<T1>();

                            return new Tuple<T, List<T1>>(data, tem1);
                        }
                    }
                }
            }
        }

        public static List<T> ExecuteScalerList<T>(
            string storedProcedureName,
            Dictionary<string, Tuple<object, ParameterDirection, bool>> parameters) where T : class
        {
            using (var context = new DataEntities())
            {
                var connection = context.Database.Connection;
                using (connection)
                {
                    connection.Open();
                    // Create the command that we are going to be sending
                    using (var command = (SqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = storedProcedureName;
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (var param in parameters)
                        {
                            var p = command.Parameters.AddWithValue(param.Key, param.Value.Item1);
                            p.Direction = param.Value.Item2;
                        }
                        using (IDataReader dataReader = command.ExecuteReader())
                        {
                            var dataBaseContext = ((IObjectContextAdapter)context).ObjectContext;
                            var data = dataBaseContext.Translate<T>(dataReader as DbDataReader).ToList();
                            return data;
                        }
                    }
                }
            }
        }
      
        #endregion

        #region "Get All and complex Db methods"

        public static Tuple<List<T>, T1> ExecuteScalerComplexGetAllSingle<T, T1>(
            string storedProcedureName,
            Dictionary<string, Tuple<object, ParameterDirection, bool>> parameters)
            where T : class
            where T1 : class
        {
            using (var context = new DataEntities())
            {
                var connection = context.Database.Connection;
                using (connection)
                {
                    connection.Open();
                    // Create the command that we are going to be sending
                    using (var command = (SqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = storedProcedureName;
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (var param in parameters)
                        {
                            var p = command.Parameters.AddWithValue(param.Key, param.Value.Item1);
                            p.Direction = param.Value.Item2;
                        }
                        using (IDataReader dataReader = command.ExecuteReader())
                        {
                            var dataBaseContext = ((IObjectContextAdapter)context).ObjectContext;
                            var data = dataBaseContext.Translate<T>(dataReader as DbDataReader).ToList();


                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem1 = dataBaseContext.Translate<T1>(dataReader as DbDataReader).FirstOrDefault<T1>();

                            return new Tuple<List<T>, T1>(data, tem1);
                        }
                    }
                }
            }
        }


        public static Tuple<List<T>, List<T1>> ExecuteScalerComplexGetAllSingleList<T, T1>(
          string storedProcedureName,
          Dictionary<string, Tuple<object, ParameterDirection, bool>> parameters)
            where T : class
            where T1 : class
        {
            using (var context = new DataEntities())
            {
                var connection = context.Database.Connection;
                using (connection)
                {
                    connection.Open();
                    // Create the command that we are going to be sending
                    using (var command = (SqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = storedProcedureName;
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (var param in parameters)
                        {
                            var p = command.Parameters.AddWithValue(param.Key, param.Value.Item1);
                            p.Direction = param.Value.Item2;
                        }
                        using (IDataReader dataReader = command.ExecuteReader())
                        {
                            var dataBaseContext = ((IObjectContextAdapter)context).ObjectContext;
                            var data = dataBaseContext.Translate<T>(dataReader as DbDataReader).ToList();


                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem1 = dataBaseContext.Translate<T1>(dataReader as DbDataReader).ToList<T1>();

                            return new Tuple<List<T>, List<T1>>(data, tem1);
                        }
                    }
                }
            }
        }

        public static Tuple<List<T>, T1, List<T2>> ExecuteScalerComplexGetAllDoubleMixed<T, T1, T2>(
            string storedProcedureName,
            Dictionary<string, Tuple<object, ParameterDirection, bool>> parameters)
            where T : class
            where T1 : class
            where T2 : class
        {
            using (var context = new DataEntities())
            {
                var connection = context.Database.Connection;
                using (connection)
                {
                    connection.Open();
                    // Create the command that we are going to be sending
                    using (var command = (SqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = storedProcedureName;
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (var param in parameters)
                        {
                            var p = command.Parameters.AddWithValue(param.Key, param.Value.Item1);
                            p.Direction = param.Value.Item2;
                        }
                        using (IDataReader dataReader = command.ExecuteReader())
                        {
                            var dataBaseContext = ((IObjectContextAdapter)context).ObjectContext;
                            var data = dataBaseContext.Translate<T>(dataReader as DbDataReader).ToList();


                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem1 = dataBaseContext.Translate<T1>(dataReader as DbDataReader).SingleOrDefault<T1>();
                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem2 = dataBaseContext.Translate<T2>(dataReader as DbDataReader).ToList<T2>();

                            return new Tuple<List<T>, T1, List<T2>>(data, tem1, tem2);
                        }
                    }
                }
            }
        }

        public static Tuple<List<T>, T1, T2> ExecuteScalerComplexGetAllDouble<T, T1, T2>(
            string storedProcedureName,
            Dictionary<string, Tuple<object, ParameterDirection, bool>> parameters)
            where T : class
            where T1 : class
            where T2 : class
        {
            using (var context = new DataEntities())
            {
                var connection = context.Database.Connection;
                using (connection)
                {
                    connection.Open();
                    // Create the command that we are going to be sending
                    using (var command = (SqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = storedProcedureName;
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (var param in parameters)
                        {
                            var p = command.Parameters.AddWithValue(param.Key, param.Value.Item1);
                            p.Direction = param.Value.Item2;
                        }
                        using (IDataReader dataReader = command.ExecuteReader())
                        {
                            var dataBaseContext = ((IObjectContextAdapter)context).ObjectContext;
                            var data = dataBaseContext.Translate<T>(dataReader as DbDataReader).ToList();


                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem1 = dataBaseContext.Translate<T1>(dataReader as DbDataReader).SingleOrDefault<T1>();
                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem2 = dataBaseContext.Translate<T2>(dataReader as DbDataReader).SingleOrDefault<T2>();

                            return new Tuple<List<T>, T1, T2>(data, tem1, tem2);
                        }
                    }
                }
            }
        }

        public static Tuple<List<T>, List<T1>, List<T2>> ExecuteScalerComplexGetAllDoubleList<T, T1, T2>(
            string storedProcedureName,
            Dictionary<string, Tuple<object, ParameterDirection, bool>> parameters)
            where T : class
            where T1 : class
            where T2 : class
        {
            using (var context = new DataEntities())
            {
                var connection = context.Database.Connection;
                using (connection)
                {
                    connection.Open();
                    // Create the command that we are going to be sending
                    using (var command = (SqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = storedProcedureName;
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (var param in parameters)
                        {
                            var p = command.Parameters.AddWithValue(param.Key, param.Value.Item1);
                            p.Direction = param.Value.Item2;
                        }
                        using (IDataReader dataReader = command.ExecuteReader())
                        {
                            var dataBaseContext = ((IObjectContextAdapter)context).ObjectContext;
                            var data = dataBaseContext.Translate<T>(dataReader as DbDataReader).ToList();


                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem1 = dataBaseContext.Translate<T1>(dataReader as DbDataReader).ToList<T1>();
                            // 7. Advance to the next result sets
                            dataReader.NextResult();
                            var tem2 = dataBaseContext.Translate<T2>(dataReader as DbDataReader).ToList<T2>();

                            return new Tuple<List<T>, List<T1>, List<T2>>(data, tem1, tem2);
                        }
                    }
                }
            }
        }

      
        #endregion

        #endregion
    }
}
