using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Utilities.Logging;

namespace Utilities.Database
{
    public static class DbHelper
    {
        #region Misc
        public static IList<DataRow> ToRowsWithErrors(DataSet ds)
        {
            IList<DataRow> rowsWithErrors = null;
            if (ds != null && true == ds.HasErrors)
            {
                rowsWithErrors = new List<DataRow>();
                foreach (DataTable table in ds.Tables)
                {
                    if (table.HasErrors)
                    {
                        DataRow[] errorRows = table.GetErrors();
                        if (errorRows != null)
                        {
                            foreach (DataRow row in errorRows)
                            {
                                rowsWithErrors.Add(row);
                            }
                        }

                    }
                }
            }
            return rowsWithErrors;
        }
        #endregion

        #region Get DataRow

        public static T? GetColumnValueAs<T>(DataRow row, int columnIndex)
            where T : struct
        {
            DataTable table = row.Table;
            if (table.Columns.Count <= columnIndex)
            {
                Logger.ErrorThrow(new Exception(string.Format("Invalid columnIndex index= {0}. " +
                                                  "There are {1} columns in the data table",
                                                  columnIndex, table.Columns.Count)));
            }

            DataColumn column = table.Columns[columnIndex];
            if (column.DataType != typeof(T))
            {
                Logger.ErrorThrow(new Exception("Invalid column type for column '" +
                                    table.Columns[columnIndex].ColumnName +
                                    "' (index = " + columnIndex +
                                    "), expected '" + typeof(T).ToString() +
                                    "' but got '" + column.DataType + "'"));
            }

            var value = row[columnIndex];
            if (value.GetType() == typeof(System.DBNull))
                return null;

            
            T? result = (T?)(row[column]);
            return result;
        }

        public static T? GetColumnValueAs<T>(DataRow row, string columnName)
          where T : struct
        {
            DataTable table = row.Table;
            int columnIndex = table.Columns.IndexOf(columnName);

            if (columnIndex == -1)
            {
                Logger.ErrorThrow(new Exception("Invalid column name " + columnName));
            }
            return GetColumnValueAs<T>(row, columnIndex);
        }

        public static bool? GetColumnValueAsBool(DataRow row, int columnIndex)
        {
            DataTable table = row.Table;
            if (table.Columns.Count <= columnIndex)
            {
                Logger.ErrorThrow(new Exception(string.Format("Invalid columnIndex index= {0}. " +
                                                  "There are {1} columns in the data table",
                                                  columnIndex, table.Columns.Count)));
            }

            DataColumn column = table.Columns[columnIndex];
            if (column.DataType != typeof(string))
            {
                Logger.ErrorThrow(new Exception("Invalid column type for column '" +
                                    table.Columns[columnIndex].ColumnName +
                                    "' (index = " + columnIndex +
                                    "), expected '" + typeof(string).ToString() +
                                    "' but got '" + column.DataType + "'"));
            }

            var value = row[columnIndex];
            if (value.GetType() == typeof(System.DBNull))
                return null;

            string valueStr = (string)(row[column]);
            bool? result = (valueStr == "Y" ? true : false);
            return result;
        }
        
        public static bool? GetColumnValueAsBool(DataRow row, string columnName)
        {
            DataTable table = row.Table;
            int columnIndex = table.Columns.IndexOf(columnName);

            if (columnIndex == -1)
            {
                Logger.ErrorThrow( new Exception("Invalid column name " + columnName));
            }
            return GetColumnValueAsBool(row, columnIndex);
        }

        public static string GetColumnValueAsString(DataRow row, int columnIndex)
        {
            DataTable table = row.Table;
            if (table.Columns.Count <= columnIndex)
            {
                Logger.ErrorThrow(new Exception(string.Format("Invalid columnIndex index= {0}. " +
                                                  "There are {1} columns in the data table",
                                                  columnIndex, table.Columns.Count)));
            }

            DataColumn column = table.Columns[columnIndex];
            if (column.DataType != typeof(string))
            {
                Logger.ErrorThrow(new Exception("Invalid column type for column '" +
                                    table.Columns[columnIndex].ColumnName +
                                    "' (index = " + columnIndex +
                                    "), expected '" + typeof(string).ToString() +
                                    "' but got '" + column.DataType + "'"));
            }

            var value = row[columnIndex];
            if (value.GetType() == typeof(System.DBNull))
                return null;

            string result = (string)(row[column]);
            return result;
        }

