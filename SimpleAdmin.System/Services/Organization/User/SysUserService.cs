﻿// Copyright (c) 2022-Now 少林寺驻北固山办事处大神父王喇嘛
// 
// SimpleAdmin 基于 Apache License Version 2.0 协议发布，可用于商业项目，但必须遵守以下补充条款:
// 1.请不要删除和修改根目录下的LICENSE文件。
// 2.请不要删除和修改SimpleAdmin源码头部的版权声明。
// 3.分发源码时候，请注明软件出处 https://gitee.com/dotnetmoyu/SimpleAdmin
// 4.基于本软件的作品，只能使用 SimpleAdmin 作为后台服务，除外情况不可商用且不允许二次分发或开源。
// 5.请不得将本软件应用于危害国家安全、荣誉和利益的行为，不能以任何形式用于非法为目的的行为。
// 6.任何基于本软件而产生的一切法律纠纷和责任，均于我司无关。

using SimpleAdmin.Core.Extension;
using TTback.MyUtils;

namespace SimpleAdmin.System;

/// <summary>
/// <inheritdoc cref="ISysUserService"/>
/// </summary>
public class SysUserService : DbRepository<SysUser>, ISysUserService
{
    private readonly ILogger<ILogger> _logger;
    private readonly ISimpleCacheService _simpleCacheService;
    private readonly IRelationService _relationService;
    private readonly IResourceService _resourceService;
    private readonly ISysOrgService _sysOrgService;
    private readonly ISysRoleService _sysRoleService;
    private readonly IImportExportService _importExportService;
    private readonly ISysPositionService _sysPositionService;
    private readonly IDictService _dictService;
    private readonly IConfigService _configService;
    private readonly IBatchEditService _batchEditService;

    public SysUserService(ILogger<ILogger> logger, ISimpleCacheService simpleCacheService, IRelationService relationService,
        IResourceService resourceService, ISysOrgService orgService, ISysRoleService sysRoleService,
        IImportExportService importExportService, ISysPositionService sysPositionService, IDictService dictService,
        IConfigService configService, IBatchEditService updateBatchService)
    {
        _logger = logger;
        _simpleCacheService = simpleCacheService;
        _relationService = relationService;
        _resourceService = resourceService;
        _sysOrgService = orgService;
        _sysRoleService = sysRoleService;
        _importExportService = importExportService;
        _sysPositionService = sysPositionService;
        _dictService = dictService;
        _configService = configService;
        _batchEditService = updateBatchService;
    }

    #region 查询

    /// <inheritdoc/>
    public async Task<string> GetUserAvatar(long userId)
    {
        //先从缓存拿
        var avatar = _simpleCacheService.HashGetOne<string>(SystemConst.CACHE_SYS_USER_AVATAR, userId.ToString());
        if (string.IsNullOrEmpty(avatar))
        {
            //单查获取用户头像
            avatar = await GetFirstAsync(it => it.Id == userId, it => it.Avatar);
            if (!string.IsNullOrEmpty(avatar))
            {
                //插入缓存
                _simpleCacheService.HashAdd(SystemConst.CACHE_SYS_USER_AVATAR, userId.ToString(), avatar);
            }
        }
        return avatar;
    }

    /// <inheritdoc/>
    public async Task<SysUser> GetUserByAccount(string account, long? tenantId = null)
    {
        var userId = await GetIdByAccount(account, tenantId);//获取用户ID
        if (userId != SimpleAdminConst.ZERO)
        {
            var sysUser = await GetUserById(userId);//获取用户信息
            if (sysUser.Account == account)//这里做了比较用来限制大小写
                return sysUser;
            return null;
        }
        return null;
    }

    /// <inheritdoc/>
    public async Task<SysUser> GetUserByPhone(string phone, long? tenantId = null)
    {
        var userId = await GetIdByPhone(phone, tenantId);//获取用户ID
        if (userId > 0)
        {
            return await GetUserById(userId);//获取用户信息
        }
        return null;
    }

    /// <inheritdoc/>
    public async Task<long> GetIdByPhone(string phone, long? tenantId = null)
    {
        var orgIds = new List<long>();
        var key = SystemConst.CACHE_SYS_USER_PHONE;
        if (tenantId != null)
        {
            key += $":{tenantId}";
            orgIds = await _sysOrgService.GetOrgChildIds(tenantId.Value);//获取下级机构
        }
        //先从缓存拿
        var userId = _simpleCacheService.HashGetOne<long>(key, phone);
        if (userId == 0)
        {
            var sm4Phone = CryptogramUtil.Sm4Encrypt(phone);//SM4加密一下
            //单查获取用户手机号对应的账号
            userId = await Context.Queryable<SysUser>()
                .Where(it => it.Phone == sm4Phone)
                .WhereIF(orgIds.Count > 0, it => orgIds.Contains(it.OrgId))
                .Select(it => it.Id)
                .FirstAsync();
            if (userId > 0)
            {
                //插入缓存
                _simpleCacheService.HashAdd(key, phone, userId);
            }
        }
        return userId;
    }

    /// <inheritdoc/>
    public async Task<SysUser> GetUserById(long userId)
    {
        //先从缓存拿 
        var sysUser = _simpleCacheService.HashGetOne<SysUser>(SystemConst.CACHE_SYS_USER, userId.ToString());
        if (sysUser == null)
        {
            sysUser = await GetUserFromDb(userId);//从数据库拿用户信息
        }
        return sysUser;
    }

    /// <inheritdoc/>
    public async Task<T> GetUserById<T>(long userId)
    {
        var user = await GetUserById(userId);
        return user.Adapt<T>();
    }

