namespace SeaFight.API.DTOs
{
    public class ApiResponse<T>
    {
        public bool Succes { get; set; }
        public T Data { get; set; }
        public string Error { get; set; }
    }
}
