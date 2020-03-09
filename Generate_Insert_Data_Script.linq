<Query Kind="Program" />

void Main()
{
	var tableName = "Customers";
	var sqlCommand = @"
SELECT *
FROM Customers WITH (NoLock)
";

	var result = GenerateSql(this.Connection, tableName, sqlCommand);
	result.Dump();
}

public class ColumnInfo
{
	public string Name { get; set; }

	public string DataType { get; set; }

	public string DataTypeName { get; set; }
}

public string GenerateSql(IDbConnection connection, string tableName, string sqlCommand)
{
	IDbCommand command = new SqlCommand();
	command.Connection = connection;
	if (connection.State != ConnectionState.Open)
	{
		connection.Open();
	}
	command.CommandType = CommandType.Text;
	command.CommandText = sqlCommand;

	var reader = command.ExecuteReader();

	DataTable schemaTable = reader.GetSchemaTable();

	List<ColumnInfo> columnInfos = new List<ColumnInfo>();

	foreach (DataRow dataRow in schemaTable.Rows)
	{
		ColumnInfo info = new ColumnInfo();

		foreach (DataColumn dataColumn in schemaTable.Columns)
		{
			if (dataColumn.ColumnName.Equals("ColumnName"))
			{
				info.Name = dataRow[dataColumn].ToString();
			}
			if (dataColumn.ColumnName.Equals("DataType"))
			{
				info.DataType = dataRow[dataColumn].ToString();
			}
			if (dataColumn.ColumnName.Equals("DataTypeName"))
			{
				info.DataTypeName = dataRow[dataColumn].ToString();
			}
		}

		columnInfos.Add(info);
	}

	List<string> columns = new List<string>();

	foreach (DataRow row in schemaTable.Rows)
	{
		foreach (DataColumn column in schemaTable.Columns)
		{
			if (column.ColumnName.Equals("ColumnName"))
			{
				columns.Add(row[column].ToString().ToUpper());
			}
		}
	}

	string colNames = string.Join(",", columns.ToArray());

	StringBuilder sb = new StringBuilder();

	const string insertTemplate = @"INSERT INTO {0} ({1}) VALUES ({2})";

	while (reader.Read())
	{
		sb.AppendLine(string.Format(insertTemplate, tableName, colNames, Row_Values(reader, columnInfos)));
	}

	return sb.ToString();
}

public string Row_Values(IDataReader reader, List<ColumnInfo> columnInfos)
{
	List<string> colsVals = new List<string>();

	var unicodeDataTypes = new string[] { "nvarchar", "nchar", "ntext" };
	var dateTimeDateTypes = new string[] { "datetime", "smalldatetime" };

	for (int i = 0; i < reader.FieldCount; i++)
	{
		var columnName = reader.GetName(i);
		var columnInfo = columnInfos.FirstOrDefault(x => x.Name == columnName);

		string columnValueTemplate = "";
		string columnValue = "";

		if (reader[i].GetType().ToString().Equals("System.DBNull"))
		{
			colsVals.Add("NULL");
		}
		else if (reader[i] == null)
		{
			colsVals.Add("NULL");
		}
		else if (reader[i].GetType().ToString().Equals("System.String"))
		{
			if (reader[i] == null)
			{
				colsVals.Add("NULL");
			}
			else
			{
				// nvarchar, nchar, ntext
				if (unicodeDataTypes.Contains(columnInfo.DataTypeName.ToLower()))
				{
					columnValueTemplate = "N'{0}'";
				}
				else
				{
					columnValueTemplate = "'{0}'";
				}

				columnValue = string.Format(columnValueTemplate, reader[i].ToString().Replace("'", "''").Replace(",", "-"));
				colsVals.Add(columnValue);
			}
		}
		else
		{
			if (dateTimeDateTypes.Contains(columnInfo.DataTypeName.ToLower()))
			{
				// datetime, smalldatetime
				columnValue = ((DateTime)reader[i]).ToString("yyyyMMdd HH:mm:ss");
			}
			else
			{
				// other data type
				columnValue = reader[i].ToString().Replace("'", "''").Replace(",", "-");
			}

			colsVals.Add(string.Format("'{0}'", columnValue));
		}
	}

	return string.Join(",", colsVals.ToArray());
}