    /// <inheritdoc/>
    public async Task<long> GetIdByAccount(string account, long? tenantId = null)
    {
        var orgIds = new List<long>();
        var key = SystemConst.CACHE_SYS_USER_ACCOUNT;
        if (tenantId != null)
        {
            key += $":{tenantId}";
            orgIds = await _sysOrgService.GetOrgChildIds(tenantId.Value);//获取下级机构
        }
        //先从缓存拿
        var userId = _simpleCacheService.HashGetOne<long>(key, account);
        if (userId == 0)
        {
            //单查获取用户账号对应ID
            userId = await Context.Queryable<SysUser>()
                .Where(it => it.Account == account)
                .WhereIF(orgIds.Count > 0, it => orgIds.Contains(it.OrgId))
                .Select(it => it.Id)
                .FirstAsync();
            if (userId != 0)
            {
                //插入缓存
                _simpleCacheService.HashAdd(key, account, userId);
            }
        }
        return userId;
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetButtonCodeList(long userId)
    {
        var buttonCodeList = new List<string>();//按钮ID集合
        //获取用户资源集合
        var resourceList = await _relationService.GetRelationListByObjectIdAndCategory(userId, CateGoryConst.RELATION_SYS_USER_HAS_RESOURCE);
        var buttonIdList = new List<long>();//按钮ID集合
        if (resourceList.Count == 0)//如果有表示用户单独授权了不走用户角色
        {
            //获取用户角色关系集合
            var roleList = await _relationService.GetRelationListByObjectIdAndCategory(userId, CateGoryConst.RELATION_SYS_USER_HAS_ROLE);
            var roleIdList = roleList.Select(x => x.TargetId.ToLong()).ToList();//角色ID列表
            if (roleIdList.Count > 0)//如果该用户有角色
            {
                resourceList = await _relationService.GetRelationListByObjectIdListAndCategory(roleIdList,
                    CateGoryConst.RELATION_SYS_ROLE_HAS_RESOURCE);//获取资源集合
            }
        }
        resourceList.ForEach(it =>
        {
            if (!string.IsNullOrEmpty(it.ExtJson))
                buttonIdList.AddRange(it.ExtJson.ToJsonEntity<RelationRoleResource>().ButtonInfo);//如果有按钮权限，将按钮ID放到buttonIdList
        });
        if (buttonIdList.Count > 0)
        {
            buttonCodeList = await _resourceService.GetCodeByIds(buttonIdList, CateGoryConst.RESOURCE_BUTTON);
        }
        return buttonCodeList;
    }

    /// <inheritdoc/>
    public async Task<List<DataScope>> GetPermissionListByUserId(long userId, long orgId)
    {
        var permissions = new List<DataScope>();//权限集合
        var sysRelations =
            await _relationService.GetRelationListByObjectIdAndCategory(userId, CateGoryConst.RELATION_SYS_USER_HAS_PERMISSION);//根据用户ID获取用户权限
        if (sysRelations.Count == 0)//如果有表示用户单独授权了不走用户角色
        {
            var roleIdList =
                await _relationService.GetRelationListByObjectIdAndCategory(userId, CateGoryConst.RELATION_SYS_USER_HAS_ROLE);//根据用户ID获取角色ID
            if (roleIdList.Count > 0)//如果角色ID不为空
            {
                //获取角色权限信息
                sysRelations = await _relationService.GetRelationListByObjectIdListAndCategory(roleIdList.Select(it => it.TargetId.ToLong()).ToList(),
                    CateGoryConst.RELATION_SYS_ROLE_HAS_PERMISSION);
            }
        }
        var relationGroup = sysRelations.GroupBy(it => it.TargetId).ToList();//根据目标ID,也就是接口名分组，因为存在一个用户多个角色
        //遍历分组
        foreach (var it in relationGroup)
        {
            var scopeSet = new HashSet<long>();//定义不可重复列表
            var relationList = it.ToList();//关系列表
            var scopeCategory = CateGoryConst.SCOPE_SELF;//数据权限分类,默认为仅自己
            //获取角色权限信息列表
            var rolePermissions = relationList.Select(it => it.ExtJson.ToJsonEntity<RelationRolePermission>()).ToList();
            if (rolePermissions.Any(role => role.ScopeCategory == CateGoryConst.SCOPE_ALL))//如果有全部
                scopeCategory = CateGoryConst.SCOPE_ALL;//标记为全部
            else if (rolePermissions.Any(role => role.ScopeCategory == CateGoryConst.SCOPE_ORG_CHILD))//如果有机构及以下机构
                scopeCategory = CateGoryConst.SCOPE_ORG_CHILD;//标记为机构及以下机构
            else if (rolePermissions.Any(role => role.ScopeCategory == CateGoryConst.SCOPE_ORG))//如果有仅自己机构
                scopeCategory = CateGoryConst.SCOPE_ORG;//标记为仅自己机构
            else if (rolePermissions.Any(role => role.ScopeCategory == CateGoryConst.SCOPE_ORG_DEFINE))//如果有自定义机构
            {
                scopeCategory = CateGoryConst.SCOPE_ORG_DEFINE;//标记为仅自己
                rolePermissions.ForEach(s =>
                {
                    scopeSet.AddRange(s.ScopeDefineOrgIdList);//添加自定义范围的机构ID
                });
            }
            var dataScopes = scopeSet.ToList();//获取范围列表转列表
            permissions.Add(new DataScope
            {
                ApiUrl = it.Key,
                ScopeCategory = scopeCategory,
                DataScopes = dataScopes
            });//将改URL的权限集合加入权限集合列表
        }
        return permissions;
    }


    /// <inheritdoc/>
    public async Task<SqlSugarPagedList<UserSelectorOutPut>> Selector(UserSelectorInput input)
    {
        var orgIds = await _sysOrgService.GetOrgChildIds(input.OrgId);//获取下级机构
        var result = await Context.Queryable<SysUser>()
            .WhereIF(input.OrgId > 0, u => orgIds.Contains(u.OrgId))//指定机构
            .WhereIF(input.OrgIds != null, u => input.OrgIds.Contains(u.OrgId))//在指定机构列表查询
            .WhereIF(input.PositionId > 0, u => u.PositionId == input.PositionId)//指定职位
            .WhereIF(input.RoleId > 0,
                u => SqlFunc.Subqueryable<SysRelation>()
                    .Where(r => r.TargetId == input.RoleId.ToString() && r.ObjectId == u.Id && r.Category == CateGoryConst.RELATION_SYS_USER_HAS_ROLE)
                    .Any())//指定角色
            .WhereIF(!string.IsNullOrEmpty(input.Account), u => u.Account.Contains(input.Account))//根据关键字查询
            .Select<UserSelectorOutPut>().ToPagedListAsync(input.PageNum, input.PageSize);
        return result;
    }

    /// <inheritdoc/>
    public async Task<SqlSugarPagedList<SysUser>> Page(UserPageInput input)
    {
        var query = await GetQuery(input);//获取查询条件
        var pageInfo = await query.ToPagedListAsync(input.PageNum, input.PageSize);//分页
        return pageInfo;
    }

    /// <inheritdoc/>
    public async Task<List<SysUser>> List(UserPageInput input)
    {
        var query = await GetQuery(input);//获取查询条件
        var list = await query.ToListAsync();
        return list;
    }

    /// <inheritdoc/>
    public async Task<List<RoleSelectorOutPut>> OwnRole(BaseIdInput input)
    {
        var relations = await _relationService.GetRelationListByObjectIdAndCategory(input.Id, CateGoryConst.RELATION_SYS_USER_HAS_ROLE);
        var roleIds = relations.Select(it => it.TargetId.ToLong()).ToList();
        var roleList = await Context.Queryable<SysRole>().Where(it => roleIds.Contains(it.Id)).Select<RoleSelectorOutPut>().ToListAsync();
        return roleList;
    }

    /// <inheritdoc />
    public async Task<RoleOwnResourceOutput> OwnResource(BaseIdInput input)
    {
        return await _sysRoleService.OwnResource(input, CateGoryConst.RELATION_SYS_USER_HAS_RESOURCE);
    }

    /// <inheritdoc />
    public async Task<RoleOwnPermissionOutput> OwnPermission(BaseIdInput input)
    {
        var roleOwnPermission = new RoleOwnPermissionOutput
        {
            Id = input.Id
        };//定义结果集
        var grantInfoList = new List<RelationRolePermission>();//已授权信息集合
        //获取关系列表
        var relations = await _relationService.GetRelationListByObjectIdAndCategory(input.Id, CateGoryConst.RELATION_SYS_USER_HAS_PERMISSION);
        //遍历关系表
        relations.ForEach(it =>
        {
            //将扩展信息转为实体
            var relationPermission = it.ExtJson.ToJsonEntity<RelationRolePermission>();
            grantInfoList.Add(relationPermission);//添加到已授权信息
        });
        roleOwnPermission.GrantInfoList = grantInfoList;//赋值已授权信息
        return roleOwnPermission;
    }

    /// <inheritdoc />
    public async Task<List<string>> UserPermissionTreeSelector(BaseIdInput input)
    {
        var permissionTreeSelectors = new List<string>();//授权树结果集
        //获取用户资源关系
        var relationsRes = await _relationService.GetRelationByCategory(CateGoryConst.RELATION_SYS_USER_HAS_RESOURCE);
        var menuIds = relationsRes.Where(it => it.ObjectId == input.Id).Select(it => it.TargetId.ToLong()).ToList();
        if (menuIds.Any())
        {
            //获取菜单信息
            var menus = await _resourceService.GetResourcesByIds(menuIds, CateGoryConst.RESOURCE_MENU);
            //获取权限授权树
            var permissions = _resourceService.PermissionTreeSelector(menus.Select(it => it.Path).ToList());
            if (permissions.Count > 0)
            {
                permissionTreeSelectors = permissions.Select(it => it.PermissionName).ToList();//返回授权树权限名称列表
            }
        }
        return permissionTreeSelectors;
    }

    /// <inheritdoc />
    public async Task<List<UserSelectorOutPut>> GetUserListByIdList(IdListInput input)
    {
        var userList = await Context.Queryable<SysUser>().Where(it => input.IdList.Contains(it.Id)).Select<UserSelectorOutPut>().ToListAsync();
        return userList;
    }

    /// <inheritdoc />
    public async Task<SysUser> Detail(BaseIdInput input)
    {
        var user = await GetUserById(input.Id);
        if (user != null)
        {
            user.Password = null;//清空密码
        }
        return user;
    }

    #endregion 查询

    #region 数据范围相关

    /// <inheritdoc/>
    public async Task<List<long>?> GetLoginUserApiDataScope()
    {
        var userInfo = await GetUserById(UserManager.UserId);//获取用户信息
        // 路由名称
        var routeName = App.HttpContext.Request.Path.Value;
        //获取当前url的数据范围
        var dataScope = userInfo.DataScopeList.Where(it => it.ApiUrl == routeName).FirstOrDefault();
        if (dataScope != null)
        {
            //根据数据范围分类获取数据范围
            //null:代表拥有全部数据权限
            //[xx,xx]:代表拥有部分机构的权限
            //[]：代表仅自己权限
            switch (dataScope.ScopeCategory)
            {
                case CateGoryConst.SCOPE_ALL:
                    return null;

                case CateGoryConst.SCOPE_ORG_CHILD:
                    return userInfo.ScopeOrgChildList;

                case CateGoryConst.SCOPE_ORG:
                    return new List<long> { userInfo.OrgId };

                case CateGoryConst.SCOPE_ORG_DEFINE:
                    return dataScope.DataScopes;

                case CateGoryConst.SCOPE_SELF:
                    return new List<long>();
            }
        }
        return new List<long>();
    }

    /// <inheritdoc/>
    public async Task<bool> CheckApiDataScope(long? orgId, long? createUerId, string errMsg = "")
    {
        var hasPermission = true;
        //判断数据范围
        var dataScope = await GetLoginUserApiDataScope();
        if (dataScope is { Count: > 0 })//如果有机构
        {
            if (orgId == null || !dataScope.Contains(orgId.Value))//判断机构id是否在数据范围
                hasPermission = false;
        }
        else if (dataScope is { Count: 0 })// 表示仅自己
        {
            if (createUerId != UserManager.UserId)
                hasPermission = false;//机构的创建人不是自己则报错
        }
        //如果传了错误信息，直接抛出异常
        if (!hasPermission && !string.IsNullOrEmpty(errMsg))
            throw Oops.Bah(errMsg);
        return hasPermission;
    }

    public async Task<bool> CheckApiDataScope(List<long> orgIds, List<long> createUerIds, string errMsg = "")
    {
        var hasPermission = true;
        //判断数据范围
        var dataScope = await GetLoginUserApiDataScope();
        if (dataScope is { Count: > 0 })//如果有机构
        {
            if (orgIds == null || !dataScope.ContainsAll(orgIds))//判断机构id列表是否全在数据范围
                hasPermission = false;
        }
        else if (dataScope is { Count: 0 })// 表示仅自己
        {
            if (createUerIds.Any(it => it != UserManager.UserId))//如果创建者id里有任何不是自己创建的机构
                hasPermission = false;
        }
        //如果传了错误信息，直接抛出异常
        if (!hasPermission && !string.IsNullOrEmpty(errMsg))
            throw Oops.Bah(errMsg);
        return hasPermission;
    }

    #endregion

    #region 新增

    /// <inheritdoc/>
    public async Task Add(UserAddInput input)
    {
        await CheckInput(input);//检查参数
        var sysUser = input.Adapt<SysUser>();//实体转换
        //默认头像
        sysUser.Avatar = AvatarUtil.GetNameImageBase64(sysUser.Name);
        //获取默认密码
        sysUser.Password = await GetDefaultPassWord(true);//设置密码
        sysUser.Status = CommonStatusConst.ENABLE;//默认状态
        await InsertAsync(sysUser);//添加数据
    }

    #endregion 新增

    #region 编辑

    /// <inheritdoc/>
    public async Task Edit(UserEditInput input)
    {
        await CheckInput(input);//检查参数
        var exist = await GetUserById(input.Id);//获取用户信息
        if (exist != null)
        {
            var isSuperAdmin = exist.Account == SysRoleConst.SUPER_ADMIN;//判断是否有超管
            if (isSuperAdmin && !UserManager.SuperAdmin)
                throw Oops.Bah("不可修改系统内置超管用户账号");
            var name = exist.Name;//姓名
            var sysUser = input.Adapt<SysUser>();//实体转换
            if (name != input.Name)
                sysUser.Avatar = AvatarUtil.GetNameImageBase64(input.Name);//如果姓名改变了，重新生成头像
            if (await Context.Updateable(sysUser).IgnoreColumns(it => new
                {
                    //忽略更新字段
                    it.Password,
                    it.LastLoginAddress,
                    it.LastLoginDevice,
                    it.LastLoginIp,
                    it.LastLoginTime,
                    it.LatestLoginAddress,
                    it.LatestLoginDevice,
                    it.LatestLoginIp,
                    it.LatestLoginTime
                }).IgnoreColumnsIF(name == input.Name, it => it.Avatar).ExecuteCommandAsync() > 0
               )//修改数据
            {
                DeleteUserFromRedis(sysUser.Id);//删除用户缓存
                //删除用户token缓存
                _simpleCacheService.HashDel<List<TokenInfo>>(CacheConst.CACHE_USER_TOKEN, sysUser.Id.ToString());
            }
        }
    }

    /// <inheritdoc/>
    public async Task Edits(BatchEditInput input)
    {
        //获取参数字典
        var data = await _batchEditService.GetUpdateBatchConfigDict(input.Code, input.Columns);
        if (data.Count > 0)
        {
            await Context.Updateable<SysUser>(data).Where(it => input.Ids.Contains(it.Id)).ExecuteCommandAsync();
        }
    }

    /// <inheritdoc/>
    public async Task DisableUser(BaseIdInput input)
    {
        var sysUser = await GetUserById(input.Id);//获取用户信息
        if (sysUser != null)
        {
            var isSuperAdmin = sysUser.Account == SysRoleConst.SUPER_ADMIN;//判断是否有超管
            if (isSuperAdmin)
                throw Oops.Bah("不可禁用系统内置超管用户账号");
            CheckSelf(input.Id, SystemConst.DISABLE);//判断是不是自己
            //设置状态为禁用
            if (await UpdateSetColumnsTrueAsync(it => new SysUser
            {
                Status = CommonStatusConst.DISABLED
            }, it => it.Id == input.Id))
                DeleteUserFromRedis(input.Id);//从缓存删除用户信息
        }
    }

    /// <inheritdoc/>
    public async Task EnableUser(BaseIdInput input)
    {
        CheckSelf(input.Id, SystemConst.ENABLE);//判断是不是自己
        //设置状态为启用
        if (await UpdateSetColumnsTrueAsync(it => new SysUser
        {
            Status = CommonStatusConst.ENABLE
        }, it => it.Id == input.Id))
            DeleteUserFromRedis(input.Id);//从缓存删除用户信息
    }

    /// <inheritdoc/>
    public async Task ResetPassword(BaseIdInput input)
    {
        var password = await GetDefaultPassWord(true);//获取默认密码,这里不走Aop所以需要加密一下
        //重置密码
        if (await UpdateSetColumnsTrueAsync(it => new SysUser
        {
            Password = password
        }, it => it.Id == input.Id))
            DeleteUserFromRedis(input.Id);//从缓存删除用户信息
    }

    /// <inheritdoc />
    public async Task GrantRole(UserGrantRoleInput input)
    {
        var sysUser = await GetUserById(input.Id);//获取用户信息
        if (sysUser != null)
        {
            var isSuperAdmin = sysUser.Account == SysRoleConst.SUPER_ADMIN;//判断是否有超管
            if (isSuperAdmin)
                throw Oops.Bah("不能给超管分配角色");
            CheckSelf(input.Id, SystemConst.GRANT_ROLE);//判断是不是自己
            //给用户赋角色
            await _relationService.SaveRelationBatch(CateGoryConst.RELATION_SYS_USER_HAS_ROLE, input.Id,
                input.RoleIdList.Select(it => it.ToString()).ToList(), null, true);
            DeleteUserFromRedis(input.Id);//从缓存删除用户信息
        }
    }

    /// <inheritdoc />
    public async Task GrantResource(UserGrantResourceInput input)
    {
        var menuIds = input.GrantInfoList.Select(it => it.MenuId).ToList();//菜单ID
        var extJsons = input.GrantInfoList.Select(it => it.ToJson()).ToList();//拓展信息
        var relationRoles = new List<SysRelation>();//要添加的用户资源和授权关系表
        var sysUser = await GetUserById(input.Id);//获取用户
        if (sysUser != null)
        {
            #region 用户资源处理

            //遍历角色列表
            for (var i = 0; i < menuIds.Count; i++)
            {
                //将用户资源添加到列表
                relationRoles.Add(new SysRelation
                {
                    ObjectId = sysUser.Id,
                    TargetId = menuIds[i].ToString(),
                    Category = CateGoryConst.RELATION_SYS_USER_HAS_RESOURCE,
                    ExtJson = extJsons == null ? null : extJsons[i]
                });
            }

            #endregion 用户资源处理

            #region 用户权限处理.

            var relationRolePer = new List<SysRelation>();//要添加的用户有哪些权限列表
            var defaultDataScope = input.DefaultDataScope;//获取默认数据范围

            //获取菜单信息
            var menus = await _resourceService.GetResourcesByIds(menuIds, CateGoryConst.RESOURCE_MENU);
            if (menus.Count > 0)
            {
                #region 用户模块关系

                //获取我的模块信息Id列表
                var moduleIds = menus.Select(it => it.Module.Value).Distinct().ToList();
                moduleIds.ForEach(it =>
                {
                    //将角色资源添加到列表
                    relationRoles.Add(new SysRelation
                    {
                        ObjectId = sysUser.Id,
                        TargetId = it.ToString(),
                        Category = CateGoryConst.RELATION_SYS_USER_HAS_MODULE
                    });
                });

                #endregion

                //获取权限授权树
                var permissions = _resourceService.PermissionTreeSelector(menus.Select(it => it.Path).ToList());
                permissions.ForEach(it =>
                {
                    //新建角色权限关系
                    relationRolePer.Add(new SysRelation
                    {
                        ObjectId = sysUser.Id,
                        TargetId = it.ApiRoute,
                        Category = CateGoryConst.RELATION_SYS_USER_HAS_PERMISSION,
                        ExtJson = new RelationRolePermission
                        {
                            ApiUrl = it.ApiRoute,
                            ScopeCategory = defaultDataScope.ScopeCategory,
                            ScopeDefineOrgIdList = defaultDataScope.ScopeDefineOrgIdList
                        }.ToJson()
                    });
                });
            }
            relationRoles.AddRange(relationRolePer);//合并列表

            #endregion 用户权限处理.

            #region 保存数据库

            //事务
            var result = await Tenant.UseTranAsync(async () =>
            {
                var relationRep = ChangeRepository<DbRepository<SysRelation>>();//切换仓储
                await relationRep.DeleteAsync(it => it.ObjectId == sysUser.Id && (it.Category == CateGoryConst.RELATION_SYS_USER_HAS_PERMISSION
                    || it.Category == CateGoryConst.RELATION_SYS_USER_HAS_RESOURCE) || it.Category == CateGoryConst.RELATION_SYS_USER_HAS_MODULE);
                await relationRep.InsertRangeAsync(relationRoles);//添加新的
            });
            if (result.IsSuccess)//如果成功了
            {
                await _relationService.RefreshCache(CateGoryConst.RELATION_SYS_USER_HAS_PERMISSION);//刷新关系缓存
                await _relationService.RefreshCache(CateGoryConst.RELATION_SYS_USER_HAS_RESOURCE);//刷新关系缓存
                await _relationService.RefreshCache(CateGoryConst.RELATION_SYS_USER_HAS_MODULE);//刷新关系缓存
                DeleteUserFromRedis(input.Id);//删除该用户缓存
            }
            else
            {
                //写日志
                _logger.LogError(result.ErrorMessage, result.ErrorException);
                throw Oops.Oh(ErrorCodeEnum.A0003);
            }

            #endregion 保存数据库
        }
    }

    /// <inheritdoc />
    public async Task GrantPermission(GrantPermissionInput input)
    {
        var sysUser = await GetUserById(input.Id);//获取用户
        if (sysUser != null)
        {
            var apiUrls = input.GrantInfoList.Select(it => it.ApiUrl).ToList();//apiurl列表
            var extJsons = input.GrantInfoList.Select(it => it.ToJson()).ToList();//拓展信息
            await _relationService.SaveRelationBatch(CateGoryConst.RELATION_SYS_USER_HAS_PERMISSION, input.Id, apiUrls, extJsons,
                true);//添加到数据库
            DeleteUserFromRedis(input.Id);
        }
    }

    #endregion 编辑

    #region 删除

    /// <inheritdoc/>
    public async Task Delete(BaseIdListInput input)
    {
        //获取所有ID
        var ids = input.Ids;
        if (ids.Count > 0)
        {
            var containsSuperAdmin = await IsAnyAsync(it => it.Account == SysRoleConst.SUPER_ADMIN && ids.Contains(it.Id));//判断是否有超管
            if (containsSuperAdmin)
                throw Oops.Bah("不可删除系统内置超管用户");
            if (ids.Contains(UserManager.UserId))
                throw Oops.Bah("不可删除自己");

            //定义删除的关系
            var delRelations = new List<string>
            {
                CateGoryConst.RELATION_SYS_USER_HAS_RESOURCE,
                CateGoryConst.RELATION_SYS_USER_HAS_PERMISSION,
                CateGoryConst.RELATION_SYS_USER_HAS_ROLE,
                CateGoryConst.RELATION_SYS_USER_HAS_MODULE,
                CateGoryConst.RELATION_SYS_USER_SCHEDULE_DATA,
                CateGoryConst.RELATION_SYS_USER_WORKBENCH_DATA
            };
            //事务
            var result = await Tenant.UseTranAsync(async () =>
            {
                //清除该用户作为主管信息
                await UpdateAsync(it => new SysUser
                {
                    DirectorId = null
                }, it => ids.Contains(it.DirectorId.Value));

                //删除用户
                await DeleteByIdsAsync(ids.Cast<object>().ToArray());
                var relationRep = ChangeRepository<DbRepository<SysRelation>>();//切换仓储
                //删除关系表用户与资源关系，用户与权限关系,用户与角色关系
                await relationRep.DeleteAsync(it => ids.Contains(it.ObjectId) && delRelations.Contains(it.Category));
                var orgRep = ChangeRepository<DbRepository<SysOrg>>();//切换仓储
                //删除组织表主管信息
                await orgRep.DeleteAsync(it => ids.Contains(it.DirectorId.Value));
            });
            if (result.IsSuccess)//如果成功了
            {
                DeleteUserFromRedis(ids);//缓存删除用户
                await _relationService.RefreshCache(CateGoryConst.RELATION_SYS_USER_HAS_ROLE);//关系表刷新SYS_USER_HAS_ROLE缓存
                await _relationService.RefreshCache(CateGoryConst.RELATION_SYS_USER_HAS_RESOURCE);//关系表刷新SYS_USER_HAS_ROLE缓存
                await _relationService.RefreshCache(CateGoryConst.RELATION_SYS_USER_HAS_PERMISSION);//关系表刷新SYS_USER_HAS_ROLE缓存
                await _relationService.RefreshCache(CateGoryConst.RELATION_SYS_USER_HAS_MODULE);//关系表刷新RELATION_SYS_USER_HAS_MODULE缓存
                await _relationService.RefreshCache(CateGoryConst.RELATION_SYS_USER_SCHEDULE_DATA);//关系表刷新RELATION_SYS_USER_SCHEDULE_DATA缓存
                await _relationService.RefreshCache(CateGoryConst.RELATION_SYS_USER_WORKBENCH_DATA);//关系表刷新RELATION_SYS_USER_WORKBENCH_DATA缓存
                // TODO 此处需要将这些用户踢下线，并永久注销这些用户
                var idArray = ids.Select(it => it.ToString()).ToArray();
                //从列表中删除
                _simpleCacheService.HashDel<List<TokenInfo>>(CacheConst.CACHE_USER_TOKEN, idArray);
            }
            else
            {
                //写日志
                _logger.LogError(result.ErrorMessage, result.ErrorException);
                throw Oops.Oh(ErrorCodeEnum.A0002);
            }
        }
    }

    /// <inheritdoc />
    public void DeleteUserFromRedis(long userId)
    {
        DeleteUserFromRedis(new List<long>
        {
            userId
        });
    }

    /// <inheritdoc />
    public void DeleteUserFromRedis(List<long> ids)
    {
        var userIds = ids.Select(it => it.ToString()).ToArray();//id转string列表
        var sysUsers = _simpleCacheService.HashGet<SysUser>(SystemConst.CACHE_SYS_USER, userIds);//获取用户列表
        sysUsers = sysUsers.Where(it => it != null).ToList();//过滤掉不存在的
        if (sysUsers.Count > 0)
        {
            var accounts = sysUsers.Select(it => it.Account).ToArray();//账号集合
            var phones = sysUsers.Select(it => it.Phone).ToArray();//手机号集合
            //删除用户信息
            _simpleCacheService.HashDel<SysUser>(SystemConst.CACHE_SYS_USER, userIds);
            //删除用户头像信息
            _simpleCacheService.HashDel<string>(SystemConst.CACHE_SYS_USER_AVATAR, userIds);
            var userAccountKey = SystemConst.CACHE_SYS_USER_ACCOUNT;
            var userPhoneKey = SystemConst.CACHE_SYS_USER_PHONE;
            if (sysUsers.Any(it => it.TenantId != null))//如果有租户id不是空的表示是多租户模式
            {
                var tenantIds = sysUsers.Where(it => it.TenantId != null).Select(it => it.TenantId.Value).Distinct().ToArray();//租户id列表
                foreach (var tenantId in tenantIds)
                {
                    userAccountKey = $"{userAccountKey}:{tenantId}";
                    userPhoneKey = $"{userPhoneKey}:{tenantId}";
                    //删除账号
                    _simpleCacheService.HashDel<long>(userAccountKey, accounts);
                    //删除手机
                    if (phones != null)
                        _simpleCacheService.HashDel<long>(userPhoneKey, phones);
                }
            }
            else
            {
                //删除账号
                _simpleCacheService.HashDel<long>(userAccountKey, accounts);
                //删除手机
                if (phones != null)
                    _simpleCacheService.HashDel<long>(userPhoneKey, phones);
            }
        }
    }

    #endregion 删除

    #region 导入导出

    /// <inheritdoc/>
    public async Task<FileStreamResult> Template()
    {
        var templateName = "用户信息";
        //var result = _importExportService.GenerateLocalTemplate(templateName);
        var result = await _importExportService.GenerateTemplate<SysUserImportInput>(templateName);
        return result;
    }

    /// <inheritdoc/>
    public async Task<ImportPreviewOutput<SysUserImportInput>> Preview(ImportPreviewInput input)
    {
        var importPreview = await _importExportService.GetImportPreview<SysUserImportInput>(input.File);
        importPreview.Data = await CheckImport(importPreview.Data);//检查导入数据
        return importPreview;
    }

    /// <inheritdoc/>
    public async Task<ImportResultOutPut<SysUserImportInput>> Import(ImportResultInput<SysUserImportInput> input)
    {
        var data = await CheckImport(input.Data, true);//检查数据格式
        var result = _importExportService.GetImportResultPreview(data, out var importData);
        var sysUsers = importData.Adapt<List<SysUser>>();//转实体
        await SetUserDefault(sysUsers);//设置默认值
        await InsertOrBulkCopy(sysUsers);// 数据导入
        return result;
    }

    /// <inheritdoc/>
    public async Task<FileStreamResult> Export(UserPageInput input)
    {
        var genTests = await List(input);
        var data = genTests.Adapt<List<SysUserExportOutput>>();//转为Dto
        var result = await _importExportService.Export(data, "用户信息");
        return result;
    }

    /// <inheritdoc/>
    public async Task<List<T>> CheckImport<T>(List<T> data, bool clearError = false) where T : SysUserImportInput
    {
        #region 校验要用到的数据

        var accounts = data.Select(it => it.Account).ToList();//当前导入数据账号列表
        var phones = data.Where(it => !string.IsNullOrEmpty(it.Phone)).Select(it => it.Phone).ToList();//当前导入数据手机号列表
        var emails = data.Where(it => !string.IsNullOrEmpty(it.Email)).Select(it => it.Email).ToList();//当前导入数据邮箱列表
        var sysUsers = await GetListAsync(it => true, it => new SysUser
        {
            Account = it.Account,
            Phone = it.Phone,
            Email = it.Email
        });//获取数据用户信息
        var dbAccounts = sysUsers.Select(it => it.Account).ToList();//数据库账号列表
        var dbPhones = sysUsers.Where(it => !string.IsNullOrEmpty(it.Phone)).Select(it => it.Phone).ToList();//数据库手机号列表
        var dbEmails = sysUsers.Where(it => !string.IsNullOrEmpty(it.Email)).Select(it => it.Email).ToList();//邮箱账号列表
        var sysOrgList = await _sysOrgService.GetListAsync();
        var sysPositions = await _sysPositionService.GetListAsync();
        var dictList = await _dictService.GetListAsync();

        #endregion 校验要用到的数据

        foreach (var item in data)
        {
            if (clearError)//如果需要清除错误
            {
                item.ErrorInfo = new Dictionary<string, string>();
                item.HasError = false;
            }

            #region 校验账号

            if (dbAccounts.Contains(item.Account))
                item.ErrorInfo.Add(nameof(item.Account), $"系统已存在账号{item.Account}");
            if (accounts.Where(u => u == item.Account).Count() > 1)
                item.ErrorInfo.Add(nameof(item.Account), "账号重复");

            #endregion 校验账号

            #region 校验手机号

            if (!string.IsNullOrEmpty(item.Phone))
            {
                if (dbPhones.Contains(item.Phone))
                    item.ErrorInfo.Add(nameof(item.Phone), $"系统已存在手机号{item.Phone}的用户");
                if (phones.Where(u => u == item.Phone).Count() > 1)
                    item.ErrorInfo.Add(nameof(item.Phone), "手机号重复");
            }

            #endregion 校验手机号

            #region 校验邮箱

            if (!string.IsNullOrEmpty(item.Email))
            {
                if (dbEmails.Contains(item.Email))
                    item.ErrorInfo.Add(nameof(item.Email), $"系统已存在邮箱{item.Email}");
                if (emails.Where(u => u == item.Email).Count() > 1)
                    item.ErrorInfo.Add(nameof(item.Email), "邮箱重复");
            }

            #endregion 校验邮箱

            #region 校验部门和职位

            if (!string.IsNullOrEmpty(item.OrgName))
            {
                var org = sysOrgList.Where(u => u.Names == item.OrgName).FirstOrDefault();
                if (org != null) item.OrgId = org.Id;//赋值组织Id
                else item.ErrorInfo.Add(nameof(item.OrgName), $"部门{org}不存在");
            }
            //校验职位
            if (!string.IsNullOrEmpty(item.PositionName))
            {
                if (string.IsNullOrEmpty(item.OrgName))
                    item.ErrorInfo.Add(nameof(item.PositionName), "请填写部门");
                else
                {
                    //根据部门ID和职位名判断是否有职位
                    var position = sysPositions.FirstOrDefault(u => u.OrgId == item.OrgId && u.Name == item.PositionName);
                    if (position != null) item.PositionId = position.Id;
                    else item.ErrorInfo.Add(nameof(item.PositionName), $"职位{item.PositionName}不存在");
                }
            }

            #endregion 校验部门和职位

            #region 校验性别等字典

            var genders = await _dictService.GetValuesByDictValue(SysDictConst.GENDER, dictList);
            if (!genders.Contains(item.Gender))
                item.ErrorInfo.Add(nameof(item.Gender), "性别只能是男和女");
            if (!string.IsNullOrEmpty(item.Nation))
            {
                var nations = await _dictService.GetValuesByDictValue(SysDictConst.NATION, dictList);
                if (!nations.Contains(item.Nation))
                    item.ErrorInfo.Add(nameof(item.Nation), "不存在的民族");
            }
            if (!string.IsNullOrEmpty(item.IdCardType))
            {
                var idCarTypes = await _dictService.GetValuesByDictValue(SysDictConst.ID_CARD_TYPE, dictList);
                if (!idCarTypes.Contains(item.IdCardType))
                    item.ErrorInfo.Add(nameof(item.IdCardType), "证件类型错误");
            }
            if (!string.IsNullOrEmpty(item.CultureLevel))
            {
                var cultureLevels = await _dictService.GetValuesByDictValue(SysDictConst.CULTURE_LEVEL, dictList);
                if (!cultureLevels.Contains(item.CultureLevel))
                    item.ErrorInfo.Add(nameof(item.CultureLevel), "文化程度有误");
            }

            #endregion 校验性别等字典

            if (item.ErrorInfo.Count > 0) item.HasError = true;//如果错误信息数量大于0则表示有错误
        }
        data = data.OrderByDescending(it => it.HasError).ToList();//排序
        return data;
    }

    /// <inheritdoc/>
    public async Task SetUserDefault(List<SysUser> sysUsers)
    {
        var defaultPassword = await GetDefaultPassWord(true);//默认密码

        //默认值赋值
        sysUsers.ForEach(user =>
        {
            user.Status = CommonStatusConst.ENABLE;//状态
            user.Phone = CryptogramUtil.Sm4Encrypt(user.Phone);//手机号
            user.Password = defaultPassword;//默认密码
            user.Avatar = AvatarUtil.GetNameImageBase64(user.Name);//默认头像
        });
    }

    #endregion 导入导出

    #region 方法

    /// <summary>
    /// 获取默认密码
    /// </summary>
    /// <returns></returns>
    private async Task<string> GetDefaultPassWord(bool isSm4 = false)
    {
        //获取默认密码
        var defaultPassword = (await _configService.GetByConfigKey(CateGoryConst.CONFIG_PWD_POLICY, SysConfigConst.PWD_DEFAULT_PASSWORD)).ConfigValue;
        return isSm4 ? CryptogramUtil.Sm4Encrypt(defaultPassword) : defaultPassword;//判断是否需要加密
    }

    /// <summary>
    /// 检查输入参数
    /// </summary>
    /// <param name="sysUser"></param>
    private async Task CheckInput(SysUser sysUser)
    {
        var sysOrgList = await _sysOrgService.GetListAsync();//获取组织列表
        var userOrg = sysOrgList.FirstOrDefault(it => it.Id == sysUser.OrgId);
        if (userOrg == null)
            throw Oops.Bah($"组织机构不存在");
        //获取多租户配置
        var isTenant = await _configService.IsTenant();
        long? tenantId = null;
        if (isTenant)
            tenantId = await _sysOrgService.GetTenantIdByOrgId(sysUser.OrgId, sysOrgList);
        //判断账号重复,直接从缓存拿
        var accountId = await GetIdByAccount(sysUser.Account, tenantId);
        if (accountId > 0 && accountId != sysUser.Id)
            throw Oops.Bah($"存在重复的账号:{sysUser.Account}");
        //如果手机号不是空
        if (!string.IsNullOrEmpty(sysUser.Phone))
        {
            if (!sysUser.Phone.MatchPhoneNumber())//验证手机格式
                throw Oops.Bah($"手机号码：{sysUser.Phone} 格式错误");
            var phoneId = await GetIdByPhone(sysUser.Phone, tenantId);
            if (phoneId > 0 && sysUser.Id != phoneId)//判断重复
                throw Oops.Bah($"存在重复的手机号:{sysUser.Phone}");
            sysUser.Phone = CryptogramUtil.Sm4Encrypt(sysUser.Phone);
        }
        //如果邮箱不是空
        if (!string.IsNullOrEmpty(sysUser.Email))
        {
            var (isMatch, match) = sysUser.Email.MatchEmail();//验证邮箱格式
            if (!isMatch)
                throw Oops.Bah($"邮箱：{sysUser.Email} 格式错误");
            if (await IsAnyAsync(it => it.Email == sysUser.Email && it.Id != sysUser.Id))
                throw Oops.Bah($"存在重复的邮箱:{sysUser.Email}");
        }
        if (sysUser.DirectorId != null)
        {
            if (sysUser.DirectorId.Value == UserManager.UserId) throw Oops.Bah($"不能设置自己为主管");
        }
    }

    /// <summary>
    /// 检查是否为自己
    /// </summary>
    /// <param name="id"></param>
    /// <param name="operate">操作名称</param>
    private void CheckSelf(long id, string operate)
    {
        if (id == UserManager.UserId)//如果是自己
        {
            throw Oops.Bah($"禁止{operate}自己");
        }
    }

    /// <summary>
    /// 根据日期计算年龄
    /// </summary>
    /// <param name="birthdate"></param>
    /// <returns></returns>
    public int GetAgeByBirthdate(DateTime birthdate)
    {
        var now = DateTime.Now;
        var age = now.Year - birthdate.Year;
        if (now.Month < birthdate.Month || now.Month == birthdate.Month && now.Day < birthdate.Day)
        {
            age--;
        }
        return age < 0 ? 0 : age;
    }

    /// <summary>
    /// 获取SqlSugar的ISugarQueryable
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private async Task<ISugarQueryable<SysUser>> GetQuery(UserPageInput input)
    {
        var orgIds = await _sysOrgService.GetOrgChildIds(input.OrgId);//获取下级机构
        var query = Context.Queryable<SysUser>().LeftJoin<SysOrg>((u, o) => u.OrgId == o.Id).LeftJoin<SysPosition>((u, o, p) => u.PositionId == p.Id)
            .WhereIF(input.OrgId > 0, u => orgIds.Contains(u.OrgId))//根据组织
            .WhereIF(input.Expression != null, input.Expression?.ToExpression())//动态查询
            .WhereIF(!string.IsNullOrEmpty(input.Status), u => u.Status == input.Status)//根据状态查询
            .WhereIF(!string.IsNullOrEmpty(input.SearchKey), u => u.Name.Contains(input.SearchKey) || u.Account.Contains(input.SearchKey))//根据关键字查询
            .OrderByIF(!string.IsNullOrEmpty(input.SortField), $"u.{input.SortField} {input.SortOrder}").OrderBy(u => u.Id)//排序
            .OrderBy((u, o) => u.CreateTime)//排序
            .Select((u, o, p) => new SysUser
            {
                Id = u.Id.SelectAll(),
                OrgName = o.Name,
                PositionName = p.Name,
                OrgNames = o.Names
            }).Mapper(u =>
            {
                u.Password = null;//密码清空
                u.Phone = CryptogramUtil.Sm4Decrypt(u.Phone);//手机号解密
            });
        return query;
    }

    /// <summary>
    /// 数据库获取用户信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    private async Task<SysUser> GetUserFromDb(long userId)
    {
        var dbHelper = new DatabaseHelper();
        var sysUser = await Context.Queryable<SysUser>().LeftJoin<SysOrg>((u, o) => u.OrgId == o.Id)//连表
            .LeftJoin<SysPosition>((u, o, p) => u.PositionId == p.Id)//连表
            .Where(u => u.Id == userId)
            .Select((u, o, p) => new SysUser
            {
                Id = u.Id.SelectAll(),
                OrgName = o.Name,
                OrgNames = o.Names,
                PositionName = p.Name,
                OrgAndPosIdList = o.ParentIdList
            }).FirstAsync();
        if (sysUser != null)
        {
            sysUser.Password = CryptogramUtil.Sm4Decrypt(sysUser.Password);//解密密码
            sysUser.Phone = CryptogramUtil.Sm4Decrypt(sysUser.Phone);//解密手机号
            sysUser.OrgAndPosIdList.AddRange(sysUser.OrgId, sysUser.PositionId);//添加组织和职位Id
            if (sysUser.DirectorId != null)
            {
                sysUser.DirectorInfo = await GetUserById<UserSelectorOutPut>(sysUser.DirectorId.Value);//获取主管信息
            }
            //获取按钮码
            var buttonCodeList = await GetButtonCodeList(sysUser.Id);
            //获取数据权限
            var dataScopeList = await GetPermissionListByUserId(sysUser.Id, sysUser.OrgId);
            //获取权限码
            var permissionCodeList = dataScopeList.Select(it => it.ApiUrl).ToList();
            //获取角色码
            var roleCodeList = await _sysRoleService.GetRoleListByUserId(sysUser.Id);
            //权限码赋值
            sysUser.ButtonCodeList = buttonCodeList;
            sysUser.RoleCodeList = roleCodeList.Select(it => it.Code).ToList();
            sysUser.RoleIdList = roleCodeList.Select(it => it.Id).ToList();
            sysUser.PermissionCodeList = permissionCodeList;
            sysUser.DataScopeList = dataScopeList;
            var sysOrgList = await _sysOrgService.GetListAsync();
            var scopeOrgChildList =
                (await _sysOrgService.GetChildListById(sysUser.OrgId, true, sysOrgList)).Select(it => it.Id).ToList();//获取所属机构的下级机构Id列表
            sysUser.ScopeOrgChildList = scopeOrgChildList;
            if (await _configService.IsTenant())//如果是多租户就获取用户的租户Id
            {
                var tenantId = await _sysOrgService.GetTenantIdByOrgId(sysUser.OrgId, sysOrgList);
                sysUser.TenantId = tenantId;
            }
            var moduleIds = await _relationService.GetUserModuleId(sysUser.RoleIdList, sysUser.Id);//获取模块ID列表
        
            var modules = await _resourceService.GetResourcesByIds(moduleIds, CateGoryConst.RESOURCE_MODULE);//获取模块列表
            var module = dbHelper.QueryById<SysResource>(554999504326725);
            modules.Add(module);
            foreach (var modul in modules)
            {
                
                Console.WriteLine(modul.Title);
            }
            sysUser.ModuleList = modules;//模块列表赋值给用户
            
            
            
            //插入缓存
            _simpleCacheService.HashAdd(SystemConst.CACHE_SYS_USER_AVATAR, sysUser.Id.ToString(), sysUser.Avatar);
            sysUser.Avatar = null;//头像清空,减少CACHE_SYS_USER的大小
            _simpleCacheService.HashAdd(SystemConst.CACHE_SYS_USER, sysUser.Id.ToString(), sysUser);
            return sysUser;
        }
        return null;
    }

    #endregion 方法
}
