using User;

namespace api.Model
{
    public class UserResponse
    {
        public class UserAddResponseData
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
        }
        public class AddUserRequest { 
            public string Name { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public UserRole? Role { get; set; } = UserRole.Undefiened;
            public string Email { get; set; } = string.Empty;
        }
        public class GetUserResponse
        {

            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;
            public UserRole? Role { get; set; } = UserRole.Undefiened;
            public ResStatus? Status { get; set; }
        }
        
    }
}
