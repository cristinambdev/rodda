using Business.Dtos;
using Data.Entities;
using Domain.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public interface IAuthService
{
    Task<AuthResult> SignInAsync(SignInFormData formData);
    Task<AuthResult> SignUpAsync(SignUpFormData formData);
    Task<AuthResult> SignOutAsync();
}

public class AuthService(IUserService userService, SignInManager<UserEntity> signInManager, UserManager<UserEntity> userManager) : IAuthService
{
    private readonly IUserService _userService = userService;
    private readonly SignInManager<UserEntity> _signInManager = signInManager;
    private readonly UserManager<UserEntity> _userManager = userManager;

    // **************************************************************************************************************************
    public async Task<AuthResult> SignInAsync(SignInFormData formData)
    {
        if (formData == null)
            return new AuthResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Not all required fields are supplied." };

        var email = formData.Email.Trim();
        var password = formData.Password;

        var user = await FindUserByEmailAsync(email);
        if (user == null)
            return new AuthResult { Succeeded = false, StatusCode = 401, ErrorMessage = "Invalid email or password." };

        await RepairIdentityFieldsAsync(user, email);

        if (!await _userManager.CheckPasswordAsync(user, password))
            return new AuthResult { Succeeded = false, StatusCode = 401, ErrorMessage = "Invalid email or password." };

        await _signInManager.SignInAsync( user,
            new AuthenticationProperties { IsPersistent = formData.IsPersistent });

        return new AuthResult { Succeeded = true, StatusCode = 200 };
    }

    // **************************************************************************************************************************
    public async Task<AuthResult> SignUpAsync(SignUpFormData formData)
    {
        if (formData == null)
            return new AuthResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Not all required fields are supplied." };

        var result = await _userService.CreateUserAsync(formData);
        if (!result.Succeeded)
            return new AuthResult { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };

        return await SignInAsync(new SignInFormData
        {
            Email = formData.Email.Trim(),
            Password = formData.Password,
            IsPersistent = false
        });
    }

    // **************************************************************************************************************************
    public async Task<AuthResult> SignOutAsync()
    {
        await _signInManager.SignOutAsync();
        return new AuthResult { Succeeded = true, StatusCode = 200 };
    }

    // **************************************************************************************************************************
    private async Task<UserEntity?> FindUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
            return user;

        return await _userManager.Users
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
    }

    // **************************************************************************************************************************
    private async Task RepairIdentityFieldsAsync(UserEntity user, string email)
    {
        if (string.IsNullOrWhiteSpace(user.UserName))
            await _userManager.SetUserNameAsync(user, email);

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        if (string.IsNullOrWhiteSpace(user.NormalizedEmail) && !string.IsNullOrWhiteSpace(user.Email))
            await _userManager.SetEmailAsync(user, user.Email);
    }
}