        public static string GetColumnValueAsString(DataRow row, string columnName)
        {
            DataTable table = row.Table;
            int columnIndex = table.Columns.IndexOf(columnName);

            if (columnIndex == -1)
            {
                Logger.ErrorThrow(new Exception("Invalid column name " + columnName));
            }
            return GetColumnValueAsString(row, columnIndex);
        }
        
        #endregion

        #region Get DataReader
        public static T? GetColumnValueAs<T>(IDataReader reader, int columnIndex)
           where T : struct
        {
            if (reader.FieldCount <= columnIndex)
            {
                Logger.ErrorThrow(new Exception(string.Format("Invalid columnIndex index= {0}. " +
                                                  "There are {1} columns in the data table",
                                                  columnIndex, reader.FieldCount)));
            }

            if (reader.GetFieldType(columnIndex) != typeof(T))
            {
                Logger.ErrorThrow( new Exception("Invalid column type for column '" +
                                    reader.GetName(columnIndex) +
                                    "' (index = " + columnIndex +
                                    "), expected '" + typeof(T).ToString() +
                                    "' but got '" + reader.GetFieldType(columnIndex) + "'"));
            }

            if (reader.IsDBNull(columnIndex))
                return null;

            T? result = (T?)reader.GetValue(columnIndex);
            return result;
        }

        public static T? GetColumnValueAs<T>(IDataReader reader, string columnName)
        where T : struct
        {
            int columnIndex = -1;
            try
            {
                columnIndex = reader.GetOrdinal(columnName);
            }
            catch (Exception exp)
            {
                Logger.ErrorThrow(new Exception("Could not find a field named '" + columnName + "'", exp));            
            }

            if (columnIndex == -1)
            {
                Logger.ErrorThrow(new Exception("Invalid column name " + columnName));
            }
            return GetColumnValueAs<T>(reader, columnIndex);
        }

        public static bool? GetColumnValueAsBool(IDataReader reader, int columnIndex)
        {
            if (reader.FieldCount <= columnIndex)
            {
                Logger.ErrorThrow(new Exception(string.Format("Invalid columnIndex index= {0}. " +
                                                  "There are {1} columns in the data table",
                                                  columnIndex, reader.FieldCount)));
            }

            if (reader.GetFieldType(columnIndex) != typeof(string))
            {
                Logger.ErrorThrow(new Exception("Invalid column type for column '" +
                                    reader.GetName(columnIndex) +
                                    "' (index = " + columnIndex +
                                    "), expected '" + typeof(string).ToString() +
                                    "' but got '" + reader.GetFieldType(columnIndex) + "'"));
            }

            if (reader.IsDBNull(columnIndex))
                return null;
            string valueStr = (string)reader.GetValue(columnIndex);
            bool? result = (valueStr == "Y" ? true : false);
            return result;
        }

        public static bool? GetColumnValueAsBool(IDataReader reader, string columnName)
        {
            int columnIndex = reader.GetOrdinal(columnName);

            if (columnIndex == -1)
            {
                Logger.ErrorThrow(new Exception("Invalid column name " + columnName));
            }
            return GetColumnValueAsBool(reader, columnIndex);
        }
        
