using System;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using SimpleInjector;
using Taskboard.Commands.Commands;
using Taskboard.Commands.DTO;
using Taskboard.Commands.Handlers;
using Taskboard.Commands.Repositories;

namespace Taskboard.Commands.Api
{
    public static class UpdateTask
    {
        public static Container Container = BuildContainer();

        [FunctionName(nameof(UpdateTask))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "list/{listid}/task/{taskid}")] TaskDTO task,
            string listid, string taskid)
        {
            var command = new UpdateTaskCommand
            {
                ListId = listid,
                TaskId = taskid,
                Name = task.Name,
                Description = task.Description
            };
            var handler = Container.GetInstance<ICommandHander<UpdateTaskCommand>>();

            var result = await handler.Execute(command);

            return result.Match<IActionResult>(
                error => new InternalServerErrorResult(),
                () => new NoContentResult()
            );
        }

        private static Container BuildContainer()
        {
            var container = new Container();

            container.RegisterSingleton(() => new TelemetryClient
            {
                InstrumentationKey = Environment.GetEnvironmentVariable("AI_INSTRUMENTATIONKEY")
            });
            container.RegisterSingleton<IDocumentClient>(() =>
                new DocumentClient(new Uri(Environment.GetEnvironmentVariable("COSMOS_ENDPOINT")),
                    Environment.GetEnvironmentVariable("COSMOS_KEY")));
            container.Register<IListRepository>(() => new ListRepository(container.GetInstance<TelemetryClient>(),
                container.GetInstance<IDocumentClient>(),
                Environment.GetEnvironmentVariable("COSMOS_DB"),
                Environment.GetEnvironmentVariable("COSMOS_COLLECTION")));
            container.Register<ICommandHander<UpdateTaskCommand>, UpdateTaskCommandHandler>();

            return container;
        }
    }
}