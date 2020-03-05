<Query Kind="Program" />

void Main()
{
	// 設定 Class Name
	var className = "RoadModel";
	// 設定要查找註解的 Table Name, 多筆用 "," 分開
	var tables = @"YUTRoomNum";

	// 這邊修改為你要執行的 SQL Command
	var sqlCommand = @"SELECT *
FROM YUTRoomNum";

	var dtAnnotation = this.Connection.GetAnnotation(tables);

	//C#
	this.Connection.DumpClassCSharp(dtAnnotation, sqlCommand.ToString(), className).Dump();
	//VB
	//this.Connection.DumpClassVB(dtAnnotation, sqlCommand.ToString(), "County").Dump();

}
// Define other methods and classes here
public static class LINQPadExtensions
{
	private static readonly Dictionary<Type, string> TypeAliasesCSharp = new Dictionary<Type, string> {
		{ typeof(int), "int" },
		{ typeof(short), "short" },
		{ typeof(byte), "byte" },
		{ typeof(byte[]), "byte[]" },
		{ typeof(long), "long" },
		{ typeof(double), "double" },
		{ typeof(decimal), "decimal" },
		{ typeof(float), "float" },
		{ typeof(bool), "bool" },
		{ typeof(string), "string" }
	};

	private static readonly Dictionary<Type, string> TypeAliasesVB = new Dictionary<Type, string> {
		{ typeof(int), "Integer" },
		{ typeof(short), "Short" },
		{ typeof(byte), "Byte" },
		{ typeof(byte[]), "Byte()" },
		{ typeof(long), "Long" },
		{ typeof(double), "Double" },
		{ typeof(decimal), "Decimal" },
		{ typeof(float), "Float" },
		{ typeof(bool), "Boolean" },
		{ typeof(string), "String" }
	};

	private static readonly HashSet<Type> NullableTypes = new HashSet<Type> {
		typeof(int),
		typeof(short),
		typeof(long),
		typeof(double),
		typeof(decimal),
		typeof(float),
		typeof(bool),
		typeof(DateTime)
	};

	public static string DumpClassCSharp(this IDbConnection connection, DataTable dtAnnotation, string sql, string className = "Info")
	{
		if (connection.State != ConnectionState.Open)
		{
			connection.Open();
		}

		var cmd = connection.CreateCommand();
		cmd.CommandText = sql;
		var reader = cmd.ExecuteReader();

		var builder = new StringBuilder();
		do
		{
			if (reader.FieldCount <= 1) continue;

			builder.AppendFormat("public class {0}{1}", className, Environment.NewLine);
			builder.AppendLine("{");
			var schema = reader.GetSchemaTable();

			foreach (DataRow row in schema.Rows)
			{
				var type = (Type)row["DataType"];
				var name = TypeAliasesCSharp.ContainsKey(type) ? TypeAliasesCSharp[type] : type.Name;
				var isNullable = (bool)row["AllowDBNull"] && NullableTypes.Contains(type);
				var collumnName = (string)row["ColumnName"];

				var aa = (
						from dr in dtAnnotation.AsEnumerable()
						where dr.Field<string>("COLUMN_NAME") == collumnName
						select dr).FirstOrDefault();

				builder.AppendLine(string.Format("\t/// <summary>"));
				builder.AppendLine(string.Format("\t/// {0}", aa == null ? "對應不到註解" : aa.Field<string>("FieldMemo")));
				builder.AppendLine(string.Format("\t/// </summary>"));
				builder.AppendLine(string.Format("\tpublic {0}{1} {2} {{ get; set; }}", name, isNullable ? "?" : string.Empty, collumnName));
				builder.AppendLine();
			}

			builder.AppendLine("}");
			builder.AppendLine();
		} while (reader.NextResult());

		return builder.ToString();
	}

	public static string DumpClassVB(this IDbConnection connection, DataTable dtAnnotation, string sql, string className = "Info")
	{
		if (connection.State != ConnectionState.Open)
		{
			connection.Open();
		}

		var cmd = connection.CreateCommand();
		cmd.CommandText = sql;
		var reader = cmd.ExecuteReader();

		var builder = new StringBuilder();
		do
		{
			if (reader.FieldCount <= 1) continue;

			builder.AppendFormat("Public Class {0}{1}", className, Environment.NewLine);
			var schema = reader.GetSchemaTable();
			//schema.Columns.Dump();  
			foreach (DataRow row in schema.Rows)
			{
				var type = (Type)row["DataType"];
				var typename = row["DataTypeName"];
				var name = TypeAliasesVB.ContainsKey(type) ? TypeAliasesVB[type] : type.Name;
				if (Convert.ToString(typename) == "date")
				{
					name = "Date";
				}
				var isNullable = (bool)row["AllowDBNull"] && NullableTypes.Contains(type);
				var collumnName = (string)row["ColumnName"];

				var aa = (
						from dr in dtAnnotation.AsEnumerable()
						where dr.Field<string>("COLUMN_NAME") == collumnName
						select dr).FirstOrDefault();

				builder.AppendLine(string.Format("\t''' <summary>"));
				builder.AppendLine(string.Format("\t''' {0}", aa.Field<string>("FieldMemo")));
				builder.AppendLine(string.Format("\t''' </summary>"));
				builder.AppendLine(string.Format("\tPublic Property {0} As {1}{2} ", collumnName, name, isNullable ? "?" : string.Empty));
				builder.AppendLine();
			}

			builder.AppendLine("End Class");
			builder.AppendLine();
		} while (reader.NextResult());

		return builder.ToString();
	}

	//找出欄位備註
	public static DataTable GetAnnotation(this IDbConnection connection, string tables)
	{
		var sqlTable = "'" + string.Join("','", tables.Split(',')) + "'";
		var sql = string.Format(@"SELECT
							a.TABLE_NAME,
							b.COLUMN_NAME,
							b.DATA_TYPE,
							b.CHARACTER_MAXIMUM_LENGTH,
							b.COLUMN_DEFAULT,
							b.IS_NULLABLE,
							(SELECT
								value
							FROM fn_listextendedproperty(NULL, 'schema', 'dbo', 'table',
							a.TABLE_NAME, 'column', DEFAULT)
							WHERE name = 'MS_Description'
							AND objtype = 'COLUMN'
							AND objname COLLATE Chinese_Taiwan_Stroke_CI_AS = b.COLUMN_NAME)
							AS FieldMemo
						FROM INFORMATION_SCHEMA.TABLES a
						LEFT JOIN INFORMATION_SCHEMA.COLUMNS b
							ON (a.TABLE_NAME = b.TABLE_NAME)
						WHERE TABLE_TYPE = 'BASE TABLE'
						AND a.TABLE_NAME IN ({0})", sqlTable);

		if (connection.State != ConnectionState.Open)
		{
			connection.Open();
		}

		var adapter = new SqlDataAdapter();
		var cmd = connection.CreateCommand();
		cmd.CommandText = sql;
		cmd.CommandType = CommandType.Text;

		// Set the SqlDataAdapter's SelectCommand.
		adapter.SelectCommand = (SqlCommand)cmd;

		// Fill the DataSet.
		var dataSet = new DataSet();
		adapter.Fill(dataSet);

		return dataSet.Tables[0];
	}
}

