using System.Data;
using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic.CompilerServices;

namespace TTback.MyUtils;

using SqlSugar;
using Microsoft.Extensions.Configuration;

public class DatabaseHelper
{
    private readonly SqlSugarClient db;
    private string connects = "";

    public DatabaseHelper()
    {
        connects = "Server=58.240.211.43,8097;Database=SimpleAdmin;User ID=sa;Password=Syl600183;";
        db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connects,
            // ConnectionString = "Data Source=localhost,1433;Initial Catalog=TTback;User ID=sa;Password=London0401@;",
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            // 使用属性名称作为实体的主键
            InitKeyType = InitKeyType.Attribute
        });
    }


    //查询数据
    public List<T> Query<T>() where T : class, new()
    {
        return db.Queryable<T>().ToList();
    }

    //执行sql 语句
    public List<T> ExecuteRawSql<T>(string sql, params SugarParameter[] parameters) where T : class, new()
    {
        return db.Ado.SqlQuery<T>(sql, parameters);
    }


    //插入数据
    public bool Insert<T>(T entity) where T : class, new()
    {
        return db.Insertable(entity).ExecuteCommand() > 0;
    }
    
    // 插入数据并且返回自增的id
    public int InsertAndReturnId<T>(T entity) where T : class, new()
    {
        return db.Insertable(entity).ExecuteReturnIdentity();
    }

    //更新数据
    public bool Update<T>(T entity) where T : class, new()
    {
        return db.Updateable(entity).ExecuteCommand() > 0;
    }

    //删除数据
    public bool Delete<T>(T entity) where T : class, new()
    {
        return db.Deleteable(entity).ExecuteCommand() > 0;
    }


    //根据id 查询数据
    public T QueryById<T>(long id) where T : class, new()
    {
        return db.Queryable<T>().InSingle(id);
    }

    // 按条件查询数据
    // Expression<Func<User, bool>> condition = u => u.Age > 25 && u.Gender == "Male";
    // var result = dbHelper.QueryByCondition<User>(condition);
    public List<T> QueryByCondition<T>(Expression<Func<T, bool>> whereExpression) where T : class, new()
    {
        return db.Queryable<T>().Where(whereExpression).ToList();
    }

    // 执行自定义 SQL 查询
    // string sql = "SELECT * FROM Users WHERE Age > @age";
    // var parameters = new { age = 25 };
    // var result = dbHelper.ExecuteSqlQuery<User>(sql, parameters);
    public List<T> ExecuteSqlQuery<T>(string sql, object parameters = null) where T : class, new()
    {
        return db.Ado.SqlQuery<T>(sql, parameters);
    }

    // 执行自定义 SQL 命令
    // string sql = "DELETE FROM Users WHERE Id = @id";
    // var parameters = new { id = 10 };
    // int rowsAffected = dbHelper.ExecuteSqlCommand(sql, parameters);
    public int ExecuteSqlCommand(string sql, object parameters = null)
    {
        return db.Ado.ExecuteCommand(sql, parameters);
    }

    /*调用存储过程*/
    public void CallStoredProcedure(string procedureName)
    {
        using (SqlConnection connection = new SqlConnection(connects))
        {
            using (SqlCommand command = new SqlCommand(procedureName, connection))
            {
                try
                {
                    connection.Open();
                    command.CommandType = CommandType.StoredProcedure;


                    // 执行存储过程
                    command.ExecuteNonQuery();

                    // 如果存储过程有返回结果集，可以使用 SqlDataReader 来读取结果
                    // using (SqlDataReader reader = command.ExecuteReader())
                    // {
                    //     while (reader.Read())
                    //     {
                    //         // 处理结果集
                    //     }
                    // }
                }
                catch (Exception ex)
                {
                    // 处理异常
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }
    }
}