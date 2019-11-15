namespace BTCPayServerDockerConfigurator.Models
{
    public class UpdateSettings<T,TAdditionalData>
    {
        public T Settings { get; set; }
        public TAdditionalData Additional { get; set; }
        public string Json { get; set; }
    }
}