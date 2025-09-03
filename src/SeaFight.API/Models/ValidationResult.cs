namespace SeaFight.API.Models
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public List<string> ErrorMessage()
        {
            return Errors;
        }
    }
}
