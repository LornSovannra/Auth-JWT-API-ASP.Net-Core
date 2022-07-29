namespace Auth_JWT.Responses
{
    public class UserResponseManager
    {
        public IEnumerable<string> Data { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
    }
}
