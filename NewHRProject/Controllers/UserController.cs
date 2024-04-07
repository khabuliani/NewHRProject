using Microsoft.AspNetCore.Mvc;
using NewHRProject.Services;

namespace NewHRProject.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class UserController : ControllerBase
{
    private readonly IUserService _iUserService;
    public UserController(IUserService iUserService)
    {
        _iUserService = iUserService;
    }
}
