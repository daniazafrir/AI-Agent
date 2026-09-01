namespace Agent.Api.Features.Knowledge.Chunking;

public interface IChunkingService
{
    IReadOnlyList<string> Split(string text, int chunkSize = 800, int overlap = 150);

}