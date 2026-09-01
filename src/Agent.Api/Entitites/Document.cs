namespace Agent.Api.Entitites
{
    public sealed class Document
    {
        public Guid Id { get; set; }

        public string FileName { get; set; } =
            string.Empty;

        public string ContentHash { get; set; } =
            string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }
}
