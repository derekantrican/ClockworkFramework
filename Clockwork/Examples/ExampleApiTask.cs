using Clockwork.Core;

namespace Clockwork
{
    public class ExampleApiTask : ITask
    {
        public Interval Interval => new Interval(TimeType.Second, 30);

        public void Run()
        {
            dynamic user = Utilities.JsonToDynamic(Utilities.ApiRequest("https://randomuser.me/api", HttpMethod.Get)).results[0];
            Utilities.WriteToConsoleWithColor($"The random user is {user.name.first} {user.name.last}", ConsoleColor.Yellow);
        }
    }
}