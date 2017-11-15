namespace InEngine.Core
{
    public class CommandResult
    {
        public bool IsSuccessful { get; set; }
        public string Message { get; set; }

        public CommandResult(bool isSuccssful, string message = "")
        {
            IsSuccessful = isSuccssful;
            Message = message;
        }
    }
}
