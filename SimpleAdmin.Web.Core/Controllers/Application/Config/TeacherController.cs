
using System.Linq.Expressions;

namespace SimpleAdmin.Web.Core.Controllers.Config;

[ApiDescriptionSettings(Tag = "教师管理")]
[Route("biz/config/teacher")]
public class TeacherController
{
    [Route("getTeachers")]
    [HttpGet]
    public JsonResult getTeachers()
    {
        return new JsonResult("");
    }
}