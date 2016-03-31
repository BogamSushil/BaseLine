#region "File Description & Edit History"
//	Author: vinayc
//	Date Created: 02/Jan/09
//	Description: 
//		Helper class for executing database queries/statements.
//
//	Change History:
//	---------------------------------------------------------------------------------
//	Date		User		Description
//	----------	----------	---------------------------------------------------------
//	02/Jan/09	vinayc		Created this file.
//	04/May/09	vinayc		Modified to ensure that procedure name will always be
//							schema qualified (dbo.<proc name>).
//	07/May/09	vinayc		Added LoadMetadataDataSet & ExecuteMetadataNonQuery 
//							methods for querying the metadata database.
//  11/May/09   vinayc      Fixed slight bug in above methods where command used was
//                          created on main database instead of metadata database.
//  12/May/09   vinayc      Added ParseDbException method to make sense of database
//                          exception (map into application exception if possible).
//	13/May/09	vinayc		Added CreateParameter overload that takes Metadata 
//							DataType as parameter.
//	01/Jun/09	vinayc		Added HandleDbException method for common database
//							exception handling.
//	08/Jul/09	amolm		Modified ParseDbException()
//	16/Jul/09	vinayc		Fixed bugs in parsing database exceptions for unique
//							key constraint violation.
//	10/Aug/09	vinayc		Added GetDbExpressionForLeft and 
//							GetDbExpressionForSubString for generating database 
//							expressions for left and substring functions.
//	04/Sep/09	vinayc		Added ParseTimeStampInfo helper method to read time-stamp
//							information.
//	08/Sep/09	vinayc		Modified method signatures for reading data from row so
//							that they will become extension methods for data row.
//	06/Nov/09	vinayc		Updated documentation for GetDbExpressionForSubString 
//							to explain how start parameter will be interpreted.
//							Corrected the implementation of the same accordingly. 
//	07/Dec/09	vinayc		Added GetDatabaseInfo method for getting database 
//							information.
//	05/01/10	vinayc		Added RegisterTransactionCallback for registering 
//							callback for transaction completion.
//	---------------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using EntLib = Microsoft.Practices.EnterpriseLibrary;
using Microsoft.Practices.EnterpriseLibrary.Data;
using CallContext = System.Runtime.Remoting.Messaging.CallContext;
using System.Data;
using System.Data.Common;
using System.Transactions;
using LnF.WindStar.Core.Metadata;
using LnF.WindStar.Core;
using LnF.WindStar.Core.Common;

namespace LnF.WindStar.Dal
{
	/// <summary>
	/// Helper class for database access. Current implementation is a wrapper 
	/// over Entrprise Library DAAB 3.1
	/// </summary>
	/// <remarks>
	/// The class serves many purposes - 
	/// 1.	It provides similar interface to SqlHelper (from DAAB Version 1.0); 
	///		Hopefully, this will reduce learning curve.
	///	2.	It eliminates DAAB methods that provides passing explicit transaction
	///		as we will be using TransactionScope for transaction propogation across
	///		various methods/layers.
	///	3.	It encapsulate DAAB database object creation - although, DAAB privides 
	///		a factory for the purpose; it works based on DAAB configuration elements
	///		and we may have to choose it based on application specific configuration
	///		elements.
	///	4.	Some additional tweecking can be done here such as setting up the
	///		command time-out.
	///	5.	Needless to say, it gives us capability to replace DAAB with another
	///		implemnetation if need arises.
	/// </remarks>
	public static class DbHelper
	{
		#region "Related to Database object creation"

		private static Database GetDatabase()
		{
			// We will store the database object in the call context to save
			// the construction time.
			const string KEY = "winstar_database";
			// See in the current call context
			var database = CallContext.GetData(KEY) as Database;
			if (null == database)
			{
				// Create the database
				database = CreateDatabase(
					Core.AppContext.Current.Config.Database.MainConnectionString);
				// Store in the call context
				CallContext.SetData(KEY, database);
			}
			return database;
		}

