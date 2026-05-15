namespace Business.Dtos;

public class AuthResult : ServiceResult
{
}

public class AuthResult<T> : ServiceResult
{
    public T? Result { get; set; }
}