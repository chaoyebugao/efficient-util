namespace AwsEcrLogin
{
    internal class Program
    {
        static Task Main(string[] args)
        {
            return LoginService.Execute();
        }

    }
}
