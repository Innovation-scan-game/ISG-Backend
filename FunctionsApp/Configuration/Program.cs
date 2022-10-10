// using System.Threading.Tasks;
// using FunctionsApp.Security;
// using Microsoft.Extensions.Hosting;
//
// public class Program
// {
//     static Task Main()
//     {
//         var host = new HostBuilder()
//             .ConfigureFunctionsWorkerDefaults(
//                 builder =>
//                 {
//                     builder.UseMiddleware<JwtMiddleware>();
//                 }
//             )
//             .Build();
//
//         return host.RunAsync();
//     }
// }
