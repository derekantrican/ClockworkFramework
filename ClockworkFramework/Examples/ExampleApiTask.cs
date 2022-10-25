using ClockworkFramework.Core;

namespace ClockworkFramework.Examples
{
    public class ExampleApiTask : IClockworkTaskBase
    {
        [TaskMethod]
        [Interval(TimeType.Minute, 1)]
        public void Run()
        {
            dynamic user = Utilities.JsonToDynamic(Utilities.ApiRequest("https://randomuser.me/api", HttpMethod.Get)).results[0];
            Utilities.WriteToConsoleWithColor($"The random user is {user.name.first} {user.name.last}", ConsoleColor.Yellow);
        }
    }
}