		private static Database GetMetadataDatabase()
		{
			return CreateDatabase(
				Core.AppContext.Current.Config.Database.MetadataConnectionString);
		}

		private static Database CreateDatabase(string connectionString)
		{
			// Create the database - we will be using Sql Server
			return new EntLib.Data.Sql.SqlDatabase(connectionString);
		}

		#endregion

		#region "Database Information"

		/// <summary>
		/// Returns the application/metadata database information such as 
		/// server name/database name.
		/// </summary>
		public static string GetDatabaseInfo(bool forMetadata)
		{
			return GetDatabaseInfo(forMetadata ?
				Core.AppContext.Current.Config.Database.MetadataConnectionString :
				Core.AppContext.Current.Config.Database.MainConnectionString);
		}

		private static string GetDatabaseInfo(string connectionString)
		{
			// ** Sql-server specific implementation **
			var builder = new System.Data.SqlClient.SqlConnectionStringBuilder(
				connectionString);
			return string.Format("[{0}, {1}]", builder.DataSource,
				builder.InitialCatalog);
		}

		#endregion

		#region "Transactions related"

		#region "TransactionScope Creation"

		public static TransactionScope CreateTransactionScope()
		{
			return CreateTransactionScope(TransactionScopeOption.Required);
		}

		public static TransactionScope CreateTransactionScope(
			TransactionScopeOption option)
		{
			// TODO Set transaction time-out
			return new TransactionScope(option);
		}

		public static TransactionScope CreateTransactionScope(
			TransactionScopeOption option, System.Transactions.IsolationLevel level)
		{
			var transactionOptions = new TransactionOptions();
			transactionOptions.IsolationLevel = level;
			// TODO Set transaction time-out
			return new TransactionScope(option, transactionOptions);
		}

		#endregion

		/// <summary>
		/// Registers a callback when the current transaction completes. The callback
		/// will be invoked with true/false value indicating whether the transation
		/// was committed or rolled back. 
		/// </summary>
		/// <returns>
		/// Returns false if there is no current transaction.
		/// </returns>
		public static bool RegisterTransactionCallback(Utility.Method<bool> callback)
		{
			if (null == callback)
			{
				throw new ArgumentNullException();
			}
			var current = Transaction.Current;
			if (null == current)
			{
				return false;
			}
			current.TransactionCompleted +=
				delegate(object sender, TransactionEventArgs e)
				{
					callback(e.Transaction.TransactionInformation.Status ==
						TransactionStatus.Committed);
				};
			return true;
		}

		#endregion

		#region "Parameter Creation"

		/// <summary>
		/// Creates and returns in parameter with the given name and value.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateParameter(string name, object value)
		{
			var param = CreateParameter(name);
			param.Direction = ParameterDirection.Input;
			param.Value = null != value ? value : DBNull.Value;
			return param;
		}

		/// <summary>
		/// Creates and returns in parameter of integer type with the given 
		/// name and value.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateParameter(string name, int value)
		{
			return CreateParameter(name, DbType.Int32, value);
		}

		/// <summary>
		/// Creates and returns in parameter of integer type with the given 
		/// name and nullable value.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateParameter(string name, int? value)
		{
			return CreateParameter(name, DbType.Int32,
				value.HasValue ? (object)value.Value : null);
		}

		/// <summary>
		/// Creates and returns in parameter of long type with the given 
		/// name and value.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateParameter(string name, long value)
		{
			return CreateParameter(name, DbType.Int64, value);
		}

		/// <summary>
		/// Creates and returns in parameter of long type with the given 
		/// name and nullable value.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateParameter(string name, long? value)
		{
			return CreateParameter(name, DbType.Int64,
				value.HasValue ? (object)value.Value : null);
		}

