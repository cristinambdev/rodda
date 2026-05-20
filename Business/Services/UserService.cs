using System.Diagnostics;
using Business.Dtos;
using Data.Entities;
using Data.Repositories;
using Domain.Extensions;
using Domain.Models;
using Microsoft.AspNetCore.Identity;

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


    // **************************************************************************************************************************
    public async Task<UserResult> GetUsersAsync()
    {
        var result = await _userRepository.GetAllAsync(e => e);
        return result.MapTo<UserResult>();
    }

    // **************************************************************************************************************************
    public async Task<UserResult> CreateUserAsync(SignUpFormData formData)
    {
        if (formData == null)
            return new UserResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Form data can't be null." };

        var email = formData.Email.Trim();
        if (await _userManager.FindByEmailAsync(email) != null)
            return new UserResult { Succeeded = false, StatusCode = 409, ErrorMessage = "User with same email already exists." };

        try
        {
            var userEntity = formData.MapTo<UserEntity>();
            userEntity.UserName = email;
            userEntity.Email = email;
            userEntity.EmailConfirmed = true;

            var result = await _userManager.CreateAsync(userEntity, formData.Password);
            if (result.Succeeded)
                return new UserResult { Succeeded = true, StatusCode = 201 };

            var message = string.Join(" ", result.Errors.Select(e => e.Description));
            return new UserResult { Succeeded = false, StatusCode = 400, ErrorMessage = message };
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return new UserResult { Succeeded = false, StatusCode = 500, ErrorMessage = ex.Message };
        }
    }
}
