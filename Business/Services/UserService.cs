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
    Task<string> GetDisplayNameAsync(string? userName);
    Task<string> GetDisplayEmailAsync(string? userEmail);
    Task<UserProfile?> GetUserProfileAsync(string? userName);
    Task<UserResult> UpdateUserAsync(UpdateUserFormData formData);
    Task<UserResult> DeleteUserAsync(string? userName);
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

    // **************************************************************************************************************************
    public async Task<string> GetDisplayNameAsync(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return "";

        var user = await _userManager.FindByNameAsync(userName);
        return user == null ? "" : user.DisplayName;
    }

    // **************************************************************************************************************************
    public async Task<string> GetDisplayEmailAsync(string? userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return "";

        var user = await _userManager.FindByEmailAsync(userEmail);
        if (user != null)
            return user.Email ?? "";

        user = await _userManager.FindByNameAsync(userEmail);
        return user?.Email ?? "";
    }

    // **************************************************************************************************************************
    public async Task<UserProfile?> GetUserProfileAsync(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return null;

        var user = await _userManager.FindByNameAsync(userName.Trim());
        if (user == null)
            return null;

        return new UserProfile
        {
            DisplayName = user.DisplayName ?? "",
            Email = user.Email ?? "",
            ProfileImageUrl = user.ProfileImageUrl,
        };
    }

    // **************************************************************************************************************************
    public async Task<UserResult> UpdateUserAsync(UpdateUserFormData formData)
    {
        if (formData == null)
            return new UserResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Form data can't be null." };

        var user = await _userManager.FindByNameAsync(formData.UserName.Trim());
        if (user == null)
            return new UserResult { Succeeded = false, StatusCode = 404, ErrorMessage = "User not found." };

        try
        {
            if (!string.IsNullOrWhiteSpace(formData.DisplayName))
                user.DisplayName = formData.DisplayName.Trim();

            if (formData.ProfileImageUrl != null)
                user.ProfileImageUrl = string.IsNullOrWhiteSpace(formData.ProfileImageUrl)
                    ? null
                    : formData.ProfileImageUrl.Trim();

            if (!string.IsNullOrWhiteSpace(formData.Email))
            {
                var email = formData.Email.Trim();
                var existing = await _userManager.FindByEmailAsync(email);
                if (existing != null && existing.Id != user.Id)
                    return new UserResult { Succeeded = false, StatusCode = 409, ErrorMessage = "Another account already uses this email." };

                var emailResult = await _userManager.SetEmailAsync(user, email);
                if (!emailResult.Succeeded)
                {
                    var message = string.Join(" ", emailResult.Errors.Select(e => e.Description));
                    return new UserResult { Succeeded = false, StatusCode = 400, ErrorMessage = message };
                }

                await _userManager.SetUserNameAsync(user, email);
            }

            if (!string.IsNullOrWhiteSpace(formData.NewPassword))
            {
                var hasPassword = await _userManager.HasPasswordAsync(user);
                IdentityResult passwordResult;

                if (hasPassword)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    passwordResult = await _userManager.ResetPasswordAsync(user, token, formData.NewPassword);
                }
                else
                    passwordResult = await _userManager.AddPasswordAsync(user, formData.NewPassword);

                if (!passwordResult.Succeeded)
                {
                    var message = string.Join(" ", passwordResult.Errors.Select(e => e.Description));
                    return new UserResult { Succeeded = false, StatusCode = 400, ErrorMessage = message };
                }
            }

            user.UpdatedAt = DateTime.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var message = string.Join(" ", updateResult.Errors.Select(e => e.Description));
                return new UserResult { Succeeded = false, StatusCode = 400, ErrorMessage = message };
            }

            return new UserResult { Succeeded = true, StatusCode = 200 };
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return new UserResult { Succeeded = false, StatusCode = 500, ErrorMessage = ex.Message };
        }
    }

    // **************************************************************************************************************************
    public async Task<UserResult> DeleteUserAsync(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return new UserResult { Succeeded = false, StatusCode = 400, ErrorMessage = "User name is required." };

        var user = await _userManager.FindByNameAsync(userName.Trim());
        if (user == null)
            return new UserResult { Succeeded = false, StatusCode = 404, ErrorMessage = "User not found." };

        try
        {
            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                var message = string.Join(" ", deleteResult.Errors.Select(e => e.Description));
                return new UserResult { Succeeded = false, StatusCode = 400, ErrorMessage = message };
            }

            return new UserResult { Succeeded = true, StatusCode = 200 };
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return new UserResult { Succeeded = false, StatusCode = 500, ErrorMessage = ex.Message };
        }
    }
}