		/// <summary>
		/// Creates and returns in parameter of integer type with the given 
		/// name and value.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateParameter(string name, bool value)
		{
			return CreateParameter(name, DbType.Boolean, value);
		}

		/// <summary>
		/// Creates and returns in parameter of integer type with the given 
		/// name and nullable value.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateParameter(string name, bool? value)
		{
			return CreateParameter(name, DbType.Boolean,
				value.HasValue ? (object)value.Value : null);
		}

		/// <summary>
		/// Creates and returns in parameter of date/time type with the given 
		/// name and value.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateParameter(string name, DateTime value)
		{
			return CreateParameter(name, DbType.DateTime, value);
		}

		/// <summary>
		/// Creates and returns in parameter of date/time type with the given 
		/// name and nullable value.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateParameter(string name, DateTime? value)
		{
			return CreateParameter(name, DbType.DateTime,
				value.HasValue ? (object)value.Value : null);
		}

		/// <summary>
		/// Creates and returns in parameter of string type with the given 
		/// name and (nullable) value.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateParameter(string name, string value)
		{
			return CreateParameter(name, DbType.String,
				string.IsNullOrEmpty(value) ? null : value);
		}

		/// <summary>
		/// Creates and returns in parameter with the given name and data type.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateParameter(string name,
			DataType type)
		{
			DbType dbType;
			switch (type)
			{
				case DataType.Integer:
					dbType = DbType.Int32;
					break;
				case DataType.Long:
					dbType = DbType.Int64;
					break;
				case DataType.Decimal:
					dbType = DbType.Decimal;
					break;
				case DataType.String:
					dbType = DbType.String;
					break;
				case DataType.DateTime:
					dbType = DbType.DateTime;
					break;
				case DataType.Currency:
					dbType = DbType.Currency;
					break;
				case DataType.Boolean:
					dbType = DbType.Boolean;
					break;
				default:
					throw new NotSupportedException();
			}
			return CreateParameter(name, dbType, null);
		}


		/// <summary>
		/// Creates and returns in parameter with the given name and data type.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateParameter(string name, DbType type)
		{
			return CreateParameter(name, type, null);
		}

