using Contracts;
using MassTransit;

namespace AuctionService.Consumers;

public class AuctionCreatedFaultConsumer : IConsumer<Fault<AuctionCreated>>
{
  public async Task Consume(ConsumeContext<Fault<AuctionCreated>> context)
  {
    Console.WriteLine("--> Consuming faulty creation");
    var exception = context.Message.Exceptions.First();
    if(exception.ExceptionType == "System.ArgumentException")
    {
      //change the Model name
      context.Message.Message.Model = "FooBar";
      //republish
      await context.Publish(context.Message.Message);
    }
    else 
    {
      //all other faults
      Console.WriteLine("Not an argument exception - updte error dashboard somewhere");
    }
  }
}
