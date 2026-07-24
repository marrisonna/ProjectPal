using System;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System.Text;

namespace Utilities.Database
{
    public enum DbExceptionType
    {
        ForeignKeyFailure,
        LoginFailure,
        ConnectionFailure,
        LocksFailure,
        ConcurrencyFailure,
        DatabaseFailure,
        ConstraintFailure,
        TransactionFailure,
        OtherFailure
    }

    public class DbException : Exception
    {
        public DbExceptionType ExceptionType { get; private set; }
        public DataSet OriginalDataSet { get; private set; }
        public IList<DataRow> ErrorRows { get; private set; }

        public DbException(string message,Exception ex,DataSet culpritDS) : base(message, ex)
        {
            OriginalDataSet = culpritDS;
            Type thrownType = ex.GetType();
            if(thrownType.Equals(typeof(ConstraintException)))
            {
                ExceptionType = DbExceptionType.ConstraintFailure;
            }
            else if(thrownType.Equals(typeof(DBConcurrencyException)))
            {
            }
            else if(thrownType.Equals(typeof(DataException)))
            {
                 ExceptionType = DbExceptionType.ConstraintFailure;
            }
            else if(thrownType.Equals(typeof(SqlException)))
            {
                 HandleSqlException(ex as SqlException);
            }
            else
            {
                 ExceptionType = DbExceptionType.OtherFailure;
            }
            ErrorRows = DbHelper.ToRowsWithErrors(OriginalDataSet);
        }

        //547 - foriegn key violation
        //1201 - 1223 locks
        //2502 - could not start transaction
        //2520-5 - could not find database
        //2627 - Unique Index/Constraint violation
        //2601 - Unique Index/Constraint violation
        //4060-4 - could not open database
        //18450 - 18461 - login failed
        //18482,3,5 - could not connect to server
        private void HandleSqlException(SqlException ex)
        {
            if(ex.Number >= 1201 && ex.Number <=1223)
            {
                ExceptionType = DbExceptionType.LocksFailure;
            }
            else if(ex.Number == 2502)
            {
                ExceptionType = DbExceptionType.TransactionFailure;
            }
            else if(ex.Number >= 2520 && ex.Number <=2525)
            {
                ExceptionType = DbExceptionType.DatabaseFailure;
            }
            else if(ex.Number == 2627 || ex.Number == 2601 || ex.Number == 547)
            {
                ExceptionType = DbExceptionType.ConstraintFailure;
            }
            if (ex.Number >= 4060 && ex.Number <= 4064)
            {
                ExceptionType = DbExceptionType.DatabaseFailure;
            }
            else if (ex.Number >= 18450 && ex.Number <= 18461)
            {
                ExceptionType = DbExceptionType.LoginFailure;
            }
            else if (ex.Number == 18482 || ex.Number == 18483 || ex.Number == 18485)
            {
                ExceptionType = DbExceptionType.ConnectionFailure;
            }
            else 
                ExceptionType = DbExceptionType.OtherFailure;
        }
    }
}
