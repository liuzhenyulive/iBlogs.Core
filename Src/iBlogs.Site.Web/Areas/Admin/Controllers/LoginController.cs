﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using iBlogs.Site.Core.Common.DTO;
using iBlogs.Site.Core.Common.Response;
using iBlogs.Site.Core.Security;
using iBlogs.Site.Core.Security.DTO;
using iBlogs.Site.Core.Security.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ClaimTypes = iBlogs.Site.Core.Common.ClaimTypes;

namespace iBlogs.Site.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;

        public LoginController(IConfiguration configuration, IUserService userService)
        {
            _configuration = configuration;
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ApiResponse<string> Index(LoginParam loginParam)
        {
            var user = _userService.FindUsers(new Users
            {
                Username = loginParam.Username,
                Password = loginParam.Password
            }).FirstOrDefault();
            if (user != null)
                return ApiResponse<string>.Ok(GenerateJsonWebToken(user));
            return ApiResponse<string>.Fail("登录失败");
        }

        private string GenerateJsonWebToken(Users user)
        {
            var token = Guid.NewGuid().ToString();
            var claims = new[]
            {
                new Claim(ClaimTypes.Uid, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Token,token), 
            };

            LoginToken.SaveToken(user.Id,token);

            var days = _configuration["JwtExpireDays"];
            var key = _configuration["JwtKey"];
            var issuer = _configuration["JwtIssuer"];

            var expires = DateTime.UtcNow.AddDays(Convert.ToDouble(days));

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var jwtSecurityToken = new JwtSecurityToken
            (
                issuer,
                issuer,
                claims,
                expires: expires,
                signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
            );
            return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        }
    }
}