namespace BTCPayServerDockerConfigurator.Models
{
    public class UpdateSettings<T,TAdditionalData> where TAdditionalData : new()
    {
        public T Settings { get; set; }
        public TAdditionalData Additional { get; set; } = new TAdditionalData();
        public string Json { get; set; }
    }
}