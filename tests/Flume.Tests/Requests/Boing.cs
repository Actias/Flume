namespace Flume.Tests.Requests;

public class Boing : IRequest<BoingResponse>
{
    public int Thing1 { get; set; }
    public int Thing2 { get; set; }
}
