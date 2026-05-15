using Business.Dtos;
using Data.Entities;
using Data.Repositories;
using Domain.Extensions;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;

namespace Business.Services;

public interface IUserService
{
    Task<UserResult> GetUsersAsync();
    Task<UserResult> CreateUserAsync(SignUpFormData formData);
}

public class UserService(IUserRepository userRepository, UserManager<UserEntity> userManager) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly UserManager<UserEntity> _userManager = userManager;


    public async Task<UserResult> GetUsersAsync()
    {
        var result = await _userRepository.GetAllAsync(e => e);
        return result.MapTo<UserResult>();
    }

    public async Task<UserResult> CreateUserAsync(SignUpFormData formData)
    {
        if (formData == null)
            return new UserResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Not all required fields are supplied." };
        var existsResult = await _userRepository.ExistsAsync(u => u.Email == formData.Email);
        if (existsResult.Succeeded)
            return new UserResult { Succeeded = false, StatusCode = 409, ErrorMessage = "User with this emaail already exists." };


        try
        {
            var userEntity = formData.MapTo<UserEntity>();
            var result = await _userManager.CreateAsync(userEntity, formData.Password);
            return result.Succeeded
                ? new UserResult { Succeeded = false, StatusCode = 409, ErrorMessage = "Unable to create user." }
                : new UserResult { Succeeded = true, StatusCode = 201 };
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return new UserResult { Succeeded = false, StatusCode = 500, ErrorMessage = ex.Message };
        }

    }
}
