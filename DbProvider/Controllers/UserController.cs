using DbProvider.Helpers;
using DbProvider.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using Newtonsoft.Json.Linq;

namespace DbProvider.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController: ControllerBase
{
    private readonly UserRepositoryService _userService;
    public UserController()
    {
        DbConfig dbConfig = new DbConfig();
        _userService = new UserRepositoryService(dbConfig);
    }
    [HttpGet("GetUser")]
    [SwaggerOperation(
        Summary = "Информация о пользователе",
        Description = "Этот метод получает информацию о пользователе из БД.")]
    public ActionResult<UserModel> GetUser([FromQuery] string login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return BadRequest("Login is required.");
        }
        UserModel? user = _userService.GetUser(login);
        if (user == null)
        {
            return NotFound("User not found.");
        }
        return Ok(user);
    }
    [HttpGet("GetSessionUser")]
    [SwaggerOperation(
        Summary = "Сессия пользователя",
        Description = "Этот метод получает информацию о сессии пользователя из БД.")]
    public ActionResult<SessionModel> GetSessionUser([FromQuery] Guid refreshTokenJti)
    {
        if (Guid.Empty.Equals(refreshTokenJti))
        {
            return BadRequest("No valid data.");
        }

        SessionModel? session = _userService.GetSessionUser(refreshTokenJti);
        if (session == null)
        {
            return NotFound("Session not found.");
        }
        return Ok(session);
    }

    [HttpPost("AddSessionUser")]
    [SwaggerOperation(
        Summary = "Добавить сессию пользователя",
        Description = "Этот метод добовляет информацию о сессии пользователя в БД.")]
    public ActionResult<SessionModel> AddSessionUser([FromBody] UserModel? data)
    {
        if (data == null)
        {
            return BadRequest("No valid data.");
        }
        return _userService.AddSessionUser(data)
            ? Ok() 
            : BadRequest("Invalid data or failed to add session.");
    }
    [HttpPut("UpdateSessionUser")]
    [SwaggerOperation(
        Summary = "Обновить сессию пользователя",
        Description = "Этот метод обновляет информацию о сессии пользователя в БД.")]
    public ActionResult<SessionModel> UpdateSessionUser([FromBody] Dictionary<string, object>? data)
    {
        if (data == null || !data.ContainsKey("User") || !data.ContainsKey("OldRefreshTokenJti"))
        {
            return BadRequest("No valid data.");
        }
        var userJson = data["User"].ToString();
        var userModel = JsonConvert.DeserializeObject<UserModel>(userJson ?? string.Empty);
        var oldRefreshTokenJti = Guid.Parse(data["OldRefreshTokenJti"].ToString()!);
        // Передача параметров в сервис
        return _userService.UpdateSessionUser(userModel, oldRefreshTokenJti)
            ? Ok() 
            : BadRequest("Invalid data or failed to update session.");
    }
    [HttpDelete("DeleteSessionUser")]
    [SwaggerOperation(
        Summary = "Удалить сессию пользователя",
        Description = "Этот метод удаляет информацию о сессии пользователя в БД.")]
    public ActionResult DeleteSessionUser([FromQuery] Guid userId, [FromQuery] Guid sessionId)
    {
        var result = _userService.DeleteSessionUser(userId, sessionId);
        return result ? Ok() : BadRequest("Invalid data or failed to delete session.");
    }
}