        public static string GetColumnValueAsString(IDataReader reader, int columnIndex)
        {
            if (reader.FieldCount <= columnIndex)
            {
                Logger.ErrorThrow(new Exception(string.Format("Invalid columnIndex index= {0}. " +
                                                  "There are {1} columns in the data table",
                                                  columnIndex, reader.FieldCount)));
            }

            if (reader.GetFieldType(columnIndex) != typeof(string))
            {
                Logger.ErrorThrow(new Exception("Invalid column type for column '" +
                                    reader.GetName(columnIndex) +
                                    "' (index = " + columnIndex +
                                    "), expected '" + typeof(string).ToString() +
                                    "' but got '" + reader.GetFieldType(columnIndex) + "'"));
            }

            if (reader.IsDBNull(columnIndex))
                return null;

            string result = (string)reader.GetValue(columnIndex);
            return result;
        }

        public static string GetColumnValueAsString(IDataReader reader, string columnName)
        {
            int columnIndex = reader.GetOrdinal(columnName);

            if (columnIndex == -1)
            {
                Logger.ErrorThrow(new Exception("Invalid column name " + columnName));
            }
            return GetColumnValueAsString(reader, columnIndex);
        }

        public static T? GetEnumValue<T>(IDataReader reader, int column)
            where T : struct
        {
            string value = GetColumnValueAsString(reader, column);
            if (value == null)
                return null;

            if (value[0] >= '0' && value[0] <= '9')
                value = "_" + value;

            return (T)Enum.Parse(typeof(T), value, true);
        }

        #endregion

        #region AsSQLString

        public static string AsSqlString(string value)
        {
            if (null == value)
            {
                return "null";
            }
            value = value.Replace("'", "''");
            value = "'" + value + "'";
            return value;
        }

        public static string AsSqlString(DateTime? value)
        {
            if (false == value.HasValue)
            {
                return "null";
            }
            return "'" + value.Value.ToString("dd-MMM-yyyy hh:mmm:ss.fff tt") + "'";
        }

        public static string AsSqlString(bool? value)
        {
            if (false == value.HasValue)
            {
                return "null";
            }
            return "'" + (value.Value ? 'Y' : 'N') + "'";
        }

        public static string AsSqlString(int? value, Type enumType)
        {
            if (!value.HasValue)
                return "null";
            String name = Enum.GetName(enumType, value.Value);

            // Get rid of leading '_' since these indicate the value starts with a number
            // which enums cannot, so an '_' is added.
            while (name[0] == '_')
                name = name.Substring(1);

            return "'" + name + "'";
        }

        
        public static string AsSqlString<T>(Nullable<T> value)
            where T : struct
        {
            if (false == value.HasValue)
            {
                return "null";
            }
            return value.Value.ToString();
        }

        public static string AsSqlString<T>(Nullable<T> value, string format)
            where T : struct
        {

            // Hmmm, I wonder which 'ToString' method gets called.
            if (false == value.HasValue)
            {
                return "null";
            }
            T actualValue = value.Value;
            return actualValue.ToString();
        }
        #endregion

        #region GetSqlString Methods to convert types to string

        public static string GetSqlString(DateTime? value)
        {
            if (false == value.HasValue)
            {
                return null;
            }
            return value.Value.ToString("dd-MMM-yyyy hh:mmm:ss.fff tt");
        }

        public static string GetSqlString(bool? value)
        {
            if (false == value.HasValue)
            {
                return null;
            }
            return (value.Value ? "Y" : "N");
        }

        public static string GetSqlString(int? value, Type enumType)
        {
            if (!value.HasValue)
            {
                return null;
            }
            String name = Enum.GetName(enumType, value.Value);

            // Get rid of leading '_' since these indicate the value starts with a number
            // which enums cannot, so an '_' is added.
            while (name[0] == '_')
                name = name.Substring(1);

            return name;
        }


        public static string GetSqlString<T>(Nullable<T> value)
            where T : struct
        {
            if (false == value.HasValue)
            {
                return null;
            }
            return value.Value.ToString();
        }

        public static string GetSqlString<T>(Nullable<T> value, string format)
            where T : struct
        {

            // Hmmm, I wonder which 'ToString' method gets called.
            if (false == value.HasValue)
            {
                return null;
            }
            T actualValue = value.Value;
            return actualValue.ToString();
        }
        #endregion
    }
}
