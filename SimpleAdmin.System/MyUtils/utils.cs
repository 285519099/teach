using System.Data;
using Microsoft.Data.SqlClient;

using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public class utils
{
    public void execudeSqlCommand(string storedProcedureName)
    {
        string connectionString = "Server=58.240.211.43,8097;Database=SimpleAdmin;User ID=sa;Password=Syl600183;";
        SqlConnection connection = new SqlConnection(connectionString);

        SqlCommand command = new SqlCommand(storedProcedureName, connection);
        command.CommandType = CommandType.StoredProcedure;

        // 添加存储过程的输入参数
        string parameterName = "@Your_Parameter_Name";
        object parameterValue = "Your_Parameter_Value";
        SqlParameter parameter = new SqlParameter(parameterName, SqlDbType.VarChar);
        parameter.Value = parameterValue;
        command.Parameters.Add(parameter);

        connection.Open();
        command.ExecuteNonQuery();
        connection.Close();
    }

    // public static string getConnection()
    // {
    //     var builder = new ConfigurationBuilder()
    //         .SetBasePath(AppDomain.CurrentDomain.BaseDirectory) // 设置配置文件的基路径
    //         .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true); // 指定appsettings.json文件的路径
    //     // 构建配置对象
    //     IConfiguration configuration = builder.Build();
    //     // 读取数据库连接字符串
    //     return configuration.GetConnectionString("MyDBConnection");
    //     ;
    // }

    public static string getFilePath()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory) // 设置配置文件的基路径
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true); // 指定appsettings.json文件的路径
        // 构建配置对象
        IConfiguration configuration = builder.Build();
        // 读取数据库连接字符串
        return configuration.GetConnectionString("MyFilePath");
        ;
    }

    public static string getIntoPath()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory) // 设置配置文件的基路径
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true); // 指定appsettings.json文件的路径
        // 构建配置对象
        IConfiguration configuration = builder.Build();
        // 读取数据库连接字符串
        return configuration.GetConnectionString("intoPath");
        ;
    }

    public static string getmenuurl()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory) // 设置配置文件的基路径
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true); // 指定appsettings.json文件的路径
        // 构建配置对象
        IConfiguration configuration = builder.Build();
        // 读取数据库连接字符串
        return configuration.GetConnectionString("menuurl");
        ;
    }
}