		/// <summary>
		/// Creates and returns in parameter with the given name, data type and value.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateParameter(string name, DbType type,
			object value)
		{
			return CreateParameter(name, type, ParameterDirection.Input, value);
		}

		/// <summary>
		/// Creates and returns out parameter with the given name and data type.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateOutParameter(string name, DbType type)
		{
			return CreateParameter(name, type, ParameterDirection.Output, null);
		}

		/// <summary>
		/// Creates and returns parameter with the given name, data type, 
		/// direction and data type.
		/// </summary>
		/// <param name="name">The name of the parameter without database specific 
		/// prefix. For example, if in sql server, parameter name is @Value then
		/// pass only Value as a parameter name.</param>
		public static DbParameter CreateParameter(string name, DbType type,
			ParameterDirection direction, object value)
		{
			var param = CreateParameter(name);
			param.Direction = direction;
			param.DbType = type;
			param.Value = null != value ? value : DBNull.Value;
			return param;
		}

		#endregion

		#region "Execute/Load DataSet"

		public static DataSet ExecuteDataSet(string procedureName)
		{
			return ExecuteDataSet(CommandType.StoredProcedure,
				procedureName, null);
		}

		public static DataSet ExecuteDataSet(string procedureName,
			params DbParameter[] parameters)
		{
			return ExecuteDataSet(CommandType.StoredProcedure,
				procedureName, parameters);
		}

		public static DataSet ExecuteMetadataDataSet(string procedureName,
			params DbParameter[] parameters)
		{
			using (var command = GetCommand(CommandType.StoredProcedure, 
				procedureName, parameters))
			{
				return GetMetadataDatabase().ExecuteDataSet(command);
			}
		}

		public static DataSet ExecuteDataSet(CommandType commandType,
			string commandText, params DbParameter[] parameters)
		{
			using (var command = GetCommand(commandType, commandText, parameters))
			{
				return GetDatabase().ExecuteDataSet(command);
			}
		}

		public static T ExecuteDataTable<T>(string procedureName,
			params DbParameter[] parameters) where T : DataTable, new()
		{
			return ExecuteDataTable<T>(CommandType.StoredProcedure,
				procedureName, parameters);
		}

		public static T ExecuteDataTable<T>(CommandType commandType,
			string commandText, params DbParameter[] parameters)
			where T : DataTable, new()
		{
			var result = new T();
			LoadDataTable(result, LoadOption.PreserveChanges, commandType,
				commandText, parameters);
			return result;
		}

		public static void LoadDataSet(DataSet dataset, string tableName,
			string procedureName, params DbParameter[] parameters)
		{
			LoadDataSet(dataset, new[] { tableName }, CommandType.StoredProcedure,
				procedureName, parameters);
		}

		public static void LoadDataSet(DataSet dataset, string[] tableNames,
			CommandType commandType, string commandText,
			params DbParameter[] parameters)
		{
			using (var command = GetCommand(commandType, commandText, parameters))
			{
				GetDatabase().LoadDataSet(command, dataset, tableNames);
			}
		}

		public static void LoadDataTable(DataTable table, string procedureName,
			params DbParameter[] parameters)
		{
			LoadDataTable(table, LoadOption.PreserveChanges,
				CommandType.StoredProcedure, procedureName, parameters);
		}

		public static void LoadDataTable(DataTable table, LoadOption loadOption,
			CommandType commandType, string commandText,
			params DbParameter[] parameters)
		{
			using (var command = GetCommand(commandType, commandText, parameters))
			{
				var reader = GetDatabase().ExecuteReader(command);
				using (reader)
				{
					table.Load(reader, loadOption);
				}
			}
		}

		public static void LoadMetadataDataSet(DataSet dataset, string[] tableNames,
			string procedureName, params DbParameter[] parameters)
		{
			var database = GetMetadataDatabase();
			using (var command = GetCommand(database, CommandType.StoredProcedure,
				procedureName, parameters))
			{
				database.LoadDataSet(command, dataset, tableNames);
			}
		}

		#endregion

		#region "Execute Scalar/ Execute NonQuery"

		public static object ExecuteScalar(string procedureName,
			params DbParameter[] parameters)
		{
			return ExecuteScalar(CommandType.StoredProcedure,
				procedureName, parameters);
		}

		public static object ExecuteScalar(CommandType commandType,
			string commandText, params DbParameter[] parameters)
		{
			using (var command = GetCommand(commandType, commandText, parameters))
			{
				return GetDatabase().ExecuteScalar(command);
			}
		}

		public static int ExecuteNonQuery(string procedureName,
			params DbParameter[] parameters)
		{
			return ExecuteNonQuery(CommandType.StoredProcedure,
				procedureName, parameters);
		}

		public static int ExecuteNonQuery(CommandType commandType,
			string commandText, params DbParameter[] parameters)
		{
			using (var command = GetCommand(commandType, commandText, parameters))
			{
				return GetDatabase().ExecuteNonQuery(command);
			}
		}

		public static int ExecuteMetadataNonQuery(string procedureName,
			params DbParameter[] parameters)
		{
			var database = GetMetadataDatabase();
			using (var command = GetCommand(database, CommandType.StoredProcedure,
				procedureName, parameters))
			{
				return database.ExecuteNonQuery(command);
			}
		}

		#endregion

		#region "Reading Data"

		/// <summary>
		/// Returns the (not nullable) value of the given column.
		/// </summary>
		public static T Read<T>(this DataRow row, string columnName)
		{
			return (T)row[columnName];
		}

		/// <summary>
		/// Returns the nullable string value of the given column.
		/// </summary>
		public static string ReadString(this DataRow row, string columnName)
		{
			var value = row[columnName];
			return value == DBNull.Value ? null : value.ToString();
		}

		/// <summary>
		/// Returns the nullable value of the given column.
		/// </summary>
		public static T? ReadNullable<T>(this DataRow row, string columnName)
			where T : struct
		{
			var value = row[columnName];
			return value == DBNull.Value ? null : (T?)value;
		}

		/// <summary>
		/// Returns the (not nullable) value of the given column.
		/// </summary>
		public static T Read<T>(DataRow row, int columnIndex)
		{
			return (T)row[columnIndex];
		}

		/// <summary>
		/// Returns the nullable string value of the given column.
		/// </summary>
		public static string ReadString(this DataRow row, int columnIndex)
		{
			var value = row[columnIndex];
			return value == DBNull.Value ? null : value.ToString();
		}

		/// <summary>
		/// Returns the nullable value of the given column.
		/// </summary>
		public static T? ReadNullable<T>(this DataRow row, int columnIndex)
			where T : struct
		{
			var value = row[columnIndex];
			if (value == DBNull.Value)
			{
				return null;
			}
			return (T)value;
		}

		#endregion

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
		/// Analyzes the given DB Exception for retrieving more infomation
		/// about the error. 
		/// </summary>
		/// <param name="messageId">User Message Id retrieved from Db Exception.
		/// Applicable only when return value is DbError.Application</param>
		public static DbError ParseDbException(DbException execption,
			out int messageId)
		{
			const string MESSAGE_ID_KEY1 = "[**WINDSTAR_MESSAGE:";
			const string MESSAGE_ID_KEY2 = "**]";

			messageId = 0;
			// Search for special keys within the exception message
			var message = execption.Message;
			var key1Index = message.IndexOf(MESSAGE_ID_KEY1);
			if (key1Index >= 0)
			{
				var start = key1Index + MESSAGE_ID_KEY1.Length;
				var end = message.IndexOf(MESSAGE_ID_KEY2, start);
				if (end >= 0)
				{
					// Found, parse the message id
					if (int.TryParse(message.Substring(start, end - start),
						out messageId))
					{
						return DbError.Application;
					}
				}
			}

			// Handle Sql Server specific error code
			var sqlExecption = execption as System.Data.SqlClient.SqlException;
			if (null == sqlExecption)
			{
				return DbError.Unknown;
			}
			bool isConstraintViolation = false;
			if (sqlExecption.Number == 547)
			{
				isConstraintViolation = true;
			}
			else if (sqlExecption.Number == 50000 && (
				message.IndexOf("Error Number: 547") >= 0 || 
				message.IndexOf("Error Number: 2627") >= 0 ))
			{
				// Our Common logging sp converts sql errors into user
				// errors and original sql error can be found in message
				// string - so here we have again found constraint 
				// violation error.
				isConstraintViolation = true;
			}
			// In case of constraint violation, see if its foregin key or
			// unique key or primary key
			if (isConstraintViolation)
			{
				if (message.IndexOf("REFERENCE") >= 0)
				{
					return DbError.ForeignKeyViolation;
				}
				else if (message.IndexOf("PRIMARY KEY") >= 0)
				{
					return DbError.PrimaryKeyViolation;
				}
				else if (message.IndexOf("UNIQUE KEY") >= 0)
				{
					return DbError.UniqueKeyViolation;
				}
			}
			return DbError.Unknown;
		}

		/// <summary>
		/// Attempts to map the generic database exception to specific 
		/// application exception. Returns true if existing exception to
		/// be re-thrown.
		/// </summary>
		public static bool HandleDbException(OperationType operation, DbException e)
		{
			// Parse exception
			int messageId;
			switch (DbHelper.ParseDbException(e, out messageId))
			{
				case DbHelper.DbError.Application:
					throw new WindStarException(messageId);

				case DbHelper.DbError.ForeignKeyViolation:
					if (operation == OperationType.Delete)
					{
						throw new CommonDbException(
							CommonDbException.MESSAGE_DELETE_DATA_IN_USE);
					}
					break;

				case DbHelper.DbError.UniqueKeyViolation:
				case DbHelper.DbError.PrimaryKeyViolation:
					if (operation == OperationType.Add ||
						operation == OperationType.Update)
					{
						throw new CommonDbException(
							CommonDbException.MESSAGE_DUPLICATE_DATA);
					}
					break;
			}
			return true;
		}

		#endregion

		#region "Database Expressions"
		// Methods for returning various database specific expressions that may
		// be needed in query generation. Add them as need arises.

		/// <summary>
		/// Gets the data base expression for taking left n characters of an
		/// string.
		/// </summary>
		public static string GetDbExpressionForLeft(string variable, int length)
		{
			return GetDbExpressionForSubString(variable, 0, length);
		}

		/// <summary>
		/// Gets the data base expression for getting substring of a given string
		/// variable. Start is zero based index within the string.
		/// </summary>
		public static string GetDbExpressionForSubString(string variable, 
			int start, int length)
		{
			return string.Format(
				"SUBSTRING({0}, {1}, {2})", variable, start + 1, length);
		}

		#endregion

		#region "Helper Methods"

		/// <summary>
		/// Parse time stamp information from the given row provided 
		/// standard column names are used.
		/// </summary>
        public static TimeStamp? ParseTimeStampInfo(DataRow row)
        {
            if (!DbHelper.ReadNullable<DateTime>(row, "CreatedOn").HasValue ||
                !DbHelper.ReadNullable<int>(row, "CreatedBy").HasValue ||
                !DbHelper.ReadNullable<DateTime>(row, "LastUpdatedOn").HasValue ||
                !DbHelper.ReadNullable<int>(row, "LastUpdatedBy").HasValue)
            {
                return null;
            }
            return new TimeStamp()
            {
                CreatedOn = DbHelper.Read<DateTime>(row, "CreatedOn"),
                CreatedBy = DbHelper.Read<int>(row, "CreatedBy"),
                LastUpdatedOn = DbHelper.Read<DateTime>(row, "LastUpdatedOn"),
                LastUpdatedBy = DbHelper.Read<int>(row, "LastUpdatedBy")
            };
        }

		private static DbCommand GetCommand(CommandType commandType,
			string commandText, DbParameter[] parameters)
		{
			return GetCommand(GetDatabase(), commandType, commandText, parameters);
		}

		private static DbCommand GetCommand(Database database,
			CommandType commandType, string commandText, DbParameter[] parameters)
		{
			// Create command
			DbCommand command;
			if (commandType == CommandType.StoredProcedure)
			{
				// Qualify procedure name
				commandText = QualifyProcedureName(commandText);
				command = database.GetStoredProcCommand(commandText);
			}
			else
			{
				command = database.GetSqlStringCommand(commandText);
			}

			// Configure command
			command.CommandTimeout =
				Core.AppContext.Current.Config.Database.CommandTimeout;

			// Add parameters
			if (null != parameters)
			{
				for (int i = 0; i < parameters.Length; i++)
				{
					command.Parameters.Add(parameters[i]);
				}
			}
			return command;
		}

		private static DbParameter CreateParameter(string name)
		{
			var database = GetDatabase();
			var param = database.DbProviderFactory.CreateParameter();
			param.ParameterName = database.BuildParameterName(name);
			return param;
		}

		// Returns schema/user qualified stored procedure name
		private static string QualifyProcedureName(string name)
		{
			if (!name.StartsWith("dbo.", StringComparison.OrdinalIgnoreCase) &&
				name.StartsWith("[dbo].", StringComparison.OrdinalIgnoreCase))
			{
				name = "dbo." + name;
			}
			return name;
		}

		#endregion

	}
}
