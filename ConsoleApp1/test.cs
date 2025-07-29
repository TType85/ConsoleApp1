using System;

// Custom attribute to control execution
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CanRunAttribute : Attribute
{
    public bool IsAllowedToRun { get; }

    public CanRunAttribute(bool isAllowed)
    {
        IsAllowedToRun = isAllowed;
    }
}

// Interface to define the Run method
public interface IRunnable
{
    void Run();
}

// Example class using the attribute
[CanRun(true)] // Set to true to allow running, false to prevent
public class MyTask : IRunnable
{
    public void Run()
    {
        Console.WriteLine("Task is running!");
    }
}

// Helper class to check and execute
public static class Runner
{
    public static void Execute(IRunnable task)
    {
        // Get the type of the task
        Type type = task.GetType();
        
        // Check for CanRunAttribute
        var attribute = Attribute.GetCustomAttribute(type, typeof(CanRunAttribute)) as CanRunAttribute;
        
        if (attribute != null && attribute.IsAllowedToRun)
        {
            task.Run();
        }
        else
        {
            Console.WriteLine("Task is not allowed to run.");
        }
    }
}

// Example usage
class Program
{
    static void Main()
    {
        IRunnable task = new MyTask();
        Runner.Execute(task); // Will run if CanRun(true)

        // Example with a class that cannot run
        [CanRun(false)]
        class BlockedTask : IRunnable
        {
            public void Run()
            {
                Console.WriteLine("This should not run!");
            }
        }

        IRunnable blockedTask = new BlockedTask();
        Runner.Execute(blockedTask); // Will not run
    }
}
