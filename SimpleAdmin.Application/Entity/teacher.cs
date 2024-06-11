// Copyright (c) 2022-Now 少林寺驻北固山办事处大神父王喇嘛
// 
// SimpleAdmin 基于 Apache License Version 2.0 协议发布，可用于商业项目，但必须遵守以下补充条款:
// 1.请不要删除和修改根目录下的LICENSE文件。
// 2.请不要删除和修改SimpleAdmin源码头部的版权声明。
// 3.分发源码时候，请注明软件出处 https://gitee.com/dotnetmoyu/SimpleAdmin
// 4.基于本软件的作品，只能使用 SimpleAdmin 作为后台服务，除外情况不可商用且不允许二次分发或开源。
// 5.请不得将本软件应用于危害国家安全、荣誉和利益的行为，不能以任何形式用于非法为目的的行为。
// 6.任何基于本软件而产生的一切法律纠纷和责任，均于我司无关。

namespace SimpleAdmin.Application.Entity;
[SugarTable("app_teacher", TableDescription = "教师表")]
[Tenant(SqlSugarConst.DB_DEFAULT)]
public class teacher
{
           [SugarColumn(ColumnName = "id",ColumnDescription = "教师id", IsPrimaryKey=true,IsIdentity=true)]
           public long id {get;set;}

           [SugarColumn(ColumnName = "name", ColumnDescription = "姓名", Length = 100, IsNullable = false)]
           public string name {get;set;}
           
           [SugarColumn(ColumnName = "nickname", ColumnDescription = "艺名/花名/常用名/英文名", Length = 100, IsNullable = false)]
           public string  nickname {get;set;}
           
           [SugarColumn(ColumnName = "age", ColumnDescription = "年龄", Length = 100, IsNullable = false)]
           public string age {get;set;}

           [SugarColumn(ColumnName = "education", ColumnDescription = "学历", Length = 100, IsNullable = false)]
           public string education {get;set;}

           [SugarColumn(ColumnName = "sex", ColumnDescription = "性别", Length = 100, IsNullable = false)]
           public string sex {get;set;}
           
           [SugarColumn(ColumnName = "phone", ColumnDescription = "电话", Length = 100, IsNullable = false)]
           public string phone {get;set;}
           
           [SugarColumn(ColumnName = "mail", ColumnDescription = "邮箱", Length = 100, IsNullable = false)]
           public string mail {get;set;}
           
           [SugarColumn(ColumnName = "wechat", ColumnDescription = "微信号", Length = 100, IsNullable = false)]
           public string wechat {get;set;}
           
           [SugarColumn(ColumnName = "address", ColumnDescription = "收货地址", Length = 200, IsNullable = false)]
           public string address {get;set;}
           
           [SugarColumn(ColumnName = "usualaddress", ColumnDescription = "常住地", Length = 200, IsNullable = false)]
           public string usualaddress {get;set;}
           
           [SugarColumn(ColumnName = "maintain", ColumnDescription = "商务关系维护", Length = 200, IsNullable = false)]
           public string maintain {get;set;}
           
           [SugarColumn(ColumnName = "type", ColumnDescription = "老师性质", Length = 100, IsNullable = false)]
           public string type {get;set;}
           
           [SugarColumn(ColumnName = "back", ColumnDescription = "行业背景", Length = 200, IsNullable = false)]
           public string back {get;set;}

           [SugarColumn(ColumnName = "create_time", ColumnDescription = "创建时间", IsNullable = false)]
           public DateTime? create_time {get;set;}
           
           [SugarColumn(ColumnName = "update_time", ColumnDescription = "更新时间",  IsNullable = false)]
           public DateTime? update_time {get;set;}
           
           [SugarColumn(ColumnName = "is_deleted", ColumnDescription = "删除标记",  IsNullable = false)]
           public bool is_deleted {get;set;}

                

         
      

           
   
}