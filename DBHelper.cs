using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace EFTest
{
    public static class MultipleResultSets
    {
        public static MultipleResultSetWrapper MultipleResults(this DbContext db, string storedProcedure, IEnumerable<DbParameter> parameters)
        {
            return new MultipleResultSetWrapper(db, storedProcedure, parameters);
        }

        public class MultipleResultSetWrapper
        {
            private readonly DbContext _db;
            private readonly string _storedProcedure;
            private readonly IEnumerable<DbParameter> _parameters;
            public List<Func<IObjectContextAdapter, DbDataReader, IEnumerable>> _resultSets;

            public MultipleResultSetWrapper(DbContext db, string storedProcedure, IEnumerable<DbParameter> parameters)
            {
                _db = db;
                _storedProcedure = storedProcedure;
                _parameters = parameters;
                _resultSets = new List<Func<IObjectContextAdapter, DbDataReader, IEnumerable>>();
            }

            public MultipleResultSetWrapper With<TResult>()
            {
                _resultSets.Add((adapter, reader) => adapter
                    .ObjectContext
                    .Translate<TResult>(reader)
                    .ToList());

                return this;
            }

            public List<IEnumerable> Execute()
            {
                var results = new List<IEnumerable>();

                using (var connection = _db.Database.Connection)
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = _storedProcedure;
                    command.CommandType = CommandType.StoredProcedure;
                    
                    if (_parameters != null)
                    {
                        var parm = command.CreateParameter();
                        parm.ParameterName = "@EmpId";
                        parm.Value = 1;
                        command.Parameters.Add(parm);
                    }
                    using (var reader = command.ExecuteReader())
                    {
                        var adapter = ((IObjectContextAdapter)_db);
                        foreach (var resultSet in _resultSets)
                        {
                            results.Add(resultSet(adapter, reader));
                            reader.NextResult();
                        }
                    }

                    return results;
                }
            }
        }
    }


}
